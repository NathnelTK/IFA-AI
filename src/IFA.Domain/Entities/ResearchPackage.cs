using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    /// <summary>
    /// Output of the Research Orchestration phase (Model 1 + Scholarxiv + Web/YouTube).
    /// Grounded knowledge and media artifacts consumed by Course Architect (Model 2).
    /// </summary>
    public class ResearchPackage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerProfileId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<AcademicEvidence> AcademicSources { get; set; } = new();
        public List<PracticalResource> PracticalResources { get; set; } = new();
        public List<VideoResource> VideoResources { get; set; } = new();

        public LearnerProfile? LearnerProfile { get; set; }
    }

    public class AcademicEvidence
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResearchPackageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Doi { get; set; } = string.Empty;
        public string PublishedYear { get; set; } = string.Empty;
        public string RelevanceNote { get; set; } = string.Empty;
    }

    public class PracticalResource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResearchPackageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string SourceType { get; set; } = "documentation"; // documentation, tutorial, article
    }

    public class VideoResource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResearchPackageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string YouTubeVideoId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public bool FromPreferredChannel { get; set; }
    }
}
