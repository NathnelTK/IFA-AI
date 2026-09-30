using IFA.Application.Common.Interfaces;

namespace IFA.API.Endpoints
{
    public static class ModuleEndpoints
    {
        public static void MapModuleEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/courses/{courseId:guid}/current-module", GetCurrentModule)
                .WithTags("Modules")
                .WithSummary("PR 3.2 - selects the learner's current module, claiming it for generation if needed");
        }

        private static async Task<IResult> GetCurrentModule(
            Guid courseId, Guid learnerId, // learnerId as a query param for now - see note below
            ICourseOrchestrationService orchestration, CancellationToken ct)
        {
            var result = await orchestration.SelectCurrentModuleAsync(courseId, learnerId, ct);

            return result.Outcome switch
            {
                ModuleSelectionOutcome.CourseComplete => Results.Ok(new { outcome = "CourseComplete" }),
                _ => Results.Ok(new
                {
                    outcome = result.Outcome.ToString(),
                    moduleId = result.Module!.Id,
                    moduleNumber = result.Module.ModuleNumber,
                    title = result.Module.Title,
                    summary = result.Module.Summary,
                    keyTopics = result.Module.KeyTopics,
                    status = result.Module.GenerationStatus.ToString()
                })
            };
        }
    }
}