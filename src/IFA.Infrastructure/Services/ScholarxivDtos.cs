// IFA.Infrastructure/Services/ScholarxivDtos.cs
using System.Text.Json.Serialization;

namespace IFA.Infrastructure.Services
{
    internal class ScholarxivSearchResponse
    {
        [JsonPropertyName("data")]
        public List<ScholarxivPaperDto> Data { get; set; } = new();

        [JsonPropertyName("pagination")]
        public ScholarxivPagination? Pagination { get; set; }
    }

    internal class ScholarxivPaperDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("extractedID")] public string ExtractedId { get; set; } = string.Empty;
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
        [JsonPropertyName("authors")] public List<string> Authors { get; set; } = new();
        [JsonPropertyName("published")] public DateTime? Published { get; set; }
        [JsonPropertyName("primaryCategory")] public string? PrimaryCategory { get; set; }
        [JsonPropertyName("category")] public List<string> Category { get; set; } = new();
        [JsonPropertyName("pdfLink")] public string? PdfLink { get; set; }
        [JsonPropertyName("doi")] public string? Doi { get; set; } // optional per docs — many papers won't have one
    }

    internal class ScholarxivPagination
    {
        [JsonPropertyName("hasMore")] public bool HasMore { get; set; }
        [JsonPropertyName("nextPage")] public int? NextPage { get; set; }
    }
}