using System;

namespace IFA.Domain.Entities
{
    public class ResearchSource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResearchPackageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string SourceType { get; set; } = "Academic"; // Academic, Video, Documentation
        public string Authors { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public double RelevanceScore { get; set; } = 0.95;
        public int? PublishedYear { get; set; }

        // Navigation property
        public ResearchPackage? ResearchPackage { get; set; }
    }
}
