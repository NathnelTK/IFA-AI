namespace IFA.Application.Common.Interfaces
{
    public interface ICourseGenerationOrchestrator
    {
        // Fast - just writes a Queued job row and enqueues it. Called
        // directly from the API endpoint, returns almost immediately.
        Task<Guid> StartJobAsync(Guid learnerId, Guid learnerProfileId, CancellationToken ct = default);

        // Slow - runs the real pipeline. Called only by the background worker.
        Task RunJobAsync(Guid jobId, CancellationToken ct = default);
    }
}