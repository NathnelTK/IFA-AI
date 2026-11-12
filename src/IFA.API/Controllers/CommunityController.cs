using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/community")]
    public sealed class CommunityController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;

        public CommunityController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider)
        {
            _db = db;
            _authorization = authorization;
            _features = features.Value;
            _timeProvider = timeProvider;
        }

        public sealed class ReviewRequest
        {
            [Range(1, 5)] public int Rating { get; set; }
            [Required, StringLength(5000, MinimumLength = 10)] public string Body { get; set; } = string.Empty;
        }

        public sealed class ReplyRequest
        {
            [Required, StringLength(5000, MinimumLength = 2)] public string Body { get; set; } = string.Empty;
        }

        public sealed class ReportRequest
        {
            [Required, RegularExpression("^(Spam|Harassment|ConflictOfInterest|Irrelevant|Other)$")] public string Reason { get; set; } = string.Empty;
            [StringLength(2000)] public string Detail { get; set; } = string.Empty;
        }

        public sealed class AnnouncementRequest
        {
            [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
            [Required, StringLength(20_000)] public string BodyMarkdown { get; set; } = string.Empty;
            public Guid? ReleaseId { get; set; }
            public Guid? CohortId { get; set; }
            [RegularExpression("^(Enrolled|Followers|Release|Cohort)$")] public string Audience { get; set; } = "Enrolled";
        }

        public sealed class ModerationDecisionRequest
        {
            [Required, RegularExpression("^(Dismiss|HideReview|RestoreReview)$")] public string Decision { get; set; } = string.Empty;
            [Required, StringLength(2000)] public string Reason { get; set; } = string.Empty;
        }

        [HttpPost("workspaces/{workspaceId:guid}/follow")]
        [Authorize]
        public async Task<IActionResult> Follow(Guid workspaceId, CancellationToken cancellationToken)
        {
            if (!_features.Social) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _db.EducatorWorkspaces.AnyAsync(workspace => workspace.Id == workspaceId && workspace.Status == EducatorWorkspaceStatus.Verified, cancellationToken))
                return NotFound();
            if (!await _db.WorkspaceFollows.AnyAsync(follow => follow.WorkspaceId == workspaceId && follow.LearnerId == accountId, cancellationToken))
            {
                _db.Add(new WorkspaceFollow { WorkspaceId = workspaceId, LearnerId = accountId, CreatedAt = UtcNow() });
                await _db.SaveChangesAsync(cancellationToken);
            }
            return NoContent();
        }

        [HttpDelete("workspaces/{workspaceId:guid}/follow")]
        [Authorize]
        public async Task<IActionResult> Unfollow(Guid workspaceId, CancellationToken cancellationToken)
        {
            if (!_features.Social) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var follow = await _db.WorkspaceFollows.SingleOrDefaultAsync(candidate =>
                candidate.WorkspaceId == workspaceId && candidate.LearnerId == accountId,
                cancellationToken);
            if (follow != null)
            {
                _db.Remove(follow);
                await _db.SaveChangesAsync(cancellationToken);
            }
            return NoContent();
        }

        [HttpPost("courses/{courseId:guid}/announcements")]
        [Authorize]
        public async Task<IActionResult> PublishAnnouncement(
            Guid courseId,
            [FromBody] AnnouncementRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.Social) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.CanManageCourseAsync(accountId, courseId, cancellationToken)) return Forbid();
            if (request.ReleaseId.HasValue
                && !await _db.CourseReleases.AnyAsync(release => release.Id == request.ReleaseId && release.CourseId == courseId, cancellationToken))
                return BadRequest(new { message = "The release is not part of this course." });
            if (request.CohortId.HasValue
                && !await _db.CourseCohorts.AnyAsync(cohort => cohort.Id == request.CohortId && cohort.CourseId == courseId, cancellationToken))
                return BadRequest(new { message = "The cohort is not part of this course." });

            var now = UtcNow();
            var announcement = new CourseAnnouncement
            {
                CourseId = courseId,
                ReleaseId = request.ReleaseId,
                CohortId = request.CohortId,
                AuthorAccountId = accountId,
                Title = request.Title.Trim(),
                BodyMarkdown = request.BodyMarkdown.Trim(),
                Audience = request.Audience,
                CreatedAt = now,
                PublishedAt = now
            };
            _db.Add(announcement);
            _db.Add(new OutboxMessage
            {
                Type = "course.announcement.published",
                AggregateType = nameof(CourseAnnouncement),
                AggregateId = announcement.Id,
                DeduplicationKey = $"announcement:{announcement.Id}",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    announcement.Id,
                    announcement.CourseId,
                    announcement.ReleaseId,
                    announcement.CohortId,
                    announcement.Audience,
                    announcement.Title
                }),
                OccurredAt = now,
                AvailableAt = now,
                ActorLearnerId = accountId,
                CorrelationId = HttpContext.TraceIdentifier
            });
            await _db.SaveChangesAsync(cancellationToken);
            return Created($"/api/community/courses/{courseId}/announcements/{announcement.Id}", announcement);
        }

        [HttpGet("courses/{courseId:guid}/reviews")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviews(Guid courseId, CancellationToken cancellationToken)
        {
            if (!_features.Social) return NotFound();
            var reviews = await _db.CourseReviews
                .Where(review => review.CourseId == courseId && review.Status == "Published")
                .Join(_db.Learners,
                    review => review.LearnerId,
                    learner => learner.Id,
                    (review, learner) => new
                    {
                        review.Id,
                        review.ReleaseId,
                        review.Rating,
                        review.Body,
                        review.CreatedAt,
                        review.UpdatedAt,
                        learnerName = learner.Name,
                        learnerAvatarUrl = learner.AvatarUrl
                    })
                .OrderByDescending(review => review.CreatedAt)
                .ToListAsync(cancellationToken);
            var reviewIds = reviews.Select(review => review.Id).ToArray();
            var replies = await _db.CourseReviewReplies
                .Where(reply => reviewIds.Contains(reply.ReviewId) && reply.Status == "Published")
                .OrderBy(reply => reply.CreatedAt)
                .ToListAsync(cancellationToken);
            var aggregate = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(review => review.Rating), 1);
            return Ok(new
            {
                rating = aggregate,
                reviewCount = reviews.Count,
                reviews = reviews.Select(review => new
                {
                    review.Id,
                    review.ReleaseId,
                    review.Rating,
                    review.Body,
                    review.CreatedAt,
                    review.UpdatedAt,
                    review.learnerName,
                    review.learnerAvatarUrl,
                    replies = replies.Where(reply => reply.ReviewId == review.Id)
                })
            });
        }

        [HttpPut("courses/{courseId:guid}/reviews/mine")]
        [Authorize]
        public async Task<IActionResult> SaveReview(
            Guid courseId,
            [FromBody] ReviewRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.Social) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var enrollment = await _db.CourseEnrollments.SingleOrDefaultAsync(candidate =>
                candidate.CourseId == courseId
                && candidate.LearnerId == accountId
                && candidate.Status == "Active"
                && candidate.ReleaseId != null,
                cancellationToken);
            if (enrollment?.ReleaseId == null) return Forbid();
            var now = UtcNow();
            var review = await _db.CourseReviews.SingleOrDefaultAsync(candidate =>
                candidate.CourseId == courseId && candidate.LearnerId == accountId,
                cancellationToken);
            if (review == null)
            {
                review = new CourseReview
                {
                    CourseId = courseId,
                    LearnerId = accountId,
                    CreatedAt = now
                };
                _db.Add(review);
            }
            review.ReleaseId = enrollment.ReleaseId.Value;
            review.Rating = request.Rating;
            review.Body = request.Body.Trim();
            review.Status = "Published";
            review.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            await UpdateCourseAggregateAsync(courseId, cancellationToken);
            return Ok(review);
        }

        [HttpPut("reviews/{reviewId:guid}/reply")]
        [Authorize]
        public async Task<IActionResult> Reply(
            Guid reviewId,
            [FromBody] ReplyRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.Social) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var review = await _db.CourseReviews.SingleOrDefaultAsync(candidate => candidate.Id == reviewId, cancellationToken);
            if (review == null) return NotFound();
            if (!await _authorization.CanManageCourseAsync(accountId, review.CourseId, cancellationToken)) return Forbid();
            var reply = await _db.CourseReviewReplies.SingleOrDefaultAsync(candidate =>
                candidate.ReviewId == reviewId && candidate.AuthorAccountId == accountId,
                cancellationToken);
            if (reply == null)
            {
                reply = new CourseReviewReply { ReviewId = reviewId, AuthorAccountId = accountId, CreatedAt = UtcNow() };
                _db.Add(reply);
            }
            reply.Body = request.Body.Trim();
            reply.Status = "Published";
            reply.UpdatedAt = UtcNow();
            _db.Add(new OutboxMessage
            {
                Type = "notification.delivery",
                AggregateType = nameof(CourseReviewReply),
                AggregateId = reply.Id,
                DeduplicationKey = $"review-reply:{reply.Id}:{review.LearnerId}",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    LearnerId = review.LearnerId,
                    Type = "review_reply",
                    Title = "An instructor replied to your review",
                    Message = "Open the course review to read the reply.",
                    LinkUrl = $"/courses/{review.CourseId}",
                    Preference = "ReviewReplies"
                }),
                OccurredAt = UtcNow(),
                AvailableAt = UtcNow(),
                ActorLearnerId = accountId,
                CorrelationId = HttpContext.TraceIdentifier
            });
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(reply);
        }

        [HttpPost("reviews/{reviewId:guid}/reports")]
        [Authorize]
        public async Task<IActionResult> ReportReview(
            Guid reviewId,
            [FromBody] ReportRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.Social) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _db.CourseReviews.AnyAsync(review => review.Id == reviewId, cancellationToken)) return NotFound();
            if (await _db.CourseReviewReports.AnyAsync(report => report.ReviewId == reviewId && report.ReporterAccountId == accountId, cancellationToken))
                return Conflict(new { message = "You already reported this review." });
            var report = new CourseReviewReport
            {
                ReviewId = reviewId,
                ReporterAccountId = accountId,
                Reason = request.Reason,
                Detail = request.Detail.Trim(),
                CreatedAt = UtcNow()
            };
            _db.Add(report);
            _db.Add(new ModerationCase
            {
                ResourceType = nameof(CourseReview),
                ResourceId = reviewId,
                Status = "Open",
                Priority = request.Reason is "Harassment" or "ConflictOfInterest" ? "High" : "Normal",
                CreatedAt = UtcNow(),
                UpdatedAt = UtcNow()
            });
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { report.Id, report.Status });
        }

        [HttpPost("moderation/{caseId:guid}/decision")]
        [Authorize]
        public async Task<IActionResult> Moderate(
            Guid caseId,
            [FromBody] ModerationDecisionRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.Social) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var allowed = await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.PlatformModerator, cancellationToken)
                || await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.PlatformAdmin, cancellationToken);
            if (!allowed || !int.TryParse(User.FindFirst("mfa_level")?.Value, out var mfaLevel) || mfaLevel < 1) return Forbid();
            var moderation = await _db.ModerationCases.SingleOrDefaultAsync(candidate => candidate.Id == caseId, cancellationToken);
            if (moderation == null || moderation.Status != "Open") return NotFound();
            if (moderation.ResourceType != nameof(CourseReview)) return BadRequest();
            var review = await _db.CourseReviews.SingleAsync(candidate => candidate.Id == moderation.ResourceId, cancellationToken);
            review.Status = request.Decision == "HideReview" ? "Hidden" : "Published";
            review.ModeratedAt = UtcNow();
            review.ModeratedByAccountId = accountId;
            moderation.Status = "Resolved";
            moderation.Resolution = $"{request.Decision}: {request.Reason.Trim()}";
            moderation.AssignedModeratorId = accountId;
            moderation.ResolvedAt = UtcNow();
            moderation.UpdatedAt = UtcNow();
            var reports = await _db.CourseReviewReports.Where(report => report.ReviewId == review.Id && report.Status == "Open").ToListAsync(cancellationToken);
            foreach (var report in reports)
            {
                report.Status = "Resolved";
                report.ResolvedAt = UtcNow();
                report.ResolvedByAccountId = accountId;
            }
            await _db.SaveChangesAsync(cancellationToken);
            await UpdateCourseAggregateAsync(review.CourseId, cancellationToken);
            return NoContent();
        }

        private async Task UpdateCourseAggregateAsync(Guid courseId, CancellationToken cancellationToken)
        {
            var ratings = await _db.CourseReviews
                .Where(review => review.CourseId == courseId && review.Status == "Published")
                .Select(review => review.Rating)
                .ToListAsync(cancellationToken);
            var course = await _db.Courses.SingleAsync(candidate => candidate.Id == courseId, cancellationToken);
            course.Rating = ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1);
            course.ReviewCount = ratings.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
