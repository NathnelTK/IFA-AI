using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using IFA.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Multi-provider academic research aggregator. Queries OpenAlex,
    /// Semantic Scholar, arXiv, Tavily (only when an API key is configured)
    /// and Wikimedia Commons in parallel, merges the results round-robin,
    /// de-duplicates by URL and never fails as a whole: a provider that
    /// errors is skipped (logged) and the rest still answer.
    /// </summary>
    public class AcademicResearchService : IAcademicResearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IImageResourceService _imageService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AcademicResearchService> _logger;

        private const int PerProviderTimeoutSeconds = 15;

        public AcademicResearchService(
            HttpClient httpClient,
            IImageResourceService imageService,
            IConfiguration configuration,
            ILogger<AcademicResearchService> logger)
        {
            _httpClient = httpClient;
            _imageService = imageService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<AcademicSearchResult>> SearchAsync(string query, int maxResults = 8, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query) || maxResults <= 0)
            {
                return new List<AcademicSearchResult>();
            }

            maxResults = Math.Min(maxResults, 25);
            var cleanQuery = query.Trim();

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(TimeSpan.FromSeconds(PerProviderTimeoutSeconds + 5));
            var token = linked.Token;

            var perProvider = Math.Max(3, (int)Math.Ceiling(maxResults / 2.0));

            var tasks = new List<Task<List<AcademicSearchResult>>>
            {
                RunProviderAsync("OpenAlex", () => SearchOpenAlexAsync(cleanQuery, perProvider, token)),
                RunProviderAsync("SemanticScholar", () => SearchSemanticScholarAsync(cleanQuery, perProvider, token)),
                RunProviderAsync("arXiv", () => SearchArxivAsync(cleanQuery, perProvider, token))
            };

            if (!string.IsNullOrWhiteSpace(_configuration["TAVILY_API_KEY"]))
            {
                tasks.Add(RunProviderAsync("Tavily", () => SearchTavilyAsync(cleanQuery, perProvider, token)));
            }

            tasks.Add(RunProviderAsync("Wikimedia", () => SearchWikimediaAsync(cleanQuery, perProvider, token)));

            await Task.WhenAll(tasks);

            // Round-robin merge so no single provider dominates the top results,
            // then de-duplicate by URL.
            var buckets = tasks.Select(t => t.Result).Where(b => b.Count > 0).ToList();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var merged = new List<AcademicSearchResult>();

            for (var i = 0; merged.Count < maxResults && buckets.Any(b => i < b.Count); i++)
            {
                foreach (var bucket in buckets)
                {
                    if (merged.Count >= maxResults) break;
                    if (i >= bucket.Count) continue;

                    var result = bucket[i];
                    if (string.IsNullOrWhiteSpace(result.Url)) continue;
                    if (!seen.Add(result.Url)) continue;

                    merged.Add(result);
                }
            }

            return merged;
        }

        private async Task<List<AcademicSearchResult>> RunProviderAsync(
            string name, Func<Task<List<AcademicSearchResult>>> search)
        {
            try
            {
                return await search();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Academic research provider '{Provider}' failed; skipping it.", name);
                return new List<AcademicSearchResult>();
            }
        }

        private async Task<string> GetStringAsync(string url, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("IFA-AI/1.0 (educational research aggregation)");
            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }

        private async Task<List<AcademicSearchResult>> SearchOpenAlexAsync(string query, int max, CancellationToken ct)
        {
            var url = $"https://api.openalex.org/works?search={Uri.EscapeDataString(query)}&per-page={max}";
            var json = await GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var results = new List<AcademicSearchResult>();

            if (doc.RootElement.TryGetProperty("results", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var title = item.TryGetProperty("display_name", out var t) ? t.GetString() : null;
                    var doi = item.TryGetProperty("doi", out var d) ? d.GetString() : null;
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    var authors = new List<string>();
                    if (item.TryGetProperty("authorships", out var authorships))
                    {
                        foreach (var a in authorships.EnumerateArray())
                        {
                            if (a.TryGetProperty("author", out var author) &&
                                author.TryGetProperty("display_name", out var name))
                            {
                                var n = name.GetString();
                                if (!string.IsNullOrWhiteSpace(n)) authors.Add(n!);
                            }
                            if (authors.Count >= 4) break;
                        }
                    }

                    DateTime? published = null;
                    if (item.TryGetProperty("publication_date", out var pd) &&
                        pd.ValueKind == JsonValueKind.String &&
                        DateTime.TryParse(pd.GetString(), out var parsedDate))
                    {
                        published = parsedDate;
                    }

                    results.Add(new AcademicSearchResult
                    {
                        Title = title!,
                        Url = string.IsNullOrWhiteSpace(doi)
                            ? $"https://openalex.org/{(item.TryGetProperty("id", out var id) ? id.GetString() : "")}"
                            : doi!,
                        Summary = item.TryGetProperty("primary_location", out var loc) &&
                              loc.ValueKind == JsonValueKind.Object &&
                              loc.TryGetProperty("source", out var src) &&
                              src.ValueKind == JsonValueKind.Object &&
                              src.TryGetProperty("display_name", out var srcName)
                                ? $"Published in {srcName.GetString()}."
                                : string.Empty,
                        Source = "OpenAlex",
                        PublishedDate = published,
                        Authors = string.Join(", ", authors)
                    });
                }
            }

            return results;
        }

        private async Task<List<AcademicSearchResult>> SearchSemanticScholarAsync(string query, int max, CancellationToken ct)
        {
            var url = "https://api.semanticscholar.org/graph/v1/paper/search" +
                      $"?query={Uri.EscapeDataString(query)}&limit={max}&fields=title,abstract,url,year,authors";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("IFA-AI/1.0");

            var apiKey = _configuration["SEMANTIC_SCHOLAR_API_KEY"];
            if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "change_me")
            {
                request.Headers.TryAddWithoutValidation("x-api-key", apiKey);
            }

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var results = new List<AcademicSearchResult>();

            if (doc.RootElement.TryGetProperty("data", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    var paperUrl = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                    DateTime? published = null;
                    if (item.TryGetProperty("year", out var y) && y.ValueKind == JsonValueKind.Number &&
                        y.TryGetInt32(out var year))
                    {
                        published = new DateTime(year, 1, 1);
                    }

                    var authors = new List<string>();
                    if (item.TryGetProperty("authors", out var authorArr))
                    {
                        foreach (var a in authorArr.EnumerateArray())
                        {
                            var n = a.TryGetProperty("name", out var name) ? name.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(n)) authors.Add(n!);
                            if (authors.Count >= 4) break;
                        }
                    }

                    results.Add(new AcademicSearchResult
                    {
                        Title = title!,
                        Url = string.IsNullOrWhiteSpace(paperUrl)
                            ? $"https://semanticscholar.org/search?q={Uri.EscapeDataString(title!)}"
                            : paperUrl!,
                        Summary = item.TryGetProperty("abstract", out var ab) ? Truncate(ab.GetString(), 800) : null,
                        Source = "SemanticScholar",
                        PublishedDate = published,
                        Authors = string.Join(", ", authors)
                    });
                }
            }

            return results;
        }

        private async Task<List<AcademicSearchResult>> SearchArxivAsync(string query, int max, CancellationToken ct)
        {
            var url = $"http://export.arxiv.org/api/query?search_query=all:{Uri.EscapeDataString(query)}&max_results={max}";
            var xml = await GetStringAsync(url, ct);
            var doc = XDocument.Parse(xml);
            var ns = "http://www.w3.org/2005/Atom";
            var results = new List<AcademicSearchResult>();

            foreach (var entry in doc.Descendants(XName.Get("entry", ns)))
            {
                var title = entry.Element(XName.Get("title", ns))?.Value?.Trim();
                var link = entry.Elements(XName.Get("link", ns))
                    .FirstOrDefault(l => (string?)l.Attribute("type") == "text/html")
                    ?.Attribute("href")?.Value
                    ?? entry.Element(XName.Get("id", ns))?.Value;
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link)) continue;

                var summary = entry.Element(XName.Get("summary", ns))?.Value?.Trim();
                var authors = entry.Elements(XName.Get("author", ns))
                    .Select(a => a.Element(XName.Get("name", ns))?.Value?.Trim())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Take(4)
                    .ToList();
                var published = DateTime.TryParse(entry.Element(XName.Get("published", ns))?.Value, out var p)
                    ? p
                    : (DateTime?)null;

                results.Add(new AcademicSearchResult
                {
                    Title = title!,
                    Url = link!,
                    Summary = Truncate(summary, 800),
                    Source = "arXiv",
                    PublishedDate = published,
                    Authors = string.Join(", ", authors)
                });
            }

            return results;
        }

        private async Task<List<AcademicSearchResult>> SearchTavilyAsync(string query, int max, CancellationToken ct)
        {
            var apiKey = _configuration["TAVILY_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey)) return new List<AcademicSearchResult>();

            var payload = JsonSerializer.Serialize(new { api_key = apiKey, query, max_results = max });
            using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync("https://api.tavily.com/search", content, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var results = new List<AcademicSearchResult>();

            if (doc.RootElement.TryGetProperty("results", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                    var url2 = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url2)) continue;

                    results.Add(new AcademicSearchResult
                    {
                        Title = title!,
                        Url = url2!,
                        Summary = item.TryGetProperty("content", out var c) ? Truncate(c.GetString(), 800) : null,
                        Source = "Tavily"
                    });
                }
            }

            return results;
        }

        private async Task<List<AcademicSearchResult>> SearchWikimediaAsync(string query, int max, CancellationToken ct)
        {
            var images = await _imageService.SearchImagesAsync(query, Math.Min(max, 6), ct);
            return images.Select(i => new AcademicSearchResult
            {
                Title = i.Title,
                Url = i.ImageUrl,
                Summary = $"Image resource from {i.Source}.",
                Source = "Wikimedia"
            }).ToList();
        }

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? null
            : value.Length <= max ? value
            : value[..max] + "…";
    }
}
