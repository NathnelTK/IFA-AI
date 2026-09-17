using System;

namespace IFA.Domain.Entities
{
    public class SkillMetric
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public int CompetencyPercentage { get; set; }
        public string HexColor { get; set; } = "#2A9D68";
        public string IconName { get; set; } = "Terminal";
        public bool IsWeakArea { get; set; }
        public DateTime LastAssessedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Learner? Learner { get; set; }
    }
}
