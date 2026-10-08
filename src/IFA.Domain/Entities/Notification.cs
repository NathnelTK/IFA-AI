using System;

namespace IFA.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }
        public string Type { get; set; } = "achievement"; // achievement, recommendation, course_update, social
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Learner? Learner { get; set; }
    }
}
