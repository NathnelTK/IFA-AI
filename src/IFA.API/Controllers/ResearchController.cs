using System;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetOrConductResearch([FromQuery] string topic, [FromQuery] Guid? courseId)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var result = await _researchService.ConductResearchAsync(topic, learnerId, courseId);
            return Ok(result);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseResearch(Guid courseId)
        {
            var result = await _researchService.GetResearchForCourseAsync(courseId);
            if (result == null)
            {
                result = await _researchService.ConductResearchAsync("Software Engineering", null, courseId);
            }
            return Ok(result);
        }
    }
}
