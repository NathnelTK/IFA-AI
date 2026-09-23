using IFA.Application.Skills.Queries;
using IFA.Application.Skills.Services;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly SkillProfileService _skillProfileService;

        public SkillsController(SkillProfileService skillProfileService)
        {
            _skillProfileService = skillProfileService;
        }

        [HttpGet("{learnerId}")]
        public async Task<ActionResult<GetLearnerSkillsQueryResult>> GetLearnerSkills(Guid learnerId)
        {
            var result = await _skillProfileService.GetLearnerSkillsAsync(learnerId);
            return Ok(result);
        }

        [HttpGet("{learnerId}/weak-areas")]
        public async Task<ActionResult<List<LearnerSkillDto>>> GetWeakAreas(Guid learnerId)
        {
            var result = await _skillProfileService.GetWeakAreasAsync(learnerId);
            return Ok(result);
        }
    }
}