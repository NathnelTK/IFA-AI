namespace IFA.Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public int SchemaVersion { get; set; } = 1;
        public string PayloadJson { get; set; } = "{}";
        public string? AggregateType { get; set; }
        public Guid? AggregateId { get; set; }
        public string DeduplicationKey { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 8;
        public string? LeaseOwner { get; set; }
        public Guid? LeaseToken { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
        public DateTime? LastHeartbeatAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? DeadLetteredAt { get; set; }
        public string? LastErrorCode { get; set; }
        public string? LastError { get; set; }
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public Guid? ActorLearnerId { get; set; }
    }
}
