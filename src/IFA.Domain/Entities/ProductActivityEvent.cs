namespace IFA.Domain.Entities
{
    public class ProductActivityEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = string.Empty;
        public Guid? ActorAccountId { get; set; }
        public Guid? WorkspaceId { get; set; }
        public Guid? CourseId { get; set; }
        public Guid? ReleaseId { get; set; }
        public string? CorrelationId { get; set; }
        public string MetadataJson { get; set; } = "{}";
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
