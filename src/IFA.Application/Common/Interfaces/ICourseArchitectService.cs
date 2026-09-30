// IFA.Application/Common/Interfaces/ICourseArchitectService.cs
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface ICourseArchitectService
    {
        // Returns null if no valid blueprint could be produced after a retry.
        Task<CoursePipelineProposal?> GenerateBlueprintAsync(
            LearnerProfile profile, ResearchPackage research, CancellationToken ct = default);
    }
}