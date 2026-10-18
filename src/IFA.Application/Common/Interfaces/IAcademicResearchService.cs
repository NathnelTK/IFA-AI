using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    /// <summary>
    /// One merged academic / web search result returned by the research
    /// aggregation providers (OpenAlex, Semantic Scholar, arXiv, Tavily,
    /// Wikimedia).
    /// </summary>
    public class AcademicSearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        /// <summary>OpenAlex | SemanticScholar | arXiv | Tavily | Wikimedia</summary>
        public string Source { get; set; } = "OpenAlex";
        public DateTime? PublishedDate { get; set; }
        public string? Authors { get; set; }
    }

    /// <summary>
    /// Multi-provider academic search replacing the single-source ScholarXiv
    /// client. Fuses results from OpenAlex, Semantic Scholar, arXiv, Tavily
    /// (when configured) and Wikimedia Commons.
    /// </summary>
    public interface IAcademicResearchService
    {
        Task<List<AcademicSearchResult>> SearchAsync(string query, int maxResults = 8, CancellationToken ct = default);
    }
}
