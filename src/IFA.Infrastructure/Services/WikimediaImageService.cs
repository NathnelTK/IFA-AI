using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Image/diagram research backed by the Wikipedia/Wikimedia API. It needs no
    /// API key, returns real hosted image URLs (so nothing is hallucinated), and
    /// is used by the research stage to illustrate generated lessons.
    /// </summary>
    public class WikimediaImageService : IImageResourceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WikimediaImageService> _logger;

        public WikimediaImageService(HttpClient httpClient, ILogger<WikimediaImageService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<ImageResource>> SearchImagesAsync(string query, int maxResults = 3, CancellationToken cancellationToken = default)
        {
            var results = new List<ImageResource>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            var url =
                "https://en.wikipedia.org/w/api.php?action=query&format=json&redirects=1" +
                "&prop=pageimages|pageterms&piprop=original|thumbnail&pithumbsize=1000&pilimit=" + maxResults +
                "&generator=search&gsrnamespace=0&gsrlimit=" + maxResults +
                "&gsrsearch=" + Uri.EscapeDataString(query);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                // Wikimedia requires a descriptive User-Agent or it returns 403.
                request.Headers.UserAgent.ParseAdd("IFA-Learning/1.0 (https://ifa.ai; educational course generation)");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Wikimedia image search failed: {Status} for '{Query}'", response.StatusCode, query);
                    return results;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("query", out var queryEl) ||
                    !queryEl.TryGetProperty("pages", out var pages) ||
                    pages.ValueKind != JsonValueKind.Object)
                {
                    return results;
                }

                foreach (var pageProp in pages.EnumerateObject())
                {
                    var page = pageProp.Value;

                    string? imageUrl = null;
                    if (page.TryGetProperty("original", out var original) &&
                        original.TryGetProperty("source", out var originalSrc))
                    {
                        imageUrl = originalSrc.GetString();
                    }
                    else if (page.TryGetProperty("thumbnail", out var thumb) &&
                             thumb.TryGetProperty("source", out var thumbSrc))
                    {
                        imageUrl = thumbSrc.GetString();
                    }

                    if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                    // Skip tiny/icon SVGs that rarely help as lesson illustrations.
                    if (imageUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) &&
                        imageUrl.Contains("/40px-", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var title = page.TryGetProperty("title", out var t) ? t.GetString() ?? query : query;
                    results.Add(new ImageResource
                    {
                        Title = title!,
                        ImageUrl = imageUrl!,
                        Source = "Wikimedia"
                    });

                    if (results.Count >= maxResults) break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wikimedia image search errored for '{Query}'.", query);
            }

            return results;
        }
    }
}
