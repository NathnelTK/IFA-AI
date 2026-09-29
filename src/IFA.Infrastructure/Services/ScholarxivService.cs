using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Real ScholarXIV Papers API integration (REST, not MCP — see PR notes
    /// for why: this service is deterministic backend code, not an
    /// AI agent, so the plain REST Papers API is the right fit).
    /// Base URL and auth are configured via HttpClient in DependencyInjection.
    /// </summary>
    public class ScholarxivService : IScholarxivService
    {

        private readonly HttpClient _httpClient;
        private readonly ILogger<ScholarxivService> _logger;

        public ScholarxivService(HttpClient httpClient, ILogger<ScholarxivService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        // private static readonly List<ScholarxivPaperSummary> SeededPapers = new()
        // {
        //     new ScholarxivPaperSummary
        //     {
        //         Title = "Pedagogical Principles of Microservices and Clean Architecture in Enterprise Education",
        //         Authors = "M. Tekle, K. Bekele, A. Desta",
        //         Abstract = "An empirical study on structuring progressive software engineering curricula focusing on domain-driven design, onion architecture, and separation of concerns in modern cloud native frameworks.",
        //         Doi = "10.1145/3456789.3456790",
        //         PublishedYear = "2025",
        //         KeyTopics = new List<string> { "Clean Architecture", "REST APIs", "Microservices", "ASP.NET Core" }
        //     },
        //     new ScholarxivPaperSummary
        //     {
        //         Title = "Formative Assessment & Adaptive Skill Gap Detection in Technical Exit Examinations",
        //         Authors = "E. Eshetu, N. Tekleyes",
        //         Abstract = "Evaluation of diagnostic question banks mapping to Bloom's taxonomy to remediate specific conceptual gaps in developer competency before national university exit examinations.",
        //         Doi = "10.1109/TE.2024.1002341",
        //         PublishedYear = "2024",
        //         KeyTopics = new List<string> { "Adaptive Learning", "Exit Exams", "Skill Assessment", "Bloom's Taxonomy" }
        //     },
        //     new ScholarxivPaperSummary
        //     {
        //         Title = "Comparative Analysis of Stateless Authentication: JWT Token Lifecycle and Security Vulnerabilities",
        //         Authors = "D. Smith, R. Chen",
        //         Abstract = "Detailed examination of JSON Web Token signing algorithms, bearer token validation in web middleware, and mitigation strategies for token replay attacks.",
        //         Doi = "10.1016/j.cose.2024.103456",
        //         PublishedYear = "2024",
        //         KeyTopics = new List<string> { "JWT Auth", "Authentication", "Web Security", "Authorization" }
        //     }
        // };
        public async Task<List<ScholarxivPaperSummary>> SearchPapersAsync(
            string query,
            int maxResults = 3,
            CancellationToken cancellationToken = default)
        {
            var url =
                $"api/v1/papers/search?q={Uri.EscapeDataString(query)}&limit={maxResults}";

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ScholarXIV request starting. Query: '{Query}', URL: {Url}",
                query,
                url);

            try
            {
                var response = await _httpClient.GetAsync(
                    url,
                    cancellationToken);

                _logger.LogInformation(
                    "ScholarXIV responded with {StatusCode} after {ElapsedMs}ms",
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                response.EnsureSuccessStatusCode();

                var result =
                    await response.Content.ReadFromJsonAsync<ScholarxivSearchResponse>(
                        cancellationToken: cancellationToken);

                var papers = (result?.Data ?? new List<ScholarxivPaperDto>())
                    .Select(MapToSummary)
                    .ToList();

                _logger.LogInformation(
                    "ScholarXIV returned {PaperCount} papers after {ElapsedMs}ms",
                    papers.Count,
                    stopwatch.ElapsedMilliseconds);

                return papers;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "ScholarXIV request was cancelled after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        public async Task<ScholarxivPaperSummary?> GetPaperDetailsAsync(string doi, CancellationToken cancellationToken = default)
        {
            var body = new
            {
                searchFilterString = new { all = doi },
                limit = 1
            };

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ScholarXIV paper details request starting. DOI: '{Doi}'",
                doi);
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
           "api/v1/papers/search", body, cancellationToken);
                _logger.LogInformation(
                 "ScholarXIV paper details responded with {StatusCode} after {ElapsedMs}ms",
                 response.StatusCode,
                 stopwatch.ElapsedMilliseconds);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ScholarxivSearchResponse>(
              cancellationToken: cancellationToken
          );
                var match = result?.Data.FirstOrDefault();

                if (match is null || !string.Equals(match.Doi, doi, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("No confident DOI match found for {Doi}", doi);
                    return null;
                }
                return MapToSummary(match);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
           "ScholarXIV paper details request was cancelled after {ElapsedMs}ms",
           stopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        private static ScholarxivPaperSummary MapToSummary(ScholarxivPaperDto dto) => new()
        {
            Title = dto.Title,
            Authors = string.Join(", ", dto.Authors),
            Abstract = dto.Summary,
            Doi = dto.Doi ?? string.Empty,
            PublishedYear = dto.Published?.Year.ToString() ?? string.Empty,
            KeyTopics = dto.Category
        };
    }
}
