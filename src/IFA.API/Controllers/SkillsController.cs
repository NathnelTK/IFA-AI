using IFA.Application.Skills.Queries;
using IFA.Application.Skills.Services;
using IFA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SkillsController : BaseApiController
    {
        private readonly SkillProfileService _skillProfileService;
        private readonly IApplicationDbContext _context;

        public SkillsController(SkillProfileService skillProfileService, IApplicationDbContext context)
        {
            _skillProfileService = skillProfileService;
            _context = context;
        }

        [HttpGet("{learnerId}")]
        public async Task<ActionResult<GetLearnerSkillsQueryResult>> GetLearnerSkills(Guid learnerId)
        {
            if (learnerId != await GetCurrentLearnerIdAsync(_context)) return StatusCode(403);
            var result = await _skillProfileService.GetLearnerSkillsAsync(learnerId);
            return Ok(result);
        }

        [HttpGet("{learnerId}/weak-areas")]
        public async Task<ActionResult<List<LearnerSkillDto>>> GetWeakAreas(Guid learnerId)
        {
            if (learnerId != await GetCurrentLearnerIdAsync(_context)) return StatusCode(403);
            var result = await _skillProfileService.GetWeakAreasAsync(learnerId);
            return Ok(result);
        }
    }
}
