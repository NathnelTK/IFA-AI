using System;

namespace IFA.Domain.Entities
{
    public class LiveSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow.AddDays(1);
        public int DurationMinutes { get; set; } = 60;
        public string TimeZone { get; set; } = "UTC";
        public string MeetingUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled"; // Scheduled | Live | Completed | Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Course? Course { get; set; }
    }
}
