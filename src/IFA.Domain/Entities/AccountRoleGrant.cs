namespace IFA.Domain.Entities
{
    public enum AccountRoleKind
    {
        Learner,
        Educator,
        PlatformReviewer,
        PlatformModerator,
        PlatformAdmin
    }

    public class AccountRoleGrant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public AccountRoleKind Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public Guid? GrantedByAccountId { get; set; }
        public DateTime? RevokedAt { get; set; }
        public Guid? RevokedByAccountId { get; set; }
        public string? Reason { get; set; }

        public Learner? Account { get; set; }
    }
}
