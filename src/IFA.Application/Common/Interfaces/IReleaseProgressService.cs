namespace IFA.Application.Common.Interfaces
{
    public interface IReleaseProgressService
    {
        Task RecordAsync(
            Guid enrollmentId,
            string itemType,
            Guid itemId,
            decimal progressPercent,
            string state,
            string evidenceJson,
            CancellationToken cancellationToken = default);

        Task<int> RecalculateAsync(
            Guid enrollmentId,
            CancellationToken cancellationToken = default);
    }
}
