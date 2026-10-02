using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public class CoursePipelineProposal
    {
        public string CourseTitle { get; set; } = string.Empty;
        public string TargetGoal { get; set; } = string.Empty;
        public int TotalEstimatedHours { get; set; }
        public List<ModuleSummaryDto> Modules { get; set; } = new List<ModuleSummaryDto>();
    }

    public class ModuleSummaryDto
    {
        public int ModuleNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int EstimatedHours { get; set; }
        public List<string> KeyTopics { get; set; } = new List<string>();
    }

    public class JitModuleGenerationRequest
    {
        public string CourseTitle { get; set; } = string.Empty;
        public int ModuleNumber { get; set; } = 1;
        public string ModuleTitle { get; set; } = string.Empty;
        public string TargetGoal { get; set; } = string.Empty;
        public string? PreferredVideoCreator { get; set; }
        public List<string>? PriorQuizWeakAreas { get; set; }
    }

    public class GeneratedModuleResult
    {
        public Module Module { get; set; } = new Module();
        public Lesson Lesson { get; set; } = new Lesson();
        public Quiz Quiz { get; set; } = new Quiz();
    }

    public interface ICourseGenerationService
    {
        Task<CoursePipelineProposal> GenerateCoursePipelineProposalAsync(
        LearnerProfile profile,
        ResearchPackage? researchPackage,
        CancellationToken cancellationToken = default);
        Task<GeneratedModuleResult> GenerateJitModuleAsync(JitModuleGenerationRequest request, CancellationToken cancellationToken = default);
    }
}
