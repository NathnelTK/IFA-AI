namespace IFA.Domain.Entities
{
    public enum AssetStatus
    {
        PendingUpload,
        Quarantined,
        Scanning,
        Clean,
        Infected,
        Rejected,
        Deleted
    }

    public enum AssetVisibility
    {
        Private,
        EnrolledLearners,
        Public
    }

    public class Asset
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid OwnerAccountId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }
        public string ExpectedSha256 { get; set; } = string.Empty;
        public string? VerifiedSha256 { get; set; }
        public string QuarantineStorageKey { get; set; } = string.Empty;
        public string? CleanStorageKey { get; set; }
        public string? PublicStorageKey { get; set; }
        public AssetStatus Status { get; set; } = AssetStatus.PendingUpload;
        public AssetVisibility Visibility { get; set; } = AssetVisibility.Private;
        public string? RejectionCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ScannedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class UploadSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssetId { get; set; }
        public string State { get; set; } = "Pending";
        public string? MultipartUploadId { get; set; }
        public string UploadedPartsJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? AbortedAt { get; set; }
    }

    public class AssetDerivative
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssetId { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string MetadataJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AssetReference
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssetId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedByAccountId { get; set; }
    }

    public class AssetProcessingJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssetId { get; set; }
        public string Kind { get; set; } = "Scan";
        public string State { get; set; } = "Queued";
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 5;
        public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
        public string? LeaseOwner { get; set; }
        public Guid? LeaseToken { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorDetail { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
