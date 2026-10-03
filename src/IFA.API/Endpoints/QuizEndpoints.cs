// IFA.API/Endpoints/QuizEndpoints.cs
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Endpoints
{
    public record SubmitAnswerRequest(Guid QuestionId, int SelectedOptionIndex);
    public record SubmitQuizRequest(List<SubmitAnswerRequest> Answers);

    public static class QuizEndpoints
    {
        public static void MapQuizEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/quizzes/{quizId:guid}").WithTags("Quiz");

            group.MapGet("/", GetQuiz);
            group.MapPost("/submit", SubmitQuiz);
        }

        private static async Task<IResult> GetQuiz(Guid quizId, IApplicationDbContext db, CancellationToken ct)
        {
            var quiz = await db.Quizzes.Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId, ct);
            if (quiz is null) return Results.NotFound();

            // CorrectOptionIndex and Explanation withheld until after
            // submission - showing them now would let a learner see
            // answers before attempting the quiz.
            return Results.Ok(new
            {
                quiz.Id,
                quiz.Title,
                quiz.PassingScorePercentage,
                questions = quiz.Questions.Select(q => new { q.Id, q.Prompt, q.Options })
            });
        }

        private static async Task<IResult> SubmitQuiz(
            Guid quizId, Guid learnerId, SubmitQuizRequest request,
            IApplicationDbContext db, CancellationToken ct)
        {
            var quiz = await db.Quizzes.Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId, ct);
            if (quiz is null) return Results.NotFound();

            if (request.Answers.Count == 0)
                return Results.BadRequest("At least one answer is required.");

            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                LearnerId = learnerId,
                CompletedAt = DateTime.UtcNow
            };

            var correctCount = 0;
            foreach (var answer in request.Answers)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question is null) continue; // ignore answers to questions not in this quiz

                var isCorrect = answer.SelectedOptionIndex == question.CorrectOptionIndex;
                if (isCorrect) correctCount++;

                attempt.Answers.Add(new QuizAnswer
                {
                    QuestionId = question.Id,
                    SelectedOptionIndex = answer.SelectedOptionIndex,
                    IsCorrect = isCorrect
                });
            }

            // Score is based on the quiz's real question count, not just
            // how many the learner answered - an unanswered question
            // counts against the score, same as a wrong one.
            var totalQuestions = quiz.Questions.Count;
            attempt.ScorePercentage = totalQuestions == 0 ? 0 : (int)Math.Round(100.0 * correctCount / totalQuestions);
            attempt.IsPassed = attempt.ScorePercentage >= quiz.PassingScorePercentage;

            db.Add(attempt);
            await db.SaveChangesAsync(ct);

            // Explanations included now - the learner has already answered.
            return Results.Ok(new
            {
                attempt.Id,
                attempt.ScorePercentage,
                attempt.IsPassed,
                correctCount,
                totalQuestions,
                results = attempt.Answers.Select(a =>
                {
                    var q = quiz.Questions.First(x => x.Id == a.QuestionId);
                    return new
                    {
                        a.QuestionId,
                        a.SelectedOptionIndex,
                        a.IsCorrect,
                        correctOptionIndex = q.CorrectOptionIndex,
                        explanation = q.Explanation,
                        targetSkillName = q.TargetSkillName
                    };
                })
            });
        }
    }
}