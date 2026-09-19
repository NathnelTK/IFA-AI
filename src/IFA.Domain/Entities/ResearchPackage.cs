using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    // Output of the Research Agent (PR 2.2 / 2.3 / 2.4). Kept separate
    // from LearnerProfile on purpose: Course Architect (Model 2) should
    // be able to consume this without knowing whether it came from
    // Scholarxiv, a web search, or YouTube — that's the whole point of
    // normalizing it here instead of passing raw tool output downstream.
    public class ResearchPackage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerProfileId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<AcademicEvidence> AcademicSources { get; set; } = new List<AcademicEvidence>();
        public List<PracticalResource> PracticalResources { get; set; } = new List<PracticalResource>();
        public List<VideoResource> VideoResources { get; set; } = new List<VideoResource>();

        public LearnerProfile? LearnerProfile { get; set; }
    }

    // Maps 1:1 onto your existing ScholarxivPaperSummary DTO — this is
    // the "normalized" landing spot for that raw service result.
    public class AcademicEvidence
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResearchPackageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Doi { get; set; } = string.Empty;
        public string RelevanceNote { get; set; } = string.Empty; // why this matters to THIS learner
    }

    public class PracticalResource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResearchPackageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty; // "documentation", "tutorial", "article"
    }

    public class VideoResource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResearchPackageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string YouTubeVideoId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public bool FromPreferredChannel { get; set; } = false;
    }
}