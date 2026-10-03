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
            if (!await _context.Courses.AnyAsync(course => course.Id == command.CourseId))
            {
                throw new KeyNotFoundException($"Course {command.CourseId} was not found.");
            }

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
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<bool> EnrollViaShareCodeAsync(
            string shareCode,
            Guid learnerId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(shareCode))
            {
                return false;
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(
                    item => item.IsPublic && item.ShareCode == shareCode,
                    cancellationToken);

            CourseShareInvite? invite = null;
            if (course is null)
            {
                invite = await _context.CourseShareInvites
                    .Include(item => item.Course)
                    .FirstOrDefaultAsync(
                        item => item.ShareCode == shareCode
                            && !item.IsAccepted
                            && item.CreatedAt >= DateTime.UtcNow.AddDays(-7),
                        cancellationToken);
                course = invite?.Course;
            }

            if (course is null)
            {
                return false;
            }

            var alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(
                    enrollment => enrollment.CourseId == course.Id && enrollment.LearnerId == learnerId,
                    cancellationToken);
            if (!alreadyEnrolled)
            {
                _context.Add(new CourseEnrollment
                {
                    CourseId = course.Id,
                    LearnerId = learnerId,
                    EnrolledAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                });
            }

            if (invite is not null)
            {
                invite.IsAccepted = true;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private string GenerateShareCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}