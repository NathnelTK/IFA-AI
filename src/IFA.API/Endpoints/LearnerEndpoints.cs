

// IFA.API/Endpoints/LearnerEndpoints.cs
using System.Text.Json;
using System.Security.Claims;
using IFA.API.Contracts;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Endpoints
{
    public static class LearnerEndpoints
    {
        public static void MapLearnerEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/learners").WithTags("Learners").RequireAuthorization();

            group.MapPost("/{learnerId:guid}/profile", CreateProfile)
                .WithName("CreateLearnerProfile")
                .WithSummary("PR 2.1 - Understanding Agent output: create a learner profile");

            group.MapPost("/{learnerId:guid}/research", TriggerResearch)
                .WithName("TriggerResearch")
                .WithSummary("PR 2.3 - run the Research Agent against the learner's latest profile")
                .RequireRateLimiting("generation");

            group.MapGet("/{learnerId:guid}/research/latest", GetLatestResearch)
                .WithName("GetLatestResearchPackage");
        }

        private static async Task<IResult> CreateProfile(
            Guid learnerId,
            CreateLearnerProfileRequest request,
            IApplicationDbContext db,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            if (!IsSelf(user, learnerId)) return Results.Forbid();
            var learnerExists = await db.Learners.AnyAsync(l => l.Id == learnerId, cancellationToken);
            if (!learnerExists)
                return Results.NotFound($"Learner {learnerId} not found.");

            if (string.IsNullOrWhiteSpace(request.Goal))
                return Results.BadRequest("Goal is required.");
            if (request.Goal.Length > 2000 || request.AvailableStudyHoursPerWeek is < 0 or > 168 ||
                new[] { request.SubjectTopic, request.CurrentLevel, request.TargetOutcome, request.PreferredLanguage, request.PreferredLearningStyle }
                    .Any(value => value?.Length > 500) ||
                new[] { request.Constraints, request.PreferredYouTubeChannels, request.KnownStrengths, request.KnownWeaknesses }
                    .Any(values => values is { Count: > 30 } || values?.Any(value => value == null || value.Length > 500) == true))
                return Results.BadRequest("Profile fields exceed the supported limits.");

            var profile = await db.LearnerProfiles.FirstOrDefaultAsync(p => p.LearnerId == learnerId, cancellationToken);
            var created = profile == null;
            profile ??= new LearnerProfile { LearnerId = learnerId };
            profile.LearningGoal = request.Goal.Trim();
            profile.Subject = request.SubjectTopic?.Trim() ?? string.Empty;
            profile.CurrentLevel = request.CurrentLevel?.Trim() ?? "Not set";
            profile.TargetOutcome = request.TargetOutcome?.Trim() ?? string.Empty;
            profile.WeeklyStudyHours = request.AvailableStudyHoursPerWeek ?? 0;
            profile.PreferredLanguage = request.PreferredLanguage?.Trim() ?? "en";
            profile.LearningStyle = request.PreferredLearningStyle?.Trim() ?? string.Empty;
            profile.Constraints = request.Constraints is null ? string.Empty : JsonSerializer.Serialize(request.Constraints);
            profile.PreferredYouTubeChannelsJson = JsonSerializer.Serialize(request.PreferredYouTubeChannels ?? new List<string>());
            profile.KnownStrengthsJson = JsonSerializer.Serialize(request.KnownStrengths ?? new List<string>());
            profile.KnownWeaknessesJson = JsonSerializer.Serialize(request.KnownWeaknesses ?? new List<string>());
            profile.UpdatedAt = DateTime.UtcNow;

            if (created) db.Add(profile);
            await db.SaveChangesAsync(cancellationToken);

            return created
                ? Results.Created($"/api/learners/{learnerId}/profile/{profile.Id}", profile)
                : Results.Ok(profile);
        }

        private static async Task<IResult> TriggerResearch(
            Guid learnerId,
            IApplicationDbContext db,
            IResearchService researchService,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            if (!IsSelf(user, learnerId)) return Results.Forbid();
            // Latest profile by UpdatedAt - a learner can have multiple
            // profiles over time, research always runs against the most recent.
            var profile = await db.LearnerProfiles
                .Where(p => p.LearnerId == learnerId)
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (profile is null)
                return Results.NotFound($"No profile found for learner {learnerId}. Create one first.");

            // ConductResearchAsync persists the package itself.
            var package = await researchService.ConductResearchAsync(
                profile.LearningGoal, learnerId, null, cancellationToken);

            return Results.Ok(package);
        }

        private static async Task<IResult> GetLatestResearch(
            Guid learnerId,
            IApplicationDbContext db,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            if (!IsSelf(user, learnerId)) return Results.Forbid();
            var package = await db.ResearchPackages
                .Where(r => r.LearnerId == learnerId)
                .OrderByDescending(r => r.CreatedAt)
                .Include(r => r.Sources)
                .FirstOrDefaultAsync(cancellationToken);

            return package is null
                ? Results.NotFound($"No research package found for learner {learnerId}.")
                : Results.Ok(package);
        }

        private static bool IsSelf(ClaimsPrincipal user, Guid learnerId) =>
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId) && currentId == learnerId;
    }
}
