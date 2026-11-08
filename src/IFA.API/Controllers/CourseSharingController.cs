using IFA.Application.Courses.Commands;
using IFA.Application.Courses.Services;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CourseSharingController : BaseApiController
    {
        private readonly ICourseSharingService _courseSharingService;
        private readonly IApplicationDbContext _context;

        public CourseSharingController(ICourseSharingService courseSharingService, IApplicationDbContext context)
        {
            _courseSharingService = courseSharingService;
            _context = context;
        }

        [HttpPost("share")]
        public async Task<ActionResult<ShareCourseResult>> ShareCourse(ShareCourseCommand command, CancellationToken ct)
        {
            command.SharedByLearnerId = await GetCurrentLearnerIdAsync(_context);
            var result = await _courseSharingService.ShareCourseAsync(command);
            return Ok(result);
        }

        [HttpPost("enroll/{shareCode}")]
        public async Task<ActionResult> EnrollViaShareCode(string shareCode, CancellationToken ct)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var (course, invite) = await CourseAccess.ResolveShareAsync(_context, shareCode, ct);
            var enrollment = await CourseAccess.EnrollAsync(_context, course.Id, learnerId, invite?.Id, ct);
            return Ok(new
            {
                success = true,
                courseId = course.Id,
                status = enrollment.Status,
                paymentRequired = enrollment.Status == "PendingPayment"
            });
        }
    }

}
