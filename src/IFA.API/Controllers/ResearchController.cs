using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Learning.DTOs;
using IFA.Application.Research.Queries;
using IFA.Application.Research.Services;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResearchController : ControllerBase
    {
        private readonly SearchScholarxivQueryHandler _scholarxivHandler;
        private readonly IUnifiedResearchService _unifiedResearchService;

        public ResearchController(
            SearchScholarxivQueryHandler scholarxivHandler,
            IUnifiedResearchService unifiedResearchService)
        {
            _scholarxivHandler = scholarxivHandler;
            _unifiedResearchService = unifiedResearchService;
        }

        [HttpGet("scholarxiv")]
        public async Task<IActionResult> SearchScholarxiv(
            [FromQuery] string query,
            [FromQuery] int maxResults = 3,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Query parameter is required." });
            }

            var results = await _scholarxivHandler.HandleAsync(
                new SearchScholarxivQuery { Query = query, MaxResults = maxResults },
                cancellationToken);

            return Ok(results);
        }

        [HttpPost("unified")]
        public async Task<IActionResult> BuildUnifiedResearch(
            [FromBody] UnifiedResearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var profile = request.Profile ?? new LearnerProfileDto
            {
                Goal = request.Topic,
                CurrentLevel = "Beginner"
            };

            var package = await _unifiedResearchService.BuildResearchPackageAsync(
                profile,
                request.Topic,
                cancellationToken);

            return Ok(package);
        }
    }

    public class UnifiedResearchRequest
    {
        public string Topic { get; set; } = string.Empty;
        public LearnerProfileDto? Profile { get; set; }
    }
}
