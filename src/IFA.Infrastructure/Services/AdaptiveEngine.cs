using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Services
{
    public class AdaptiveEngine : IAdaptiveEngine
    {
        private readonly IApplicationDbContext _context;
        private readonly IActivityService _activityService;

        public AdaptiveEngine(IApplicationDbContext context, IActivityService activityService)
        {
            _context = context;
            _activityService = activityService;
        }

        public async Task<QuizEvaluationResult> EvaluateAndAdaptAsync(Guid learnerId, QuizSubmissionRequest request, CancellationToken ct = default)
        {
            await using var transaction = await CourseAccess.BeginMutationAsync(_context, ct);
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Module)
                .ThenInclude(m => m!.Course)
                .FirstOrDefaultAsync(q => q.Id == request.QuizId, ct);

            if (quiz == null)
            {
                throw new CourseAccessException(404, "Quiz not found.");
            }

            var access = await CourseAccess.GetAsync(_context, quiz.Module!.CourseId, learnerId, ct);
            access.RequireContent(learnerMutation: true);
            if (quiz.Questions.Count == 0 || quiz.PassingScorePercentage is < 1 or > 100 || access.Course.MaxQuizAttempts is < 1 or > 100 ||
                quiz.Questions.Any(q => q.Options.Count < 2 || q.CorrectOptionIndex < 0 || q.CorrectOptionIndex >= q.Options.Count))
                throw new CourseAccessException(409, "This quiz needs educator review before it can be submitted.");
            if (request.Answers == null || request.Answers.Count != quiz.Questions.Count ||
                quiz.Questions.Any(q => !request.Answers.TryGetValue(q.Id, out var selected) || selected < 0 || selected >= q.Options.Count))
                throw new CourseAccessException(400, "Submit exactly one valid option index for every question in this quiz, with no extra question IDs.");

            var previousAttempts = await _context.Assessments
                .CountAsync(a => a.QuizId == quiz.Id && a.LearnerId == learnerId, ct);
            if (previousAttempts >= access.Course.MaxQuizAttempts)
                throw new CourseAccessException(409, $"The maximum of {access.Course.MaxQuizAttempts} quiz attempts has been reached.");
            var previouslyPassed = await _context.Assessments.AnyAsync(a => a.QuizId == quiz.Id && a.LearnerId == learnerId && a.IsPassed, ct);

            var assessment = new Assessment
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                QuizId = quiz.Id,
                AttemptNumber = previousAttempts + 1,
                SubmittedAt = DateTime.UtcNow
            };

            int correctCount = 0;
            var feedbacks = new List<QuestionFeedbackDto>();
            var skillResults = new Dictionary<string, (int correct, int total)>(StringComparer.OrdinalIgnoreCase);

            foreach (var q in quiz.Questions)
            {
                var selectedIndex = request.Answers[q.Id];
                bool isCorrect = selectedIndex == q.CorrectOptionIndex;
                if (isCorrect) correctCount++;

                var skillName = string.IsNullOrWhiteSpace(q.TargetSkillName) ? "General" : q.TargetSkillName;
                if (!skillResults.ContainsKey(skillName))
                {
                    skillResults[skillName] = (0, 0);
                }
                var current = skillResults[skillName];
                skillResults[skillName] = (current.correct + (isCorrect ? 1 : 0), current.total + 1);

                assessment.Responses.Add(new AssessmentResponse
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessment.Id,
                    QuestionId = q.Id,
                    SelectedOptionIndex = selectedIndex,
                    IsCorrect = isCorrect,
                    Feedback = isCorrect ? "Correct! " + q.Explanation : "Incorrect. " + q.Explanation
                });

                feedbacks.Add(new QuestionFeedbackDto
                {
                    QuestionId = q.Id,
                    Prompt = q.Prompt,
                    SelectedOptionIndex = selectedIndex,
                    CorrectOptionIndex = q.CorrectOptionIndex,
                    IsCorrect = isCorrect,
                    Explanation = q.Explanation,
                    TargetSkillName = skillName
                });
            }

            int scorePercentage = quiz.Questions.Count > 0 ? (int)Math.Round((double)correctCount / quiz.Questions.Count * 100) : 0;
            bool isPassed = scorePercentage >= quiz.PassingScorePercentage;

            assessment.ScorePercentage = scorePercentage;
            assessment.IsPassed = isPassed;
            _context.Add(assessment);

            if (isPassed && !previouslyPassed)
            {
                var state = await _context.LearnerStates.FirstOrDefaultAsync(s => s.LearnerId == learnerId, ct);
                if (state != null)
                {
                    state.CompletedQuizzesCount++;
                    state.CurrentCourseId = access.Course.Id;
                    state.CurrentModuleId = quiz.ModuleId;
                    state.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Update SkillMetrics for the learner
            var mastered = new List<string>();
            var weak = new List<string>();

            foreach (var kvp in skillResults)
            {
                var skillName = kvp.Key;
                var accuracy = (double)kvp.Value.correct / kvp.Value.total;

                var existingSkill = await _context.SkillMetrics
                    .FirstOrDefaultAsync(s => s.LearnerId == learnerId && s.SkillName == skillName, ct);

                if (existingSkill == null)
                {
                    existingSkill = new SkillMetric
                    {
                        Id = Guid.NewGuid(),
                        LearnerId = learnerId,
                        SkillName = skillName,
                        CompetencyPercentage = (int)Math.Round(accuracy * 100),
                        IsWeakArea = accuracy < 0.70,
                        LastAssessedAt = DateTime.UtcNow
                    };
                    _context.Add(existingSkill);
                }
                else
                {
                    existingSkill.CompetencyPercentage = (int)Math.Round(accuracy * 100);
                    existingSkill.IsWeakArea = accuracy < 0.70;
                    existingSkill.LastAssessedAt = DateTime.UtcNow;
                }

                if (existingSkill.IsWeakArea)
                {
                    weak.Add(skillName);
                }
                else
                {
                    mastered.Add(skillName);
                }
            }

            await _context.SaveChangesAsync(ct);
            await CourseProgress.UpdateAsync(_context, access.Course, access.Enrollment!, ct);

            // Record activity
            await _activityService.RecordActivityAsync(
                learnerId,
                isPassed ? "quiz_passed" : "quiz_failed",
                $"Completed {quiz.Title}",
                $"Scored {scorePercentage}% on {quiz.Title} ({correctCount}/{quiz.Questions.Count} correct).",
                ct: ct);
            if (transaction != null) await transaction.CommitAsync(ct);

            string nextAction = isPassed
                ? (weak.Any() 
                    ? $"Passed with {scorePercentage}%! Reinforcement concepts for [{string.Join(", ", weak)}] will be woven into the next module."
                    : $"Excellent mastery ({scorePercentage}%)! You are ready for advanced concepts in the next module.")
                : previousAttempts + 1 >= access.Course.MaxQuizAttempts
                    ? "The attempt limit has been reached. Review the feedback and contact your educator for help."
                    : "Review the lesson takeaways and question feedback, then retry the assessment.";

            return new QuizEvaluationResult
            {
                AssessmentId = assessment.Id,
                ScorePercentage = scorePercentage,
                IsPassed = isPassed,
                TotalQuestions = quiz.Questions.Count,
                CorrectAnswersCount = correctCount,
                QuestionFeedbacks = feedbacks,
                MasteredSkills = mastered,
                WeakSkillsToReinforce = weak,
                AdaptiveNextAction = nextAction
            };
        }

        public async Task<List<string>> GetAdaptiveConstraintsForNextModuleAsync(Guid learnerId, Guid courseId, CancellationToken ct = default)
        {
            var weakSkills = await _context.SkillMetrics
                .Where(s => s.LearnerId == learnerId && s.IsWeakArea)
                .Select(s => s.SkillName)
                .ToListAsync(ct);

            return weakSkills;
        }
    }
}
