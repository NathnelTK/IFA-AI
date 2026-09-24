using IFA.Application.Courses.Commands;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using System.Linq;

namespace IFA.Application.Courses.Services
{
    public class CourseSharingService
    {
        private readonly IApplicationDbContext _context;

        public CourseSharingService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShareCourseResult> ShareCourseAsync(ShareCourseCommand command)
        {
            // Generate a unique share code
            var shareCode = GenerateShareCode();

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

            // In real implementation, would add to context and save
            // For now, using mock logic
            // _context.CourseShareInvites.Add(shareInvite);
            // await _context.SaveChangesAsync(CancellationToken.None);

            return new ShareCourseResult
            {
                ShareUrl = $"/courses/join/{shareCode}",
                ShareCode = shareCode,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<bool> EnrollViaShareCodeAsync(string shareCode, Guid learnerId)
        {
            // In real implementation, would query CourseShareInvites properly
            // For now, using mock logic
            var isValidCode = shareCode.Length == 8 && shareCode.All(char.IsLetterOrDigit);

            if (!isValidCode)
            {
                return false;
            }

            // Mock enrollment logic - in real implementation would check existing enrollments
            // and create new enrollment via the context
            return true;
        }

        private string GenerateShareCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}