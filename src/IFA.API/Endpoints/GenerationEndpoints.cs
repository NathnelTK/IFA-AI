using IFA.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Endpoints
{
    public static class GenerationEndpoints
    {
        public static void MapGenerationEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/learners/{learnerId:guid}/course/generate", StartGeneration)
                .WithTags("Generation")
                .WithSummary("PR 3.4 - kicks off the full pipeline in the background, returns immediately");

            app.MapGet("/api/generation-jobs/{jobId:guid}", GetJobStatus)
                .WithTags("Generation")
                .WithSummary("Poll this for progress");
        }

        private static async Task<IResult> StartGeneration(
            Guid learnerId, IApplicationDbContext db, ICourseGenerationOrchestrator orchestrator, CancellationToken ct)
        {
            var profile = await db.LearnerProfiles
                .Where(p => p.LearnerId == learnerId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (profile is null)
                return Results.NotFound("No profile found. Complete the intake conversation first.");

            var jobId = await orchestrator.StartJobAsync(learnerId, profile.Id, ct);
            return Results.Accepted($"/api/generation-jobs/{jobId}", new { jobId });
        }

        private static async Task<IResult> GetJobStatus(Guid jobId, IApplicationDbContext db, CancellationToken ct)
        {
            var job = await db.GenerationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job is null) return Results.NotFound();

            return Results.Ok(new
            {
                job.Id,
                status = job.Status.ToString(),
                currentStep = job.CurrentStep.ToString(),
                job.ResultingCourseId,
                job.ErrorMessage,
                job.AttemptCount,
                job.CreatedAt,
                job.StartedAt,
                job.CompletedAt
            });
        }
    }
}