using System;

namespace IFA.Domain.Entities
{
    public class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ModuleId { get; set; }
        public int LessonNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ContentMarkdown { get; set; } = string.Empty;
        public int ReadingTimeMinutes { get; set; } = 12;

        // Multimedia & Research Grounding (Scholarxiv + YouTube integration)
        public string? YouTubeVideoId { get; set; }
        public string? YouTubeVideoTitle { get; set; }
        public string? ScholarxivCitationDoi { get; set; }
        public string? ScholarxivPaperTitle { get; set; }

        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Module? Module { get; set; }
    }
}
