// IFA.Application/Common/Interfaces/IResearchService.cs
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IResearchService
    {
        // Owns the full "profile -> normalized package" pipeline.
        // Never throws on a Scholarxiv failure/timeout - always
        // returns a ResearchPackage, possibly with empty AcademicSources.
        Task<ResearchPackage> BuildResearchPackageAsync(LearnerProfile profile, CancellationToken cancellationToken = default);
    }
}