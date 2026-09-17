using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    public class YouTubeVideoResource
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ChannelTitle { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string EmbedUrl => $"https://www.youtube.com/embed/{VideoId}";
    }

    public interface IYouTubeResourceService
    {
        Task<List<YouTubeVideoResource>> SearchEducationalVideosAsync(string topic, string preferredCreator = "", int maxResults = 2, CancellationToken cancellationToken = default);
    }
}
