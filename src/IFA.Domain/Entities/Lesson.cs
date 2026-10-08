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

        public string? YouTubeVideoId { get; set; }
        public string? YouTubeVideoTitle { get; set; }
        public string? ScholarxivCitationDoi { get; set; }
        public string? ScholarxivPaperTitle { get; set; }

        // IsCompleted removed — see LessonProgress. Completion is
        // per-learner, and a Lesson can be viewed by many learners
        // via a shared Course, so it can't live here.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Module? Module { get; set; }
    }
}