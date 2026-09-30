

// IFA.API/Endpoints/CourseEndpoints.cs
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Endpoints
{
    public static class CourseEndpoints
    {
        public static void MapCourseEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/learners/{learnerId:guid}/course").WithTags("Course");
            group.MapPost("/propose", ProposeCourse)
                .WithSummary("PR 3.1 - Course Architect: design a blueprint (modules only, no lessons)");
        }

        private static async Task<IResult> ProposeCourse(
            Guid learnerId, IApplicationDbContext db, ICourseArchitectService architect, CancellationToken ct)
        {
            var profile = await db.LearnerProfiles
                .Where(p => p.LearnerId == learnerId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (profile is null)
                return Results.NotFound("No profile found. Complete the intake conversation first.");

            // Research is optional context: if none exists yet, design from the profile alone.
            var persisted = await db.ResearchPackages
                .Include(r => r.AcademicSources)
                .Where(r => r.LearnerProfileId == profile.Id)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(ct);
            var research = persisted ?? new ResearchPackage { LearnerProfileId = profile.Id };

            var proposal = await architect.GenerateBlueprintAsync(profile, research, ct);
            if (proposal is null)
                return Results.Problem("The course designer could not produce a valid outline. Please try again.",
                    statusCode: StatusCodes.Status502BadGateway);

            var hoursPerWeek = Math.Max(1, profile.AvailableStudyHoursPerWeek ?? 5);
            var weeks = Math.Max(1, (int)Math.Ceiling(proposal.TotalEstimatedHours / (double)hoursPerWeek));

            var course = new Course
            {
                Title = proposal.CourseTitle,
                Description = proposal.Description,
                Category = profile.SubjectTopic ?? "General",
                TargetAudience = profile.CurrentLevel ?? "General",
                EstimatedDuration = $"{weeks} week{(weeks == 1 ? "" : "s")}",
                CreatorLearnerId = learnerId,
                SourceLearnerProfileId = profile.Id,
                SourceResearchPackageId = persisted?.Id,
                // Explicit zero-state so a new AI-generated course never shows the fake default rating.
                Rating = 0,
                ReviewCount = "0",
                // Generated here so an empty ShareCode can never collide with a unique index.
                ShareCode = Guid.NewGuid().ToString("N")[..8]
            };

            foreach (var m in proposal.Modules)
            {
                course.Modules.Add(new Module
                {
                    ModuleNumber = m.ModuleNumber,
                    Title = m.Title,
                    Summary = m.Summary,
                    EstimatedHours = m.EstimatedHours,
                    KeyTopics = m.KeyTopics,
                    GenerationStatus = ModuleGenerationStatus.Blueprint
                });
            }

            db.Add(course);
            db.Add(new CourseEnrollment { CourseId = course.Id, LearnerId = learnerId });
            await db.SaveChangesAsync(ct);

            // Return a flat DTO, not the entity. Course <-> Module point at each other,
            // and serializing that graph would throw a JSON object-cycle error.
            return Results.Created($"/api/courses/{course.Id}", new CourseBlueprintResponse(
                course.Id, course.Title, course.Description, course.EstimatedDuration,
                proposal.TotalEstimatedHours,
                course.Modules.OrderBy(m => m.ModuleNumber).Select(m => new ModuleBlueprintResponse(
                    m.Id, m.ModuleNumber, m.Title, m.Summary, m.EstimatedHours, m.KeyTopics,
                    m.GenerationStatus.ToString())).ToList()));
        }
    }

    public record ModuleBlueprintResponse(
        Guid ModuleId, int ModuleNumber, string Title, string Summary,
        int EstimatedHours, List<string> KeyTopics, string Status);

    public record CourseBlueprintResponse(
        Guid CourseId, string Title, string Description, string EstimatedDuration,
        int TotalEstimatedHours, List<ModuleBlueprintResponse> Modules);
}