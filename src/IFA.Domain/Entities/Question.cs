using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid QuizId { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectOptionIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string TargetSkillName { get; set; } = string.Empty; // e.g., "Authentication", "Databases"
        public string BloomTaxonomyLevel { get; set; } = "Analyze"; // Remember, Understand, Apply, Analyze, Evaluate

        // Navigation property
        public Quiz? Quiz { get; set; }
    }
}
