using System;

namespace IFA.Domain.Entities
{
    public class CourseShareInvite
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid SenderLearnerId { get; set; }
        public string ShareCode { get; set; } = string.Empty;
        public string? RecipientEmail { get; set; }
        public bool IsAccepted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Course? Course { get; set; }
        public Learner? SenderLearner { get; set; }
    }
}
