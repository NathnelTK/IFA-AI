using System;

namespace IFA.Domain.Entities
{
    /// <summary>
    /// A single learning-material link inside an educator project. Content is
    /// fetched and (best-effort) extracted as text — files are never stored.
    /// </summary>
    public class ProjectSource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public string Url { get; set; } = string.Empty;
        /// <summary>Pdf | Video | Image | Website | Article | Ppt | Other</summary>
        public string Type { get; set; } = "Website";
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>Best-effort extracted text used by analysis and generation.</summary>
        public string? ExtractedContent { get; set; }
        /// <summary>Pending | Extracted | Failed | Skipped</summary>
        public string Status { get; set; } = "Pending";
        public string? FailureReason { get; set; }
        /// <summary>User | Ai (AI research suggestion)</summary>
        public string AddedBy { get; set; } = "User";
        /// <summary>Whether the source participates in analysis/generation.</summary>
        public bool Included { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public EducatorProject? Project { get; set; }
    }
}
