using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public enum ModuleSelectionOutcome
    {
        ReadyToStudy,      // module already has real content — learner can study it now
        NeedsGeneration,    // module is Blueprint, we just claimed it — caller should trigger Content Builder next
        AlreadyGenerating,  // another request already claimed it — caller should wait/poll, not regenerate
        CourseComplete       // every module is Ready and the learner has finished them all
    }

    public class CurrentModuleResult
    {
        public ModuleSelectionOutcome Outcome { get; set; }
        public Module? Module { get; set; } // null only when Outcome is CourseComplete
    }

    public interface ICourseOrchestrationService
    {
        Task<CurrentModuleResult> SelectCurrentModuleAsync(Guid courseId, Guid learnerId, CancellationToken ct = default);
    }
}