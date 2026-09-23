using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Progress.Services
{
    public class QuizSubmissionDto
    {
        public Guid QuizId { get; set; }
        public Guid LearnerId { get; set; }
        public List<QuestionAnswerDto> Answers { get; set; } = new();
    }

    public class QuestionAnswerDto
    {
        public Guid QuestionId { get; set; }
        public int SelectedOptionIndex { get; set; }
        public string AssociatedSkill { get; set; } = string.Empty;
    }

    public class AssessmentResultDto
    {
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int ScorePercentage { get; set; }
        public bool IsPassed { get; set; }
        public List<string> NewlyIdentifiedWeakAreas { get; set; } = new();
        public List<SkillMetricUpdateDto> UpdatedSkillMetrics { get; set; } = new();
    }

    public class SkillMetricUpdateDto
    {
        public string SkillName { get; set; } = string.Empty;
        public int NewCompetencyPercentage { get; set; }
        public bool IsWeakArea { get; set; }
    }

    public interface ISkillAssessmentService
    {
        Task<AssessmentResultDto> EvaluateQuizSubmissionAsync(
            Quiz quiz,
            QuizSubmissionDto submission,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Adaptive Learning Engine: Skill Assessment Service.
    /// Evaluates quiz results, updates skill competency metrics,
    /// and flags conceptual weak areas according to pedagogical standards.
    /// </summary>
    public class SkillAssessmentService : ISkillAssessmentService
    {
        public Task<AssessmentResultDto> EvaluateQuizSubmissionAsync(
            Quiz quiz,
            QuizSubmissionDto submission,
            CancellationToken cancellationToken = default)
        {
            var total = quiz.Questions.Count;
            if (total == 0)
            {
                return Task.FromResult(new AssessmentResultDto
                {
                    ScorePercentage = 100,
                    IsPassed = true
                });
            }

            int correctCount = 0;
            var weakAreas = new List<string>();
            var skillUpdates = new Dictionary<string, (int correct, int total)>();

            foreach (var q in quiz.Questions)
            {
                var skill = string.IsNullOrWhiteSpace(q.TargetSkillName) ? "General" : q.TargetSkillName;
                if (!skillUpdates.ContainsKey(skill))
                {
                    skillUpdates[skill] = (0, 0);
                }

                var userAns = submission.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                var isCorrect = userAns != null && userAns.SelectedOptionIndex == q.CorrectOptionIndex;

                var current = skillUpdates[skill];
                if (isCorrect)
                {
                    correctCount++;
                    skillUpdates[skill] = (current.correct + 1, current.total + 1);
                }
                else
                {
                    skillUpdates[skill] = (current.correct, current.total + 1);
                    if (!weakAreas.Contains(skill))
                    {
                        weakAreas.Add(skill);
                    }
                }
            }

            var scorePercent = (int)Math.Round((double)correctCount / total * 100);
            var isPassed = scorePercent >= quiz.PassingScorePercentage;

            var metricDtos = skillUpdates.Select(kvp =>
            {
                var competency = (int)Math.Round((double)kvp.Value.correct / kvp.Value.total * 100);
                return new SkillMetricUpdateDto
                {
                    SkillName = kvp.Key,
                    NewCompetencyPercentage = competency,
                    IsWeakArea = competency < 70
                };
            }).ToList();

            return Task.FromResult(new AssessmentResultDto
            {
                TotalQuestions = total,
                CorrectAnswers = correctCount,
                ScorePercentage = scorePercent,
                IsPassed = isPassed,
                NewlyIdentifiedWeakAreas = weakAreas,
                UpdatedSkillMetrics = metricDtos
            });
        }
    }
}
