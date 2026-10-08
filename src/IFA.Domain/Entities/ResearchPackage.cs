using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class ResearchPackage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? LearnerId { get; set; }
        public Guid? CourseId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string KeyConceptsJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Course? Course { get; set; }
        public ICollection<ResearchSource> Sources { get; set; } = new List<ResearchSource>();
    }
}
