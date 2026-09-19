using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    // Output of the Understanding Agent (PR 2.1 / 2.5).
    // Every field except Goal is nullable/optional on purpose — the
    // acceptance criteria in your plan explicitly requires that missing
    // optional fields never break the downstream pipeline. Making them
    // non-nullable would force the agent to invent answers it doesn't
    // have, which is worse than leaving them empty.
    public class LearnerProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }

        public string Goal { get; set; } = string.Empty;       // required
        public string? SubjectTopic { get; set; }
        public string? CurrentLevel { get; set; }
        public string? TargetOutcome { get; set; }
        public int? AvailableStudyHoursPerWeek { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? PreferredLearningStyle { get; set; }
        public List<string> Constraints { get; set; } = new List<string>();
        public List<string> PreferredYouTubeChannels { get; set; } = new List<string>();
        public List<string> KnownStrengths { get; set; } = new List<string>();
        public List<string> KnownWeaknesses { get; set; } = new List<string>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Learner? Learner { get; set; }
    }
}