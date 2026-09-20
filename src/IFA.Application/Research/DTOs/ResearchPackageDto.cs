using System.Collections.Generic;
using IFA.Application.Learning.DTOs;

namespace IFA.Application.Research.DTOs
{
    /// <summary>
    /// Contract produced by the Research Orchestration layer (Model 1 + Scholarxiv + Web/YouTube).
    /// Provides grounded academic evidence and multimedia candidates for Model 2 (Course Architect).
    /// </summary>
    public class ResearchPackageDto
    {
        public LearnerProfileDto? LearnerContext { get; set; }
        public List<string> ResearchFindings { get; set; } = new();
        public List<string> LearningObjectives { get; set; } = new();
        public List<string> RecommendedSequence { get; set; } = new();
        public List<AcademicSourceDto> AcademicSources { get; set; } = new();
        public List<ExternalResourceDto> ExternalResources { get; set; } = new();
        public List<VideoResourceDto> VideoResources { get; set; } = new();
    }

    public class AcademicSourceDto
    {
        public string Title { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string Doi { get; set; } = string.Empty;
        public string PublishedYear { get; set; } = string.Empty;
        public string RelevanceNote { get; set; } = string.Empty;
    }

    public class ExternalResourceDto
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string SourceType { get; set; } = "documentation"; // documentation, tutorial, article
        public string Summary { get; set; } = string.Empty;
    }

    public class VideoResourceDto
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ChannelTitle { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string EmbedUrl => $"https://www.youtube.com/embed/{VideoId}";
        public bool IsPreferredCreator { get; set; }
    }
}
