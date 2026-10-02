using System;

namespace IFA.Domain.Entities
{
    public class LearningActivity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }
        public string ActivityType { get; set; } = "lesson_completed"; // course_started, lesson_completed, quiz_passed, skill_improved, course_shared, course_published
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MetadataJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Learner? Learner { get; set; }
    }
}
