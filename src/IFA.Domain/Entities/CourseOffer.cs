namespace IFA.Domain.Entities
{
    public class CourseOffer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid ReleaseId { get; set; }
        public Guid? CohortId { get; set; }
        public int Version { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PurchaseType { get; set; } = "OneTime";
        public decimal PriceAmount { get; set; }
        public string Currency { get; set; } = "ETB";
        public bool IsActive { get; set; } = true;
        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableUntil { get; set; }
        public int? SeatLimit { get; set; }
        public string TermsJson { get; set; } = "{}";
        public Guid CreatedByAccountId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PaymentWebhookEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Provider { get; set; } = string.Empty;
        public string EventKey { get; set; } = string.Empty;
        public string PayloadHash { get; set; } = string.Empty;
        public string Status { get; set; } = "Received";
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}
