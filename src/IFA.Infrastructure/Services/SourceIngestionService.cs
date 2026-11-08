using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Extracts bounded text from public sources. Unavailable or unsupported
    /// content is a failed source, never inferred evidence. Caller cancellation propagates.
    /// </summary>
    public class SourceIngestionService : ISourceIngestionService
    {
        private const int MaxExtractedChars = 12000;
        private const int MaxTextBytes = 1024 * 1024;
        private const int MaxPdfBytes = 5 * 1024 * 1024;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SourceIngestionService> _logger;

        public SourceIngestionService(
            IHttpClientFactory httpClientFactory,
            ILogger<SourceIngestionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task IngestAsync(ProjectSource source, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            source.ExtractedContent = null;
            source.FailureReason = null;
            source.Status = "Pending";
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                var uri = ValidateSourceUri(source.Url);
                string text;
                switch (source.Type.ToLowerInvariant())
                {
                    case "video":
                        text = await FetchYouTubeTranscriptAsync(uri, timeout.Token);
                        break;
                    case "image":
                        throw new NotSupportedException("Image extraction is not supported. A URL or title cannot establish the image's contents.");
                    case "ppt":
                        throw new NotSupportedException("Presentation extraction is not supported. Provide a public PDF or text version instead.");
                    case "pdf":
                    case "website":
                    case "article":
                    case "other":
                        var fetched = await FetchAsync(uri, timeout.Token);
                        if (source.Type.Equals("Pdf", StringComparison.OrdinalIgnoreCase) && fetched.MediaType != "application/pdf")
                            throw new NotSupportedException("The PDF link did not return a PDF document.");

                        if (fetched.MediaType == "application/pdf")
                        {
                            source.Type = "Pdf";
                            text = ExtractPdfText(fetched.Content, timeout.Token);
                        }
                        else
                        {
                            var encoding = string.IsNullOrWhiteSpace(fetched.Charset)
                                ? new UTF8Encoding(false, true)
                                : Encoding.GetEncoding(fetched.Charset.Trim('"'), EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                            var raw = encoding.GetString(fetched.Content);
                            if (raw.Any(c => char.IsControl(c) && !char.IsWhiteSpace(c)))
                                throw new NotSupportedException("The response contains binary data, not supported source text.");
                            text = fetched.MediaType == "text/plain" ? raw.Trim() : StripHtml(raw);
                        }
                        break;
                    default:
                        throw new NotSupportedException("This source type is not supported. Provide a public PDF, HTML or text document.");
                }

                timeout.Token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("No readable text could be extracted. Scanned PDFs and metadata-only links are not supported.");
                source.ExtractedContent = Truncate(text, MaxExtractedChars);
                source.Status = "Extracted";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                source.Status = "Pending";
                throw;
            }
            catch (OperationCanceledException)
            {
                source.Status = "Failed";
                source.FailureReason = "Source extraction timed out. Try a smaller, directly accessible document.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Source ingestion failed for {SourceId} ({ErrorType}).", source.Id, ex.GetType().Name);
                source.Status = "Failed";
                source.FailureReason = ex is InvalidOperationException or NotSupportedException
                    ? Truncate(ex.Message, 500)
                    : "The source could not be safely fetched or parsed. Provide a directly accessible public PDF, HTML or text URL.";
            }
        }

        private static Uri ValidateSourceUri(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url.Length > 2048 ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
                !string.IsNullOrEmpty(uri.UserInfo) || uri.Port is not (80 or 443))
                throw new InvalidOperationException("Use a public HTTP(S) URL on port 80 or 443 without embedded credentials.");

            var host = uri.IdnHost.Trim('[', ']').TrimEnd('.');
            if (host.Contains('%') || host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
                uri.HostNameType is UriHostNameType.Unknown or UriHostNameType.Basic)
                throw new InvalidOperationException("Local and private source addresses are not allowed.");
            if (IPAddress.TryParse(host, out var address) && !IsPublicAddress(address))
                throw new InvalidOperationException("Local, private and reserved source addresses are not allowed.");
            return uri;
        }

        internal static bool IsPublicAddress(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            var bytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return !(bytes[0] is 0 or 10 or 127 || bytes[0] >= 224 ||
                    (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) ||
                    (bytes[0] == 169 && bytes[1] == 254) ||
                    (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                    (bytes[0] == 192 && bytes[1] == 168) ||
                    (bytes[0] == 192 && bytes[1] == 0 && bytes[2] is 0 or 2) ||
                    (bytes[0] == 192 && bytes[1] == 88 && bytes[2] == 99) ||
                    (bytes[0] == 198 && bytes[1] is 18 or 19) ||
                    (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) ||
                    (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) ||
                    (bytes[0] == 168 && bytes[1] == 63 && bytes[2] == 129 && bytes[3] == 16));
            }

            // Only global IPv6 unicast; exclude transition, special-purpose and documentation ranges.
            return address.AddressFamily == AddressFamily.InterNetworkV6 && address.ScopeId == 0 &&
                (bytes[0] & 0xe0) == 0x20 &&
                !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] < 2) &&
                !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8) &&
                !(bytes[0] == 0x20 && bytes[1] == 0x02) &&
                !(bytes[0] == 0x3f && bytes[1] == 0xff && (bytes[2] & 0xf0) == 0);
        }

        public static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken ct)
        {
            if (context.DnsEndPoint.Port is not (80 or 443))
                throw new HttpRequestException("Only public HTTP(S) source connections are allowed.");

            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
                throw new HttpRequestException("The source hostname resolved to a private or reserved address.");

            // Resolve once at connect time and dial the validated IP, never the hostname again.
            SocketException? lastError = null;
            foreach (var address in addresses)
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (SocketException ex)
                {
                    lastError = ex;
                    socket.Dispose();
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
            throw new HttpRequestException("Could not connect to the public source address.", lastError);
        }

        private async Task<(byte[] Content, string MediaType, string? Charset)> FetchAsync(Uri uri, CancellationToken ct, bool transcript = false)
        {
            var client = _httpClientFactory.CreateClient("SourceIngestion");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
            if ((int)response.StatusCode is >= 300 and < 400)
                throw new InvalidOperationException("Redirects are not followed. Provide the final public document URL.");
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"The source returned HTTP {(int)response.StatusCode}.");

            var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? string.Empty;
            var supported = transcript
                ? mediaType is "text/xml" or "application/xml" or "text/plain"
                : mediaType is "application/pdf" or "text/html" or "application/xhtml+xml" or "text/plain";
            if (!supported)
                throw new NotSupportedException("The response is not a supported PDF, HTML, plain-text document or transcript.");
            if (response.Content.Headers.ContentEncoding.Any(e => !e.Equals("identity", StringComparison.OrdinalIgnoreCase)))
                throw new NotSupportedException("Compressed source responses are not supported. Provide an uncompressed document URL.");

            var maxBytes = mediaType == "application/pdf" ? MaxPdfBytes : MaxTextBytes;
            if (response.Content.Headers.ContentLength > maxBytes)
                throw new InvalidOperationException("The source exceeds the extraction byte limit (5 MiB for PDF, 1 MiB for text).");
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var body = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(), ct)) > 0)
            {
                if (body.Length + read > maxBytes)
                    throw new InvalidOperationException("The source exceeds the extraction byte limit (5 MiB for PDF, 1 MiB for text).");
                body.Write(buffer, 0, read);
            }
            return (body.ToArray(), mediaType, response.Content.Headers.ContentType?.CharSet);
        }

        private async Task<string> FetchYouTubeTranscriptAsync(Uri uri, CancellationToken ct)
        {
            var videoId = ExtractYouTubeId(uri);
            if (videoId == null)
                throw new NotSupportedException("Only YouTube videos with an accessible text transcript are supported. The video itself is not analyzed.");

            var fetched = await FetchAsync(new Uri($"https://www.youtube.com/api/timedtext?lang=en&v={videoId}"), ct, transcript: true);
            if (fetched.Content.Length == 0)
                throw new InvalidOperationException("No accessible transcript was returned. Video metadata is not extracted content.");
            using var stream = new MemoryStream(fetched.Content);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxTextBytes
            });
            var document = await XDocument.LoadAsync(reader, LoadOptions.None, ct);
            if (document.Root?.Name.LocalName != "transcript")
                throw new InvalidOperationException("No supported video transcript was returned. Metadata is not extracted content.");

            var sb = new StringBuilder();
            foreach (var line in document.Root.Elements().Where(e => e.Name.LocalName == "text"))
            {
                ct.ThrowIfCancellationRequested();
                sb.Append(WebUtility.HtmlDecode(line.Value).Trim()).Append(' ');
                if (sb.Length >= MaxExtractedChars) break;
            }
            return sb.ToString().Trim();
        }

        private static string? ExtractYouTubeId(Uri uri)
        {
            var host = uri.IdnHost.ToLowerInvariant();
            if (host is "youtu.be" or "www.youtu.be")
            {
                var id = uri.AbsolutePath.Trim('/');
                return Regex.IsMatch(id, "^[A-Za-z0-9_-]{11}$") ? id : null;
            }
            if (host is not ("youtube.com" or "www.youtube.com" or "m.youtube.com")) return null;
            var match = Regex.Match(uri.Query.TrimStart('?'), @"(?:^|&)v=([A-Za-z0-9_-]{11})(?:&|$)");
            if (match.Success) return match.Groups[1].Value;
            match = Regex.Match(uri.AbsolutePath, @"^/(?:embed|shorts|live)/([A-Za-z0-9_-]{11})(?:/|$)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string ExtractPdfText(byte[] bytes, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var document = PdfDocument.Open(bytes);
            if (document.NumberOfPages > 100)
                throw new NotSupportedException("PDFs with more than 100 pages are not supported. Provide a smaller document.");
            var sb = new StringBuilder();
            for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
            {
                ct.ThrowIfCancellationRequested();
                var text = document.GetPage(pageNumber).Text;
                if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                if (sb.Length >= MaxExtractedChars) break;
            }
            return sb.ToString().Trim();
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var timeout = TimeSpan.FromSeconds(1);
            html = Regex.Replace(html, "<(script|style|noscript)\\b[^>]*>[\\s\\S]*?</\\1\\s*>", " ", RegexOptions.IgnoreCase, timeout);
            html = Regex.Replace(html, "<[^>]+>", " ", RegexOptions.None, timeout);
            html = WebUtility.HtmlDecode(html);
            html = Regex.Replace(html, "\\s+", " ", RegexOptions.None, timeout);
            return html.Trim();
        }

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? null
            : value.Length <= max ? value
            : value[..max];
    }
}
