using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    /// <summary>
    /// Output of the Learning Advisor & Research Orchestrator (Model 1).
    /// Captures the learner's conversational intake, goals, constraints,
    /// and learning preferences.
    /// </summary>
    public class LearnerProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }

        public string Goal { get; set; } = string.Empty;
        public string? SubjectTopic { get; set; }
        public string? CurrentLevel { get; set; }
        public string? TargetOutcome { get; set; }
        public int? AvailableStudyHoursPerWeek { get; set; }
        public string? PreferredLanguage { get; set; } = "en";
        public string? PreferredLearningStyle { get; set; }
        public List<string> Constraints { get; set; } = new();
        public List<string> PreferredYouTubeChannels { get; set; } = new();
        public List<string> KnownStrengths { get; set; } = new();
        public List<string> KnownWeaknesses { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Learner? Learner { get; set; }
    }
}
