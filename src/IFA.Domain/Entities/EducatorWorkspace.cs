namespace IFA.Domain.Entities
{
    public enum EducatorWorkspaceType
    {
        Individual,
        Organization,
        System
    }

    public enum EducatorWorkspaceStatus
    {
        Draft,
        PendingReview,
        Verified,
        Suspended
    }

    public class EducatorWorkspace
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public EducatorWorkspaceType Type { get; set; }
        public EducatorWorkspaceStatus Status { get; set; } = EducatorWorkspaceStatus.Draft;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public Guid? CreatedByAccountId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SuspendedAt { get; set; }
        public string? SuspensionReason { get; set; }
        public long AuthorizationVersion { get; set; }
    }

    public enum WorkspaceMembershipRole
    {
        Owner,
        Admin,
        Instructor
    }

    public enum WorkspaceMembershipState
    {
        Invited,
        Active,
        Suspended,
        Revoked
    }

    public class WorkspaceMembership
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid AccountId { get; set; }
        public WorkspaceMembershipRole Role { get; set; }
        public WorkspaceMembershipState State { get; set; } = WorkspaceMembershipState.Active;
        public Guid? InvitedByAccountId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

    public class WorkspaceInstructorProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid MembershipId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? PhotoUrl { get; set; }
        public string ExpertiseJson { get; set; } = "[]";
        public string LinksJson { get; set; } = "[]";
        public bool IsVisible { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WorkspaceInvitation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Email { get; set; } = string.Empty;
        public WorkspaceMembershipRole Role { get; set; } = WorkspaceMembershipRole.Instructor;
        public byte[] TokenHash { get; set; } = [];
        public Guid InvitedByAccountId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public Guid? AcceptedByAccountId { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

    public class CourseInstructorAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid MembershipId { get; set; }
        public string AssignmentRole { get; set; } = "Instructor";
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public Guid? AssignedByAccountId { get; set; }
    }

    public class EducatorApplication
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid ApplicantAccountId { get; set; }
        public string Status { get; set; } = "Draft";
        public string EvidenceJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public Guid? ReviewerAccountId { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string? DecisionReason { get; set; }
    }

    public class VerificationClaim
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;
        public string State { get; set; } = "Unverified";
        public byte[]? EvidenceHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Guid? ReviewerAccountId { get; set; }
    }

    public class WorkspaceEntitlement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Key { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public Guid? GrantedByAccountId { get; set; }
    }

    public class PublicSlugAlias
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TargetType { get; set; } = string.Empty;
        public Guid TargetId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RetiredAt { get; set; }
    }
}
