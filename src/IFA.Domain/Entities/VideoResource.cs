namespace IFA.Domain.Entities;

public class VideoResource
{
    public Guid Id { get; set; }
    public Guid ResearchPackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public int ViewCount { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ResearchPackage ResearchPackage { get; set; } = null!;
}