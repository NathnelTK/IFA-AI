

// IFA.API/Endpoints/LearnerEndpoints.cs
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
            var group = app.MapGroup("/api/learners").WithTags("Learners");

            group.MapPost("/{learnerId:guid}/profile", CreateProfile)
                .WithName("CreateLearnerProfile")
                .WithSummary("PR 2.1 - Understanding Agent output: create a learner profile");

            group.MapPost("/{learnerId:guid}/research", TriggerResearch)
                .WithName("TriggerResearch")
                .WithSummary("PR 2.3 - run the Research Agent against the learner's latest profile");

            group.MapGet("/{learnerId:guid}/research/latest", GetLatestResearch)
                .WithName("GetLatestResearchPackage");
        }

        private static async Task<IResult> CreateProfile(
            Guid learnerId,
            CreateLearnerProfileRequest request,
            IApplicationDbContext db,
            CancellationToken cancellationToken)
        {
            var learnerExists = await db.Learners.AnyAsync(l => l.Id == learnerId, cancellationToken);
            if (!learnerExists)
                return Results.NotFound($"Learner {learnerId} not found.");

            if (string.IsNullOrWhiteSpace(request.Goal))
                return Results.BadRequest("Goal is required.");

            var profile = new LearnerProfile
            {
                LearnerId = learnerId,
                Goal = request.Goal,
                SubjectTopic = request.SubjectTopic,
                CurrentLevel = request.CurrentLevel,
                TargetOutcome = request.TargetOutcome,
                AvailableStudyHoursPerWeek = request.AvailableStudyHoursPerWeek,
                PreferredLanguage = request.PreferredLanguage,
                PreferredLearningStyle = request.PreferredLearningStyle,
                Constraints = request.Constraints ?? new List<string>(),
                PreferredYouTubeChannels = request.PreferredYouTubeChannels ?? new List<string>(),
                KnownStrengths = request.KnownStrengths ?? new List<string>(),
                KnownWeaknesses = request.KnownWeaknesses ?? new List<string>()
            };

            db.Add(profile);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/learners/{learnerId}/profile/{profile.Id}", profile);
        }

        private static async Task<IResult> TriggerResearch(
            Guid learnerId,
            IApplicationDbContext db,
            IResearchService researchService,
            CancellationToken cancellationToken)
        {
            // Latest profile by CreatedAt - a learner can have multiple
            // profiles over time (see LearnerProfileConfiguration notes),
            // research always runs against the most recent one.
            var profile = await db.LearnerProfiles
                .Where(p => p.LearnerId == learnerId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (profile is null)
                return Results.NotFound($"No profile found for learner {learnerId}. Create one first.");

            var package = await researchService.BuildResearchPackageAsync(profile, cancellationToken);

            db.Add(package);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(package);
        }

        private static async Task<IResult> GetLatestResearch(
            Guid learnerId,
            IApplicationDbContext db,
            CancellationToken cancellationToken)
        {
            var package = await db.ResearchPackages
                .Where(r => r.LearnerProfile!.LearnerId == learnerId)
                .OrderByDescending(r => r.CreatedAt)
                .Include(r => r.AcademicSources)
                .Include(r => r.PracticalResources)
                .Include(r => r.VideoResources)
                .FirstOrDefaultAsync(cancellationToken);

            return package is null
                ? Results.NotFound($"No research package found for learner {learnerId}.")
                : Results.Ok(package);
        }
    }
}