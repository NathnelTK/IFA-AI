using System;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : BaseApiController
    {
        private readonly IActivityService _activityService;
        private readonly IApplicationDbContext _context;

        public ActivityController(IActivityService activityService, IApplicationDbContext context)
        {
            _activityService = activityService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivities([FromQuery] int limit = 20)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var activities = await _activityService.GetRecentActivitiesAsync(learnerId, limit);
            return Ok(activities);
        }
    }
}
