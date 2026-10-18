using System;

namespace IFA.Domain.Entities
{
    /// <summary>
    /// A payment for enrolling in a paid course, processed through a local
    /// payment aggregator (Chapa sandbox by default, Telebirr supported).
    /// </summary>
    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }
        public Guid CourseId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ETB";
        /// <summary>Chapa | Telebirr</summary>
        public string Provider { get; set; } = "Chapa";
        public string ProviderTxRef { get; set; } = string.Empty;
        /// <summary>Pending | Completed | Failed</summary>
        public string Status { get; set; } = "Pending";
        public string? CheckoutUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public Learner? Learner { get; set; }
        public Course? Course { get; set; }
    }
}
