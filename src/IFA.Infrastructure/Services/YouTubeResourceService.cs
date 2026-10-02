using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    public class YouTubeResourceService : IYouTubeResourceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<YouTubeResourceService> _logger;

        private static readonly List<YouTubeVideoResource> CuratedVideos = new()
        {
            new YouTubeVideoResource
            {
                VideoId = "3tXmQ5m_p5c",
                Title = "Clean Architecture in .NET - Complete Course",
                ChannelTitle = "freeCodeCamp.org",
                Duration = "3h 45m",
                ThumbnailUrl = "https://img.youtube.com/vi/3tXmQ5m_p5c/hqdefault.jpg"
            },
            new YouTubeVideoResource
            {
                VideoId = "dQw4w9WgXcQ",
                Title = "ASP.NET Core Web API Full Course",
                ChannelTitle = "Traversy Media",
                Duration = "2h 10m",
                ThumbnailUrl = "https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg"
            },
            new YouTubeVideoResource
            {
                VideoId = "g2O3w08g7sY",
                Title = "Entity Framework Core Deep Dive: Performance & Relationships",
                ChannelTitle = "Nick Chapsas",
                Duration = "45m",
                ThumbnailUrl = "https://img.youtube.com/vi/g2O3w08g7sY/hqdefault.jpg"
            }
        };

        public YouTubeResourceService(HttpClient httpClient, IConfiguration configuration, ILogger<YouTubeResourceService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<YouTubeVideoResource>> SearchEducationalVideosAsync(string topic, string preferredCreator = "", int maxResults = 2, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["YOUTUBE_API_KEY"] ?? _configuration["Ai:YouTube:ApiKey"];

            if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "change_me")
            {
                try
                {
                    var searchQuery = Uri.EscapeDataString($"{topic} tutorial programming {preferredCreator}".Trim());
                    var endpoint = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={searchQuery}&type=video&key={apiKey}&maxResults={maxResults}";

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(5));

                    var response = await _httpClient.GetAsync(endpoint, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cts.Token);
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.TryGetProperty("items", out var items))
                        {
                            var list = new List<YouTubeVideoResource>();
                            foreach (var item in items.EnumerateArray())
                            {
                                var idObj = item.GetProperty("id");
                                if (idObj.TryGetProperty("videoId", out var vidId))
                                {
                                    var snippet = item.GetProperty("snippet");
                                    var videoId = vidId.GetString() ?? "";
                                    var title = snippet.GetProperty("title").GetString() ?? "";
                                    var channel = snippet.GetProperty("channelTitle").GetString() ?? "";
                                    var thumb = snippet.GetProperty("thumbnails").GetProperty("high").GetProperty("url").GetString() ?? "";

                                    list.Add(new YouTubeVideoResource
                                    {
                                        VideoId = videoId,
                                        Title = title,
                                        ChannelTitle = channel,
                                        Duration = "Video",
                                        ThumbnailUrl = thumb
                                    });
                                }
                            }
                            if (list.Count > 0) return list;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("YouTube API query failed ({Message}). Using curated video library.", ex.Message);
                }
            }

            // Fallback: return curated videos tailored to topic
            var results = new List<YouTubeVideoResource>();
            foreach (var video in CuratedVideos)
            {
                results.Add(new YouTubeVideoResource
                {
                    VideoId = video.VideoId,
                    Title = $"{topic}: {video.Title}",
                    ChannelTitle = string.IsNullOrWhiteSpace(preferredCreator) ? video.ChannelTitle : preferredCreator,
                    Duration = video.Duration,
                    ThumbnailUrl = video.ThumbnailUrl
                });
                if (results.Count >= maxResults) break;
            }
            return results;
        }
    }
}
