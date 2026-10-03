using IFA.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Endpoints
{
    public static class LearningEndpoints
    {
        public static void MapLearningEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/modules/{moduleId:guid}").WithTags("Learning");

            group.MapGet("/", GetModuleDetail);
            group.MapGet("/lessons/{lessonId:guid}", GetLesson);
            group.MapPost("/lessons/{lessonId:guid}/complete", MarkLessonComplete);

        }

        private static async Task<IResult> GetModuleDetail(Guid moduleId, IApplicationDbContext db, CancellationToken ct)
        {
            var module = await db.Modules
                .Include(m => m.Lessons.OrderBy(l => l.LessonNumber))
                .Include(m => m.ModuleQuiz)
                .FirstOrDefaultAsync(m => m.Id == moduleId, ct);

            if (module is null) return Results.NotFound();

            // Quiz questions are intentionally excluded here. A learner
            // browsing the module overview should not see answers before
            // starting the quiz - that's a separate "start quiz" view.
            return Results.Ok(new
            {
                module.Id,
                module.Title,
                module.Summary,
                module.KeyTopics,
                status = module.GenerationStatus.ToString(),
                lessons = module.Lessons.Select(l => new
                {
                    l.Id,
                    l.LessonNumber,
                    l.Title,
                    l.Summary,
                    l.ReadingTimeMinutes
                    // ContentMarkdown deliberately omitted here too -
                    // fetched per-lesson via GetLesson, so the overview
                    // payload stays small.
                }),
                quizAvailable = module.ModuleQuiz is not null
            });
        }

        private static async Task<IResult> GetLesson(Guid moduleId, Guid lessonId, IApplicationDbContext db, CancellationToken ct)
        {
            var lesson = await db.Lessons
                .FirstOrDefaultAsync(l => l.Id == lessonId && l.ModuleId == moduleId, ct);

            return lesson is null ? Results.NotFound() : Results.Ok(lesson);
        }

        private static async Task<IResult> MarkLessonComplete(
    Guid moduleId, Guid lessonId, Guid learnerId, // learnerId as query param, same pattern as elsewhere
    IApplicationDbContext db, CancellationToken ct)
        {
            var lessonExists = await db.Lessons.AnyAsync(l => l.Id == lessonId && l.ModuleId == moduleId, ct);
            if (!lessonExists) return Results.NotFound();

            var existing = await db.LessonProgress
                .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.LearnerId == learnerId, ct);

            if (existing is not null)
            {
                existing.IsCompleted = true;
                existing.CompletedAt ??= DateTime.UtcNow;
            }
            else
            {
                db.Add(new LessonProgress
                {
                    LessonId = lessonId,
                    LearnerId = learnerId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok();
        }
    }

}