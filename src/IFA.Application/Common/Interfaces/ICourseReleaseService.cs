using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface ICourseReleaseService
    {
        Task<CourseRelease> PublishAsync(
            Guid courseId,
            Guid actorAccountId,
            CancellationToken cancellationToken = default);
    }
}
