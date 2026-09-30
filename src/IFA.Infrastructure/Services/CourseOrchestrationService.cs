// IFA.Infrastructure/Services/CourseOrchestrationService.cs
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Decides which module a learner should see next, and owns the
    /// Blueprint -> Generating transition (PR 3.2). This is plain
    /// application logic, not an LLM call - it lives here, not in AI/.
    /// </summary>
    public class CourseOrchestrationService : ICourseOrchestrationService
    {
        private readonly IApplicationDbContext _db;
        private readonly ILogger<CourseOrchestrationService> _logger;

        public CourseOrchestrationService(IApplicationDbContext db, ILogger<CourseOrchestrationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<CurrentModuleResult> SelectCurrentModuleAsync(
            Guid courseId, Guid learnerId, CancellationToken ct = default)
        {
            var modules = await _db.Modules
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.ModuleNumber)
                .ToListAsync(ct);

            foreach (var module in modules)
            {
                var isFinished = await IsFinishedByLearnerAsync(module, learnerId, ct);
                if (isFinished) continue; // this module is done, check the next one

                // First not-finished module in order = the current one.
                return module.GenerationStatus switch
                {
                    ModuleGenerationStatus.Ready => new CurrentModuleResult
                    {
                        Outcome = ModuleSelectionOutcome.ReadyToStudy,
                        Module = module
                    },

                    ModuleGenerationStatus.Generating => new CurrentModuleResult
                    {
                        Outcome = ModuleSelectionOutcome.AlreadyGenerating,
                        Module = module
                    },

                    // Blueprint or Failed - claim it now.
                    _ => await TryClaimForGenerationAsync(module, ct)
                };
            }

            // Every module was finished.
            return new CurrentModuleResult { Outcome = ModuleSelectionOutcome.CourseComplete, Module = null };
        }

        private async Task<CurrentModuleResult> TryClaimForGenerationAsync(Module module, CancellationToken ct)
        {
            // Re-read and guard against a race: only flip the status if it's
            // STILL Blueprint/Failed right now, not whatever we saw a moment
            // ago. ExecuteUpdateAsync issues a single UPDATE ... WHERE ...
            // and tells us how many rows it actually changed.
            var rowsChanged = await _db.Modules
                .Where(m => m.Id == module.Id &&
                    (m.GenerationStatus == ModuleGenerationStatus.Blueprint ||
                     m.GenerationStatus == ModuleGenerationStatus.Failed))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.GenerationStatus, ModuleGenerationStatus.Generating), ct);

            if (rowsChanged == 0)
            {
                // Someone else claimed it between our read and this write.
                _logger.LogInformation("Module {ModuleId} was claimed by another request first.", module.Id);
                return new CurrentModuleResult { Outcome = ModuleSelectionOutcome.AlreadyGenerating, Module = module };
            }

            module.GenerationStatus = ModuleGenerationStatus.Generating; // keep the in-memory copy consistent
            _logger.LogInformation("Claimed module {ModuleId} ('{Title}') for content generation.", module.Id, module.Title);
            return new CurrentModuleResult { Outcome = ModuleSelectionOutcome.NeedsGeneration, Module = module };
        }

        private async Task<bool> IsFinishedByLearnerAsync(Module module, Guid learnerId, CancellationToken ct)
        {
            // A Blueprint/Generating module has no lessons yet, so this is
            // naturally false for it - no special-casing required.
            var lessonIds = await _db.Lessons
                .Where(l => l.ModuleId == module.Id)
                .Select(l => l.Id)
                .ToListAsync(ct);

            if (lessonIds.Count == 0) return false;

            var completedCount = await _db.LessonProgress
                .Where(p => lessonIds.Contains(p.LessonId) && p.LearnerId == learnerId && p.IsCompleted)
                .CountAsync(ct);
            if (completedCount < lessonIds.Count) return false;

            var hasPassedQuiz = await _db.QuizAttempts
                .Where(a => a.LearnerId == learnerId && a.IsPassed &&
                    a.Quiz!.ModuleId == module.Id)
                .AnyAsync(ct);

            return hasPassedQuiz;
        }
    }
}