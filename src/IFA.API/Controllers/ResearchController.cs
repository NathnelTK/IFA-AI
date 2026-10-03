using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
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
        public async Task<IActionResult> GetOrConductResearch(
            [FromQuery] string topic,
            [FromQuery] Guid? courseId,
            CancellationToken cancellationToken)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var result = await _researchService.ConductResearchAsync(topic, learnerId, courseId, cancellationToken);
            return Ok(ToResponse(result));
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseResearch(Guid courseId, CancellationToken cancellationToken)
        {
            var result = await _researchService.GetResearchForCourseAsync(courseId);
            if (result == null)
            {
                var course = await _context.Courses
                    .Include(c => c.Modules)
                    .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

                if (course is null)
                {
                    return NotFound(new { message = "Course not found." });
                }

                var topics = course.Modules
                    .OrderBy(module => module.ModuleNumber)
                    .Select(module => module.Title);
                result = await _researchService.ConductResearchAsync(
                    $"{course.Title}: {string.Join("; ", topics)}",
                    null,
                    courseId,
                    cancellationToken);
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
