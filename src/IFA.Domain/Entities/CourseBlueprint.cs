using System;

namespace IFA.Domain.Entities
{
    /// <summary>
    /// The AI-designed, educator-reviewable course plan. Stores modules and
    /// chapters as JSON with every mapped source URL + title so the course
    /// generator always knows exactly which material each section draws from.
    /// </summary>
    public class CourseBlueprint
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public string Json { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        public EducatorProject? Project { get; set; }
    }
}
