using System;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Courses.Commands;

namespace IFA.Application.Courses.Services
{
    public interface ICourseSharingService
    {
        Task<ShareCourseResult> ShareCourseAsync(ShareCourseCommand command);
        Task<bool> EnrollViaShareCodeAsync(
            string shareCode,
            Guid learnerId,
            CancellationToken cancellationToken = default);
    }
}
