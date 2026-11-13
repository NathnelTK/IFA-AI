using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public sealed record IssuedOpaqueSession(
        Guid SessionId,
        string SessionToken,
        string CsrfToken,
        DateTime ExpiresAt);

    public sealed record ValidatedOpaqueSession(
        Guid SessionId,
        Guid AccountId,
        string Name,
        string Email,
        string RegistrationIntent,
        int MfaLevel,
        IReadOnlyList<AccountRoleKind> Roles,
        IReadOnlyList<Guid> WorkspaceIds);

    public interface IOpaqueSessionService
    {
        Task<IssuedOpaqueSession> IssueAsync(
            Guid accountId,
            string hostScope,
            AuthSiteScope siteScope,
            int mfaLevel,
            string? userAgent,
            string? ipAddress,
            Guid? parentSessionId = null,
            CancellationToken cancellationToken = default);

        Task<ValidatedOpaqueSession?> ValidateAsync(
            string rawToken,
            string hostScope,
            AuthSiteScope siteScope,
            string? csrfToken,
            bool requireCsrf,
            CancellationToken cancellationToken = default);

        Task<IssuedOpaqueSession?> ExchangeLegacyTokenAsync(
            string rawLegacyToken,
            Guid accountId,
            string hostScope,
            AuthSiteScope siteScope,
            string? userAgent,
            string? ipAddress,
            CancellationToken cancellationToken = default);

        Task RevokeAsync(
            Guid sessionId,
            string reason,
            CancellationToken cancellationToken = default);
    }
}
