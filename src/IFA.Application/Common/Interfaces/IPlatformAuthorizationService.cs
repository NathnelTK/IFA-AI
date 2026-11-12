using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IPlatformAuthorizationService
    {
        Task<bool> HasAccountRoleAsync(
            Guid accountId,
            AccountRoleKind role,
            CancellationToken cancellationToken = default);

        Task<bool> CanManageWorkspaceAsync(
            Guid accountId,
            Guid workspaceId,
            CancellationToken cancellationToken = default);

        Task<bool> CanManageCourseAsync(
            Guid accountId,
            Guid courseId,
            CancellationToken cancellationToken = default);

        Task<bool> CanManageProjectAsync(
            Guid accountId,
            Guid projectId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Guid>> GetManageableCourseIdsAsync(
            Guid accountId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Guid>> GetManageableProjectIdsAsync(
            Guid accountId,
            CancellationToken cancellationToken = default);
    }
}
