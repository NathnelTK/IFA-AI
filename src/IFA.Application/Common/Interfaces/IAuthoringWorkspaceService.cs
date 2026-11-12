namespace IFA.Application.Common.Interfaces
{
    public interface IAuthoringWorkspaceService
    {
        Task<Guid?> ResolveForCreationAsync(
            Guid accountId,
            Guid? requestedWorkspaceId,
            CancellationToken cancellationToken = default);
    }
}
