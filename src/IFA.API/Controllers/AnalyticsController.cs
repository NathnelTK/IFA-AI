using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/analytics")]
    public sealed class AnalyticsController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly TimeProvider _timeProvider;

        public AnalyticsController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            TimeProvider timeProvider)
        {
            _db = db;
            _authorization = authorization;
            _timeProvider = timeProvider;
        }

        [HttpGet("workspaces/{workspaceId:guid}/overview")]
        public async Task<IActionResult> WorkspaceOverview(
            Guid workspaceId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.CanManageWorkspaceAsync(accountId, workspaceId, cancellationToken)) return Forbid();
            var end = to?.ToUniversalTime() ?? _timeProvider.GetUtcNow().UtcDateTime;
            var start = from?.ToUniversalTime() ?? end.AddDays(-30);
            if (start >= end || end - start > TimeSpan.FromDays(366))
                return BadRequest(new { message = "Choose an analytics window of up to 366 days." });

            var courseIds = await _db.Courses
                .Where(course => course.WorkspaceId == workspaceId)
                .Select(course => course.Id)
                .ToListAsync(cancellationToken);
            var enrollments = _db.CourseEnrollments.Where(enrollment =>
                courseIds.Contains(enrollment.CourseId)
                && enrollment.EnrolledAt >= start
                && enrollment.EnrolledAt < end);
            var payments = _db.Payments.Where(payment =>
                courseIds.Contains(payment.CourseId)
                && payment.Status == "Completed"
                && payment.CompletedAt >= start
                && payment.CompletedAt < end);
            var enrollmentCount = await enrollments.CountAsync(cancellationToken);
            var completionCount = await enrollments.CountAsync(enrollment => enrollment.CompletedAt != null, cancellationToken);
            var revenue = await payments.SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0;
            var reviews = await _db.CourseReviews
                .Where(review => courseIds.Contains(review.CourseId) && review.Status == "Published")
                .Select(review => review.Rating)
                .ToListAsync(cancellationToken);
            var enrollmentTrend = await enrollments
                .GroupBy(enrollment => enrollment.EnrolledAt.Date)
                .Select(group => new { date = group.Key, count = group.Count() })
                .OrderBy(item => item.date)
                .ToListAsync(cancellationToken);
            var revenueTrend = await payments
                .GroupBy(payment => payment.CompletedAt!.Value.Date)
                .Select(group => new { date = group.Key, amount = group.Sum(payment => payment.Amount) })
                .OrderBy(item => item.date)
                .ToListAsync(cancellationToken);
            var funnel = await _db.ProductActivityEvents
                .Where(activity => activity.WorkspaceId == workspaceId
                    && activity.OccurredAt >= start
                    && activity.OccurredAt < end)
                .GroupBy(activity => activity.EventType)
                .Select(group => new { eventType = group.Key, count = group.Count() })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                window = new { start, end },
                courses = courseIds.Count,
                publishedReleases = await _db.CourseReleases.CountAsync(release =>
                    courseIds.Contains(release.CourseId) && release.PublishedAt >= start && release.PublishedAt < end,
                    cancellationToken),
                enrollments = enrollmentCount,
                completions = completionCount,
                completionRate = enrollmentCount == 0 ? 0 : Math.Round(completionCount * 100d / enrollmentCount, 1),
                revenue,
                currency = "ETB",
                rating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(), 1),
                reviewCount = reviews.Count,
                followers = await _db.WorkspaceFollows.CountAsync(follow => follow.WorkspaceId == workspaceId, cancellationToken),
                enrollmentTrend,
                revenueTrend,
                funnel
            });
        }

        [HttpGet("platform/operations")]
        public async Task<IActionResult> PlatformOperations(CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.PlatformAdmin, cancellationToken)
                || !int.TryParse(User.FindFirst("mfa_level")?.Value, out var mfaLevel)
                || mfaLevel < 1) return Forbid();
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            return Ok(new
            {
                generatedAt = now,
                queues = new
                {
                    authoring = await _db.AuthoringJobs.GroupBy(job => job.State).Select(group => new { state = group.Key, count = group.Count() }).ToListAsync(cancellationToken),
                    modules = await _db.GenerationJobs.GroupBy(job => job.Status).Select(group => new { state = group.Key, count = group.Count() }).ToListAsync(cancellationToken),
                    assets = await _db.AssetProcessingJobs.GroupBy(job => job.State).Select(group => new { state = group.Key, count = group.Count() }).ToListAsync(cancellationToken),
                    certificates = await _db.CertificateRenderJobs.GroupBy(job => job.State).Select(group => new { state = group.Key, count = group.Count() }).ToListAsync(cancellationToken),
                    outbox = new
                    {
                        pending = await _db.OutboxMessages.CountAsync(message => message.ProcessedAt == null && message.DeadLetteredAt == null, cancellationToken),
                        deadLettered = await _db.OutboxMessages.CountAsync(message => message.DeadLetteredAt != null, cancellationToken)
                    }
                },
                pendingDomains = await _db.CustomDomains.CountAsync(domain => domain.Status != "Verified" && domain.DisabledAt == null, cancellationToken),
                pendingPayments = await _db.Payments.CountAsync(payment => payment.Status == "Pending" || payment.Status == "Initializing", cancellationToken),
                openModerationCases = await _db.ModerationCases.CountAsync(moderation => moderation.Status == "Open", cancellationToken)
            });
        }
    }
}
