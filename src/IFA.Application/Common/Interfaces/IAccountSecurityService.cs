using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public sealed record IssuedAccountToken(string RawToken, DateTime ExpiresAt);

    public interface IAccountTokenService
    {
        Task<IssuedAccountToken> IssueAsync(
            Guid accountId,
            AccountSecurityTokenPurpose purpose,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default);

        Task<Guid?> ConsumeAsync(
            string rawToken,
            AccountSecurityTokenPurpose purpose,
            CancellationToken cancellationToken = default);
    }

    public interface IApplicationSecretProtector
    {
        string Protect(string plaintext);
        string Unprotect(string protectedValue);
    }
}
