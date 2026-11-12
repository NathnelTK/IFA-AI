namespace IFA.Domain.Entities
{
    public class AuditEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ActorLearnerId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public string MetadataJson { get; set; } = "{}";
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
