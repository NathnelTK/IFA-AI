using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    public class ScholarxivPaperSummary
    {
        public string Title { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string Doi { get; set; } = string.Empty;
        public string PublishedYear { get; set; } = string.Empty;
        public List<string> KeyTopics { get; set; } = new List<string>();
    }

    public interface IScholarxivService
    {
        Task<List<ScholarxivPaperSummary>> SearchPapersAsync(string query, int maxResults = 3, CancellationToken cancellationToken = default);
        Task<ScholarxivPaperSummary?> GetPaperDetailsAsync(string doi, CancellationToken cancellationToken = default);
    }
}
