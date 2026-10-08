using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class LearnerProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }
        public string LearningGoal { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string CurrentLevel { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced
        public string TargetOutcome { get; set; } = string.Empty;
        public int WeeklyStudyHours { get; set; } = 5;
        public string PreferredLanguage { get; set; } = "en";
        public string LearningStyle { get; set; } = "Hands-on"; // Hands-on, Visual, Theoretical, Audio
        public string Constraints { get; set; } = string.Empty;
        public string PreferredYouTubeChannelsJson { get; set; } = "[]";
        public string KnownStrengthsJson { get; set; } = "[]";
        public string KnownWeaknessesJson { get; set; } = "[]";
        public string Requirements { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Learner? Learner { get; set; }
    }
}
