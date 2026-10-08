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
        public string? Category { get; set; }
        public string? TargetAudience { get; set; }
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
        /// <summary>Blueprint key topics used as the section plan for just-in-time, section-by-section lesson generation.</summary>
        public List<string>? KeyTopics { get; set; }
        public string ResearchContext { get; set; } = string.Empty;
        /// <summary>Learner-supplied links (docs, repos, slides) the builder should reference in lesson content.</summary>
        public List<string>? ExternalMaterials { get; set; }
    }

    public class GeneratedModuleResult
    {
        public Module Module { get; set; } = new Module();
        /// <summary>Lessons generated section-by-section for this module (at least one).</summary>
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
        /// <summary>The module's assessments: two mini checkpoint quizzes and one exam.</summary>
        public List<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }

    public interface ICourseGenerationService
    {
        Task<CoursePipelineProposal> GenerateCoursePipelineProposalAsync(
        LearnerProfile profile,
        ResearchPackage? researchPackage,
        CancellationToken cancellationToken = default,
        IReadOnlyList<string>? externalMaterials = null);
        Task<GeneratedModuleResult> GenerateJitModuleAsync(JitModuleGenerationRequest request, CancellationToken cancellationToken = default);
    }
}
