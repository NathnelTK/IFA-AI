using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Xml;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace IFA.Infrastructure.Services
{
    public sealed class AssetContentExtractor : IAssetContentExtractor
    {
        private const int MaxCharacters = 250_000;
        private readonly IImageOcrService _ocr;

        public AssetContentExtractor(IImageOcrService ocr)
        {
            _ocr = ocr;
        }

        public async Task<string> ExtractAsync(
            Stream content,
            string contentType,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            string text;
            if (contentType == "application/pdf")
            {
                await using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, cancellationToken);
                using var document = PdfDocument.Open(buffer.ToArray());
                var builder = new StringBuilder();
                foreach (var page in document.GetPages())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    builder.AppendLine(page.Text);
                    if (builder.Length >= MaxCharacters) break;
                }
                text = builder.ToString();
            }
            else if (contentType == "application/vnd.openxmlformats-officedocument.presentationml.presentation"
                || fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase))
            {
                text = ExtractPresentation(content, cancellationToken);
            }
            else if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                text = await _ocr.ExtractTextAsync(content, contentType, cancellationToken);
            }
            else if (contentType is "text/plain" or "text/markdown")
            {
                using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var buffer = new char[16 * 1024];
                var builder = new StringBuilder();
                int read;
                while (builder.Length < MaxCharacters
                    && (read = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, MaxCharacters - builder.Length)), cancellationToken)) > 0)
                {
                    builder.Append(buffer, 0, read);
                }
                text = builder.ToString();
            }
            else
            {
                throw new NotSupportedException("Text extraction is not supported for this clean asset type.");
            }

            text = text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("No readable text could be extracted from this asset.");
            }
            return text.Length <= MaxCharacters ? text : text[..MaxCharacters];
        }

        private static string ExtractPresentation(Stream content, CancellationToken cancellationToken)
        {
            using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            var builder = new StringBuilder();
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            foreach (var entry in archive.Entries
                .Where(entry => entry.FullName.StartsWith("ppt/slides/slide", StringComparison.Ordinal)
                    && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.FullName, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = entry.Open();
                using var reader = XmlReader.Create(stream, settings);
                while (reader.Read() && builder.Length < MaxCharacters)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
                    {
                        var value = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(value)) builder.AppendLine(value);
                    }
                }
                if (builder.Length >= MaxCharacters) break;
            }
            return builder.ToString();
        }
    }

    public sealed class ImageOcrService : IImageOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly OcrOptions _options;

        public ImageOcrService(HttpClient httpClient, IOptions<OcrOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> ExtractTextAsync(
            Stream image,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint))
            {
                throw new InvalidOperationException("Image OCR is not configured.");
            }
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.TryAddWithoutValidation("X-API-Key", _options.ApiKey);
            }
            request.Content = new StreamContent(image);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<OcrResponse>(cancellationToken: cancellationToken);
            return payload?.Text ?? string.Empty;
        }

        private sealed record OcrResponse(string Text);
    }
}
