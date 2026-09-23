using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Application.Research.Queries
{
    public class SearchScholarxivQuery
    {
        public string Query { get; set; } = string.Empty;
        public int MaxResults { get; set; } = 3;
    }

    public class SearchScholarxivQueryHandler
    {
        private readonly IScholarxivService _scholarxivService;

        public SearchScholarxivQueryHandler(IScholarxivService scholarxivService)
        {
            _scholarxivService = scholarxivService;
        }

        public async Task<List<ScholarxivPaperSummary>> HandleAsync(
            SearchScholarxivQuery query,
            CancellationToken cancellationToken = default)
        {
            return await _scholarxivService.SearchPapersAsync(query.Query, query.MaxResults, cancellationToken);
        }
    }
}
