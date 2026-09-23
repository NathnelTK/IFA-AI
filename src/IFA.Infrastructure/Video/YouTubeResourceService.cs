using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.Video
{
    /// <summary>
    /// YouTube educational video search and discovery service.
    ///
    /// =========================================================================
    /// ASSIGNED TO BACKEND TEAM
    /// =========================================================================
    /// RESPONSIBILITY:
    /// Query the YouTube Data API v3 (or configured video provider) to fetch
    /// curated educational videos matching the topic and the learner's preferred
    /// creators (e.g. Nick Chapsas, freeCodeCamp, Traversy Media, etc.).
    ///
    /// STEP-BY-STEP IMPLEMENTATION INSTRUCTIONS:
    /// 1. Configure "YouTube:ApiKey" in appsettings.json or read from .env.
    /// 2. If API Key is present, make an HTTP GET request to:
    ///    https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&videoEmbeddable=true&q={query}&key={ApiKey}&maxResults={maxResults}
    /// 3. If preferredCreator is specified, append it to the search query:
    ///    e.g. $"{topic} {preferredCreator}"
    /// 4. Parse the JSON response:
    ///    - videoId = item.id.videoId
    ///    - title = item.snippet.title
    ///    - channelTitle = item.snippet.channelTitle
    ///    - thumbnailUrl = item.snippet.thumbnails.high.url
    /// 5. Error handling:
    ///    - If quota is exceeded (HTTP 403 / quotaExceeded) or if no API key is set,
    ///      gracefully fall back to the curated in-memory video catalog below so the
    ///      hackathon demo NEVER breaks on stage!
    /// =========================================================================
    /// </summary>
    public class YouTubeResourceService : IYouTubeResourceService
    {
        private static readonly List<YouTubeVideoResource> CuratedDemoVideos = new()
        {
            new YouTubeVideoResource
            {
                VideoId = "3f_22k5iK8s",
                Title = "REST APIs in .NET 10 - Complete Architecture Guide",
                ChannelTitle = "Nick Chapsas",
                Duration = "24:15",
                ThumbnailUrl = "https://img.youtube.com/vi/3f_22k5iK8s/hqdefault.jpg"
            },
            new YouTubeVideoResource
            {
                VideoId = "BfEjDD8mWYg",
                Title = "C# & .NET Web API Tutorial for Beginners",
                ChannelTitle = "freeCodeCamp.org",
                Duration = "1:45:00",
                ThumbnailUrl = "https://img.youtube.com/vi/BfEjDD8mWYg/hqdefault.jpg"
            },
            new YouTubeVideoResource
            {
                VideoId = "g23gE_24c3s",
                Title = "Clean Architecture with ASP.NET Core & EF Core",
                ChannelTitle = "Amichai Mantinband",
                Duration = "38:40",
                ThumbnailUrl = "https://img.youtube.com/vi/g23gE_24c3s/hqdefault.jpg"
            }
        };

        public Task<List<YouTubeVideoResource>> SearchEducationalVideosAsync(
            string topic,
            string preferredCreator = "",
            int maxResults = 2,
            CancellationToken cancellationToken = default)
        {
            // TODO (Backend Team): Replace fallback catalog with live YouTube Data API v3 call.
            // Notice: Keep the fallback logic intact as a safety net if the quota expires!
            var query = topic.ToLower();
            var matches = CuratedDemoVideos
                .Where(v => string.IsNullOrWhiteSpace(preferredCreator) ||
                            v.ChannelTitle.Contains(preferredCreator, StringComparison.OrdinalIgnoreCase))
                .Take(maxResults)
                .ToList();

            if (!matches.Any())
            {
                matches = CuratedDemoVideos.Take(maxResults).ToList();
            }

            return Task.FromResult(matches);
        }
    }
}
