namespace IFA.Domain.Entities
{
    public class CourseCompletionRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EnrollmentId { get; set; }
        public Guid LearnerId { get; set; }
        public Guid CourseId { get; set; }
        public Guid ReleaseId { get; set; }
        public string SnapshotJson { get; set; } = "{}";
        public string EvidenceHash { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CourseCertificate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CompletionRecordId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Queued";
        public string? PdfStorageKey { get; set; }
        public string? RenderingErrorCode { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RenderedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public Guid? RevokedByAccountId { get; set; }
        public string? RevocationReason { get; set; }
    }

    public class CertificateRenderJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CertificateId { get; set; }
        public string State { get; set; } = "Queued";
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 5;
        public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
        public string? LeaseOwner { get; set; }
        public Guid? LeaseToken { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorDetail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
