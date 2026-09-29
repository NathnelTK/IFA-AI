using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.Services
{
    public class MockScholarxivService : IScholarxivService
    {
        private static readonly List<ScholarxivPaperSummary> SeededPapers = new()
        {
            new ScholarxivPaperSummary
            {
                Title = "Pedagogical Principles of Microservices and Clean Architecture in Enterprise Education",
                Authors = "M. Tekle, K. Bekele, A. Desta",
                Abstract = "An empirical study on structuring progressive software engineering curricula focusing on domain-driven design, onion architecture, and separation of concerns in modern cloud native frameworks.",
                Doi = "10.1145/3456789.3456790",
                PublishedYear = "2025",
                KeyTopics = new List<string> { "Clean Architecture", "REST APIs", "Microservices", "ASP.NET Core" }
            },
            new ScholarxivPaperSummary
            {
                Title = "Formative Assessment & Adaptive Skill Gap Detection in Technical Exit Examinations",
                Authors = "E. Eshetu, N. Tekleyes",
                Abstract = "Evaluation of diagnostic question banks mapping to Bloom's taxonomy to remediate specific conceptual gaps in developer competency before national university exit examinations.",
                Doi = "10.1109/TE.2024.1002341",
                PublishedYear = "2024",
                KeyTopics = new List<string> { "Adaptive Learning", "Exit Exams", "Skill Assessment", "Bloom's Taxonomy" }
            },
            new ScholarxivPaperSummary
            {
                Title = "Comparative Analysis of Stateless Authentication: JWT Token Lifecycle and Security Vulnerabilities",
                Authors = "D. Smith, R. Chen",
                Abstract = "Detailed examination of JSON Web Token signing algorithms, bearer token validation in web middleware, and mitigation strategies for token replay attacks.",
                Doi = "10.1016/j.cose.2024.103456",
                PublishedYear = "2024",
                KeyTopics = new List<string> { "JWT Auth", "Authentication", "Web Security", "Authorization" }
            }
        };

        public Task<List<ScholarxivPaperSummary>> SearchPapersAsync(string query, int maxResults = 3, CancellationToken cancellationToken = default)
        {
            var cleanQuery = query.ToLower();
            var matches = SeededPapers
                .Where(p => p.Title.ToLower().Contains(cleanQuery) ||
                            p.KeyTopics.Any(t => cleanQuery.Contains(t.ToLower()) || t.ToLower().Contains(cleanQuery)))
                .Take(maxResults)
                .ToList();

            if (!matches.Any())
            {
                // Return top foundational papers as fallback
                matches = SeededPapers.Take(maxResults).ToList();
            }

            return Task.FromResult(matches);
        }

        public Task<ScholarxivPaperSummary?> GetPaperDetailsAsync(string doi, CancellationToken cancellationToken = default)
        {
            var paper = SeededPapers.FirstOrDefault(p => p.Doi == doi);
            return Task.FromResult(paper);
        }
    }
}
