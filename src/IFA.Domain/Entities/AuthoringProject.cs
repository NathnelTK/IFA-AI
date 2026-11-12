namespace IFA.Domain.Entities
{
    public enum AuthoringMode
    {
        Manual,
        Prompt,
        Materials,
        Template,
        Voice
    }

    public enum AuthoringProjectState
    {
        Draft,
        Analyzing,
        AwaitingApproval,
        Generating,
        Reviewing,
        PublishReady,
        Published,
        Failed
    }

    public class AuthoringProject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid CourseId { get; set; }
        public Guid? CreatedByAccountId { get; set; }
        public Guid? LegacyEducatorProjectId { get; set; }
        public AuthoringMode Mode { get; set; }
        public AuthoringProjectState State { get; set; } = AuthoringProjectState.Draft;
        public string Title { get; set; } = string.Empty;
        public string? FailureCode { get; set; }
        public string? FailureDetail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public long Version { get; set; }
    }

    public class CourseGenerationRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AuthoringProjectId { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string TargetAudience { get; set; } = "All Learners";
        public string LearningObjectivesJson { get; set; } = "[]";
        public string ConstraintsJson { get; set; } = "{}";
        public string? TemplateId { get; set; }
        public int? TemplateVersion { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum AuthoringSourceKind
    {
        Url,
        Asset,
        Text
    }

    public class AuthoringSource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AuthoringProjectId { get; set; }
        public Guid? LegacyProjectSourceId { get; set; }
        public AuthoringSourceKind Kind { get; set; }
        public string Locator { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string MetadataJson { get; set; } = "{}";
        public string? ExtractedTextReference { get; set; }
        public Guid? AssetId { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum AuthoringJobState
    {
        Queued,
        Running,
        Waiting,
        Succeeded,
        Failed,
        Cancelled
    }

    public class AuthoringJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AuthoringProjectId { get; set; }
        public Guid? ParentJobId { get; set; }
        public string Kind { get; set; } = string.Empty;
        public AuthoringJobState State { get; set; } = AuthoringJobState.Queued;
        public string PayloadJson { get; set; } = "{}";
        public string ResultJson { get; set; } = "{}";
        public int ProgressPercent { get; set; }
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 3;
        public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
        public string? LeaseOwner { get; set; }
        public Guid? LeaseToken { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorDetail { get; set; }
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public Guid? ActorAccountId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }

    public class LegacyProjectAlias
    {
        public Guid LegacyProjectId { get; set; }
        public Guid AuthoringProjectId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CourseTemplateVersion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TemplateKey { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string DefinitionJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RetiredAt { get; set; }
    }
}
