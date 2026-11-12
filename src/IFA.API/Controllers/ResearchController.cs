using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ResearchController : BaseApiController
    {
        private readonly IResearchService _researchService;
        private readonly IApplicationDbContext _context;

        public ResearchController(IResearchService researchService, IApplicationDbContext context)
        {
            _researchService = researchService;
            _context = context;
        }

        [HttpGet]
        [EnableRateLimiting("generation")]
        public async Task<IActionResult> GetOrConductResearch(
            [FromQuery] string topic,
            [FromQuery] Guid? courseId,
            CancellationToken cancellationToken)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            if (string.IsNullOrWhiteSpace(topic) || topic.Length > 2000)
                return BadRequest(new { message = "A research topic of 1-2000 characters is required." });
            if (courseId.HasValue)
            {
                var access = await CourseAccess.GetAsync(_context, courseId.Value, learnerId, cancellationToken);
                if (!access.IsOwner) return StatusCode(403, new { message = "Only the course owner can change its research sources." });
                if (access.Course.IsPublished) return Conflict(new { message = "Unpublish the course before changing its research sources." });
            }
            var result = await _researchService.ConductResearchAsync(topic, learnerId, courseId, cancellationToken);
            return Ok(ToResponse(result));
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseResearch(Guid courseId, CancellationToken cancellationToken)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var access = await CourseAccess.GetAsync(_context, courseId, learnerId, cancellationToken);
            access.RequireContent();
            var result = await _researchService.GetResearchForCourseAsync(courseId, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = "Research sources have not been prepared for this course." });
            }

            return Ok(ToResponse(result));
        }

        private static object ToResponse(ResearchPackage package) => new
        {
            package.Id,
            package.Topic,
            package.Summary,
            package.KeyConceptsJson,
            package.CreatedAt,
            Sources = package.Sources.Select(source => new
            {
                source.Id,
                source.Title,
                source.Url,
                source.SourceType,
                source.Authors,
                source.Snippet,
                source.PublishedYear
            })
        };
    }
}
