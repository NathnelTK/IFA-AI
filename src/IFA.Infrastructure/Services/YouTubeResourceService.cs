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

        public YouTubeResourceService(HttpClient httpClient, IConfiguration configuration, ILogger<YouTubeResourceService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<YouTubeVideoResource>> SearchEducationalVideosAsync(string topic, string preferredCreator = "", int maxResults = 2, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["YOUTUBE_API_KEY"] ?? _configuration["Ai:YouTube:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "change_me")
            {
                _logger.LogInformation("Skipping YouTube search because YOUTUBE_API_KEY is not configured.");
                return new List<YouTubeVideoResource>();
            }

            var searchQuery = Uri.EscapeDataString($"{topic} {preferredCreator}".Trim());
            var endpoint = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={searchQuery}&type=video&key={Uri.EscapeDataString(apiKey)}&maxResults={maxResults}";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var response = await _httpClient.GetAsync(endpoint, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("YouTube search failed with HTTP status {StatusCode}.", response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(content);
            var results = new List<YouTubeVideoResource>();
            if (doc.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (!item.TryGetProperty("id", out var idObj) ||
                        !idObj.TryGetProperty("videoId", out var vidId) ||
                        !item.TryGetProperty("snippet", out var snippet))
                    {
                        continue;
                    }

                    var thumbnails = snippet.GetProperty("thumbnails");
                    var thumbnail = thumbnails.TryGetProperty("high", out var high)
                        ? high.GetProperty("url").GetString()
                        : null;

                    results.Add(new YouTubeVideoResource
                    {
                        VideoId = vidId.GetString() ?? string.Empty,
                        Title = snippet.GetProperty("title").GetString() ?? string.Empty,
                        ChannelTitle = snippet.GetProperty("channelTitle").GetString() ?? string.Empty,
                        Duration = "Video",
                        ThumbnailUrl = thumbnail ?? string.Empty
                    });
                }
            }

            return results;
        }
    }
}
