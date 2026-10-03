using IFA.Application.Courses.Commands;
using IFA.Application.Courses.Services;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseSharingController : ControllerBase
    {
        private readonly ICourseSharingService _courseSharingService;

        public CourseSharingController(ICourseSharingService courseSharingService)
        {
            _courseSharingService = courseSharingService;
        }

        [HttpPost("share")]
        public async Task<ActionResult<ShareCourseResult>> ShareCourse(ShareCourseCommand command)
        {
            var result = await _courseSharingService.ShareCourseAsync(command);
            return Ok(result);
        }

        [HttpPost("enroll/{shareCode}")]
        public async Task<ActionResult> EnrollViaShareCode(string shareCode, [FromBody] EnrollRequest request)
        {
            var success = await _courseSharingService.EnrollViaShareCodeAsync(shareCode, request.LearnerId);
            if (success)
            {
                return Ok(new { message = "Successfully enrolled in course" });
            }
            return BadRequest(new { message = "Invalid or expired share code" });
        }
    }

    public class EnrollRequest
    {
        public Guid LearnerId { get; set; }
    }
}