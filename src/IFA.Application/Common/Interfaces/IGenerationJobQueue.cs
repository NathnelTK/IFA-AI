

namespace IFA.Application.Common.Interfaces
{
    public interface IGenerationJobQueue
    {
        Task EnqueueAsync(Guid jobId, CancellationToken ct = default);
        Task<Guid> DequeueAsync(CancellationToken ct);
    }
}