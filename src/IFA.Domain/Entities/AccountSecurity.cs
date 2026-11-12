namespace IFA.Domain.Entities
{
    public enum AuthSiteScope
    {
        Auth,
        Public,
        App
    }

    public class AuthSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public byte[] TokenHash { get; set; } = [];
        public byte[] CsrfTokenHash { get; set; } = [];
        public string HostScope { get; set; } = string.Empty;
        public AuthSiteScope SiteScope { get; set; }
        public int MfaLevel { get; set; }
        public long AuthorizationVersionSnapshot { get; set; }
        public Guid? ParentSessionId { get; set; }
        public string? UserAgentHash { get; set; }
        public string? IpAddressHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }

        public Learner? Account { get; set; }
    }

    public class LegacyTokenExchange
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public byte[] TokenHash { get; set; } = [];
        public Guid AccountId { get; set; }
        public Guid SessionId { get; set; }
        public DateTime ConsumedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public enum AccountSecurityTokenPurpose
    {
        EmailVerification,
        PasswordReset
    }

    public class AccountSecurityToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public AccountSecurityTokenPurpose Purpose { get; set; }
        public byte[] TokenHash { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
    }

    public class AccountMfaCredential
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public string ProtectedSecret { get; set; } = string.Empty;
        public int ProtectionKeyVersion { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        public DateTime? DisabledAt { get; set; }
    }

    public class AccountRecoveryCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public byte[] CodeHash { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
    }

    public class OAuthAuthorizationCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public Guid SessionId { get; set; }
        public byte[] CodeHash { get; set; } = [];
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string CodeChallenge { get; set; } = string.Empty;
        public string CodeChallengeMethod { get; set; } = "S256";
        public string StateHash { get; set; } = string.Empty;
        public AuthSiteScope TargetSiteScope { get; set; }
        public int MfaLevel { get; set; }
        public string TargetHost { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
    }

    public class MfaLoginChallenge
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public byte[] TokenHash { get; set; } = [];
        public string HostScope { get; set; } = string.Empty;
        public AuthSiteScope SiteScope { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
    }

    public class InternalRequestReplay
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public byte[] NonceHash { get; set; } = [];
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
    }
}
