using System;

namespace IFA.Domain.Entities
{
    public class LearnerSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }
        public bool DailyReminders { get; set; } = true;
        public bool WeeklyDigest { get; set; } = true;
        public bool AssessmentResults { get; set; } = true;
        public bool CourseRecommendations { get; set; } = true;
        public string Theme { get; set; } = "system"; // light, dark, system
        public string AccentColor { get; set; } = "pine"; // pine, ember, ocean, violet
        public string FontSize { get; set; } = "medium"; // small, medium, large
        public bool CompactMode { get; set; } = false;
        public bool ReducedMotion { get; set; } = false;
        public string PreferredYouTubeChannelsJson { get; set; } = "[]";
        public string ExcludedYouTubeChannelsJson { get; set; } = "[]";
        public string PreferredTopicsJson { get; set; } = "[]";

        // Navigation property
        public Learner? Learner { get; set; }
    }
}
