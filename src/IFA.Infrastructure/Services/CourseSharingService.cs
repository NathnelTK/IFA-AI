using IFA.Application.Courses.Commands;
using IFA.Application.Common.Interfaces;
using IFA.Application.Courses.Services;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace IFA.Infrastructure.Services
{
    public class CourseSharingService : ICourseSharingService
    {
        private readonly IApplicationDbContext _context;

        public CourseSharingService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShareCourseResult> ShareCourseAsync(ShareCourseCommand command)
        {
            var access = await CourseAccess.GetAsync(_context, command.CourseId, command.SharedByLearnerId);
            access.RequireContent();
            if (!access.Course.IsPublished)
                throw new CourseAccessException(409, "Publish this course before creating learner invitations.");
            if (!access.IsOwner && (access.Course.Visibility == "Private" || access.Course.EnrollmentMode == "InviteOnly"))
                throw new CourseAccessException(403, "Only the course owner can issue invitations to this course.");

            string shareCode;
            do
            {
                shareCode = GenerateShareCode();
            }
            while (await _context.CourseShareInvites.AnyAsync(invite => invite.ShareCode == shareCode)
                || await _context.Courses.AnyAsync(course => course.ShareCode == shareCode));

            var shareInvite = new CourseShareInvite
            {
                Id = Guid.NewGuid(),
                CourseId = command.CourseId,
                SenderLearnerId = command.SharedByLearnerId,
                ShareCode = shareCode,
                RecipientEmail = null, // Can be set if sharing via email
                IsAccepted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(shareInvite);
            await _context.SaveChangesAsync();

            return new ShareCourseResult
            {
                ShareUrl = $"/courses/join/{shareCode}",
                ShareCode = shareCode,
                ExpiresAt = shareInvite.CreatedAt.AddDays(7)
            };
        }

        public async Task<bool> EnrollViaShareCodeAsync(
            string shareCode,
            Guid learnerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (course, invite) = await CourseAccess.ResolveShareAsync(_context, shareCode, cancellationToken);
                await CourseAccess.EnrollAsync(_context, course.Id, learnerId, invite?.Id, cancellationToken);
                return true;
            }
            catch (CourseAccessException ex) when (ex.StatusCode == 404)
            {
                return false;
            }
        }

        private string GenerateShareCode()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
