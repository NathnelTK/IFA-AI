using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Best-effort content extraction for project source links. Files are never
    /// stored — only extracted text (capped) is persisted. Never throws: every
    /// source ends with a terminal Status (Extracted or Failed) plus a
    /// FailureReason when it failed or is metadata-only.
    /// </summary>
    public class SourceIngestionService : ISourceIngestionService
    {
        private const int MaxExtractedChars = 12000;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILlmGateway _llmGateway;
        private readonly ILogger<SourceIngestionService> _logger;

        public SourceIngestionService(
            IHttpClientFactory httpClientFactory,
            ILlmGateway llmGateway,
            ILogger<SourceIngestionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _llmGateway = llmGateway;
            _logger = logger;
        }

        public async Task IngestAsync(ProjectSource source, CancellationToken ct = default)
        {
            try
            {
                switch (source.Type)
                {
                    case "Video":
                        await IngestVideoAsync(source, ct);
                        break;
                    case "Image":
                        await IngestImageAsync(source, ct);
                        break;
                    case "Pdf":
                        await IngestPdfAsync(source, ct);
                        break;
                    default:
                        // Website | Article | Ppt | Other — best-effort plain fetch.
                        await IngestWebsiteAsync(source, ct);
                        break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                source.Status = "Pending"; // request aborted — leave for a later retry
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Source ingestion failed for {Url}.", source.Url);
                source.Status = "Failed";
                source.FailureReason = Truncate($"Extraction failed: {ex.Message}", 500);
            }
        }

        private async Task IngestWebsiteAsync(ProjectSource source, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("SourceIngestion");
            using var response = await client.GetAsync(source.Url, ct);
            if (!response.IsSuccessStatusCode)
            {
                source.Status = "Failed";
                source.FailureReason = Truncate($"The link returned HTTP {(int)response.StatusCode}.", 500);
                return;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            {
                var pdfBytes = await response.Content.ReadAsByteArrayAsync(ct);
                var pdfText = ExtractPdfText(pdfBytes);
                if (!string.IsNullOrWhiteSpace(pdfText))
                {
                    source.ExtractedContent = Truncate(pdfText, MaxExtractedChars);
                    source.Status = "Extracted";
                    return;
                }
            }

            var text = StripHtml(raw);
            if (string.IsNullOrWhiteSpace(text))
            {
                source.Status = "Failed";
                source.FailureReason = "No readable text could be extracted from the link.";
                return;
            }

            source.ExtractedContent = Truncate(text, MaxExtractedChars);
            source.Status = "Extracted";
            source.FailureReason = null;
        }

        private async Task IngestPdfAsync(ProjectSource source, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("SourceIngestion");
            using var response = await client.GetAsync(source.Url, ct);
            if (!response.IsSuccessStatusCode)
            {
                source.Status = "Failed";
                source.FailureReason = Truncate($"The PDF link returned HTTP {(int)response.StatusCode}.", 500);
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var text = ExtractPdfText(bytes);
            if (string.IsNullOrWhiteSpace(text))
            {
                source.Status = "Failed";
                source.FailureReason = "The PDF contains no extractable text (possibly a scanned image).";
                return;
            }

            source.ExtractedContent = Truncate(text, MaxExtractedChars);
            source.Status = "Extracted";
            source.FailureReason = null;
        }

        private async Task IngestVideoAsync(ProjectSource source, CancellationToken ct)
        {
            // Video URLs are never downloaded. Best effort: pull the YouTube
            // transcript; otherwise keep the link + title as metadata-only
            // evidence for analysis and generation.
            var transcript = await TryFetchYouTubeTranscriptAsync(source.Url, ct);
            if (!string.IsNullOrWhiteSpace(transcript))
            {
                source.ExtractedContent = Truncate(transcript, MaxExtractedChars);
                source.Status = "Extracted";
                source.FailureReason = null;
                return;
            }

            source.ExtractedContent = Truncate($"Video link: {source.Url}\nTitle: {source.Title}", 1000);
            source.Status = "Extracted";
            source.FailureReason = "Content is metadata-only (no transcript available).";
        }

        private async Task IngestImageAsync(ProjectSource source, CancellationToken ct)
        {
            // Nothing heavy is fetched for images; use the LLM to describe the
            // image from its URL/title, or fall back to URL + type only.
            try
            {
                var userPrompt = $"Describe in one paragraph what this image most likely shows, given its URL and title. " +
                                 $"Respond with the paragraph only.\nURL: {source.Url}\nTitle: {source.Title}";
                var description = await _llmGateway.CompleteAsync(
                    "You describe learning-material images for a course-authoring tool. Answer with one short paragraph only.",
                    userPrompt, LlmRole.General, ct);

                if (!string.IsNullOrWhiteSpace(description))
                {
                    source.ExtractedContent = Truncate($"Image: {source.Url}\nTitle: {source.Title}\nDescription: {description.Trim()}", 2000);
                    source.Status = "Extracted";
                    source.FailureReason = null;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Image description via LLM failed for {Url}.", source.Url);
            }

            source.ExtractedContent = Truncate($"Image: {source.Url}\nTitle: {source.Title}", 1000);
            source.Status = "Extracted";
            source.FailureReason = "Content is metadata-only (image not analyzed).";
        }

        private async Task<string?> TryFetchYouTubeTranscriptAsync(string url, CancellationToken ct)
        {
            var videoId = ExtractYouTubeId(url);
            if (string.IsNullOrWhiteSpace(videoId)) return null;

            try
            {
                var client = _httpClientFactory.CreateClient("SourceIngestion");
                // Best-effort public timedtext endpoint; often empty when captions
                // are missing, which is acceptable.
                var response = await client.GetAsync(
                    $"https://www.youtube.com/api/timedtext?lang=en&v={Uri.EscapeDataString(videoId)}", ct);
                if (!response.IsSuccessStatusCode) return null;

                var xml = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(xml) || !xml.Contains("<transcript", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var sb = new StringBuilder();
                foreach (var m in Regex.Matches(xml, "<text[^>]*>(.*?)</text>", RegexOptions.Singleline)
                             .Cast<Match>())
                {
                    var line = System.Net.WebUtility.HtmlDecode(Regex.Replace(m.Groups[1].Value, "<[^>]+>", ""));
                    if (!string.IsNullOrWhiteSpace(line)) sb.Append(line.Trim()).Append(' ');
                }

                var text = sb.ToString().Trim();
                return text.Length > 50 ? text : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "YouTube transcript fetch failed for {VideoId}.", videoId);
                return null;
            }
        }

        private static string? ExtractYouTubeId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var match = Regex.Match(url, @"(?:v=|youtu\.be/|/embed/|/shorts/)([A-Za-z0-9_-]{11})");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string ExtractPdfText(byte[] bytes)
        {
            try
            {
                using var document = PdfDocument.Open(bytes);
                var sb = new StringBuilder();
                foreach (var page in document.GetPages())
                {
                    var text = page.Text;
                    if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                    if (sb.Length > MaxExtractedChars) break;
                }
                return sb.ToString().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            html = Regex.Replace(html, "<script[\\s\\S]*?</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            html = Regex.Replace(html, "<style[\\s\\S]*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            html = Regex.Replace(html, "<noscript[\\s\\S]*?</noscript>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            html = Regex.Replace(html, "<[^>]+>", " ");
            html = System.Net.WebUtility.HtmlDecode(html);
            html = Regex.Replace(html, "\\s+", " ");
            return html.Trim();
        }

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? null
            : value.Length <= max ? value
            : value[..max];
    }
}
