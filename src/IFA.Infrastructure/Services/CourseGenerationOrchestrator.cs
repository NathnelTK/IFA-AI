using IFA.Application.Common.Helpers;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    public class CourseGenerationOrchestrator : ICourseGenerationOrchestrator
    {
        private readonly IApplicationDbContext _db;
        private readonly IGenerationJobQueue _queue;
        private readonly IResearchService _research;
        private readonly ICourseArchitectService _architect;
        private readonly ICourseOrchestrationService _moduleSelection;
        private readonly IContentBuilderService _contentBuilder;
        private readonly ILogger<CourseGenerationOrchestrator> _logger;

        public CourseGenerationOrchestrator(
            IApplicationDbContext db, IGenerationJobQueue queue, IResearchService research,
            ICourseArchitectService architect, ICourseOrchestrationService moduleSelection,
            IContentBuilderService contentBuilder, ILogger<CourseGenerationOrchestrator> logger)
        {
            _db = db; _queue = queue; _research = research;
            _architect = architect; _moduleSelection = moduleSelection;
            _contentBuilder = contentBuilder; _logger = logger;
        }

        public async Task<Guid> StartJobAsync(Guid learnerId, Guid learnerProfileId, CancellationToken ct = default)
        {
            var job = new GenerationJob { LearnerId = learnerId, LearnerProfileId = learnerProfileId };
            _db.Add(job);
            await _db.SaveChangesAsync(ct);
            await _queue.EnqueueAsync(job.Id, ct);
            return job.Id;
        }

        public async Task RunJobAsync(Guid jobId, CancellationToken ct = default)
        {
            var job = await _db.GenerationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job is null) { _logger.LogWarning("Job {JobId} not found - skipping.", jobId); return; }
            // Idempotency guard: if this job somehow gets dequeued twice
            // (e.g. a duplicate enqueue during a race at startup recovery),
            // don't redo finished work.
            if (job.Status == GenerationJobStatus.Completed) return;

            job.Status = GenerationJobStatus.Running;
            job.StartedAt ??= DateTime.UtcNow;
            job.AttemptCount++;
            await _db.SaveChangesAsync(ct);

            try
            {
                await SetStepAsync(job, GenerationStep.CheckingProfile, ct);
                var profile = await _db.LearnerProfiles.FirstOrDefaultAsync(p => p.Id == job.LearnerProfileId, ct)
                    ?? throw new InvalidOperationException("Source learner profile no longer exists.");

                await SetStepAsync(job, GenerationStep.Researching, ct);
                var research = await GetOrBuildResearchAsync(profile, ct);

                await SetStepAsync(job, GenerationStep.DesigningCourse, ct);
                var proposal = await _architect.GenerateBlueprintAsync(profile, research, ct)
                    ?? throw new InvalidOperationException("Course architect could not produce a valid blueprint.");

                var course = CourseGenerationHelpers.BuildCourseFromProposal(
                    proposal, profile, research.Id == Guid.Empty ? null : research.Id, job.LearnerId);
                _db.Add(course);
                _db.Add(new CourseEnrollment { CourseId = course.Id, LearnerId = job.LearnerId });
                await _db.SaveChangesAsync(ct);

                job.ResultingCourseId = course.Id;
                await _db.SaveChangesAsync(ct);

                await SetStepAsync(job, GenerationStep.SelectingModule, ct);
                var selection = await _moduleSelection.SelectCurrentModuleAsync(course.Id, job.LearnerId, ct);
                if (selection.Outcome != ModuleSelectionOutcome.NeedsGeneration || selection.Module is null)
                    throw new InvalidOperationException($"Unexpected module selection outcome: {selection.Outcome}");

                await SetStepAsync(job, GenerationStep.BuildingContent, ct);
                var content = await _contentBuilder.GenerateModuleContentAsync(selection.Module, profile, research, ct);
                if (content is null)
                {
                    selection.Module.GenerationStatus = ModuleGenerationStatus.Failed;
                    await _db.SaveChangesAsync(ct);
                    throw new InvalidOperationException("Content builder could not produce valid content for the first module.");
                }

                CourseGenerationHelpers.ApplyGeneratedContent(selection.Module, content);
                selection.Module.GenerationStatus = ModuleGenerationStatus.Ready;
                selection.Module.GeneratedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                job.Status = GenerationJobStatus.Completed;
                job.CurrentStep = GenerationStep.Done;
                job.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Generation job {JobId} completed. Course {CourseId}.", job.Id, course.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generation job {JobId} failed at step {Step}.", job.Id, job.CurrentStep);
                job.Status = GenerationJobStatus.Failed;
                job.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }

        private async Task<ResearchPackage> GetOrBuildResearchAsync(LearnerProfile profile, CancellationToken ct)
        {
            var existing = await _db.ResearchPackages.Include(r => r.AcademicSources)
                .Where(r => r.LearnerProfileId == profile.Id)
                .OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync(ct);
            if (existing is not null) return existing;

            var fresh = await _research.BuildResearchPackageAsync(profile, ct);
            _db.Add(fresh);
            await _db.SaveChangesAsync(ct);
            return fresh;
        }

        private async Task SetStepAsync(GenerationJob job, GenerationStep step, CancellationToken ct)
        {
            job.CurrentStep = step;
            await _db.SaveChangesAsync(ct); // written immediately so polling clients see live progress
        }
    }
}