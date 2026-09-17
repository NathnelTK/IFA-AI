using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Quiz
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int PassingScorePercentage { get; set; } = 70;
        public int? LastScorePercentage { get; set; }
        public bool IsPassed { get; set; } = false;
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public Module? Module { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
