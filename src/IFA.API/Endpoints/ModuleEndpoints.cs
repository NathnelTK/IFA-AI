using IFA.Application.Common.Helpers;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Endpoints
{
    public static class ModuleEndpoints
    {
        public static void MapModuleEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/courses/{courseId:guid}/current-module",
                    GetCurrentModule)
                .WithTags("Modules")
                .WithSummary(
                    "PR 3.2 - selects the learner's current module, claiming it for generation if needed");

            app.MapPost(
                    "/api/modules/{moduleId:guid}/generate",
                    GenerateModuleContent)
                .WithTags("Modules")
                .WithSummary(
                    "PR 3.3 - Content Builder: writes lessons + quiz for a module already claimed via current-module");
        }

        private static async Task<IResult> GetCurrentModule(
            Guid courseId,
            Guid learnerId, // Query parameter for now - see note below
            ICourseOrchestrationService orchestration,
            CancellationToken ct)
        {
            var result = await orchestration.SelectCurrentModuleAsync(
                courseId,
                learnerId,
                ct);

            return result.Outcome switch
            {
                ModuleSelectionOutcome.CourseComplete =>
                    Results.Ok(new
                    {
                        outcome = "CourseComplete"
                    }),

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

        private static async Task<IResult> GenerateModuleContent(
            Guid moduleId,
            IApplicationDbContext db,
            IContentBuilderService contentBuilder,
            CancellationToken ct)
        {
            var module = await db.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == moduleId, ct);

            if (module is null)
                return Results.NotFound();

            // Enforces the sequence:
            // PR 3.2 must claim the module before PR 3.3 generates content.
            if (module.GenerationStatus != ModuleGenerationStatus.Generating)
            {
                return Results.Conflict(
                    $"Module must be in 'Generating' status first " +
                    $"(currently '{module.GenerationStatus}'). " +
                    $"Call /current-module to claim it.");
            }

            var profile = module.Course!.SourceLearnerProfileId is Guid profileId
                ? await db.LearnerProfiles
                    .FirstOrDefaultAsync(p => p.Id == profileId, ct)
                : null;

            if (profile is null)
            {
                return Results.UnprocessableEntity(
                    "This course has no source learner profile — " +
                    "cannot generate content without it.");
            }

            var research = module.Course.SourceResearchPackageId is Guid researchId
                ? await db.ResearchPackages
                    .Include(r => r.AcademicSources)
                    .FirstOrDefaultAsync(r => r.Id == researchId, ct)
                : null;

            var content = await contentBuilder.GenerateModuleContentAsync(
                module,
                profile,
                research ?? new ResearchPackage
                {
                    LearnerProfileId = profile.Id
                },
                ct);

            if (content is null)
            {
                // Do not leave the module stuck in Generating.
                // PR 3.2 allows Failed modules to be claimed again.
                module.GenerationStatus = ModuleGenerationStatus.Failed;

                await db.SaveChangesAsync(ct);

                return Results.Problem(
                    "Content generation failed. The module can be retried.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            // Shared mapping logic:
            // GeneratedModuleContent -> Lessons + Quiz + Questions
            CourseGenerationHelpers.ApplyGeneratedContent(
                module,
                content);

            // The endpoint still controls the module lifecycle.
            module.GenerationStatus = ModuleGenerationStatus.Ready;
            module.GeneratedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                moduleId = module.Id,
                status = module.GenerationStatus.ToString(),
                lessonCount = content.Lessons.Count,
                questionCount = content.Questions.Count
            });
        }
    }
}

