using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    /// <summary>
    /// Best-effort content extraction for a ProjectSource link. Never throws —
    /// the source always ends up with a terminal Status (Extracted or Failed)
    /// and, on failure, a FailureReason explaining why.
    /// </summary>
    public interface ISourceIngestionService
    {
        Task IngestAsync(ProjectSource source, CancellationToken ct = default);
    }
}
