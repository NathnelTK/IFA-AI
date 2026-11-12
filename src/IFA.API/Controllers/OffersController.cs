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
    [Route("api/offers")]
    public sealed class OffersController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;

        public OffersController(
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

        public sealed class SaveOfferRequest
        {
            [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
            public Guid ReleaseId { get; set; }
            public Guid? CohortId { get; set; }
            [Range(0.01, 9_999_999_999.99)] public decimal PriceAmount { get; set; }
            [Required, RegularExpression("^ETB$")] public string Currency { get; set; } = "ETB";
            public DateTime? AvailableFrom { get; set; }
            public DateTime? AvailableUntil { get; set; }
            [Range(1, int.MaxValue)] public int? SeatLimit { get; set; }
            [StringLength(20_000)] public string TermsJson { get; set; } = "{}";
        }

        [HttpGet("course/{courseId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> ActiveForCourse(Guid courseId, CancellationToken cancellationToken)
        {
            if (!_features.Offers) return NotFound();
            var now = UtcNow();
            var offer = await _db.CourseOffers
                .Where(candidate => candidate.CourseId == courseId
                    && candidate.IsActive
                    && (!candidate.AvailableFrom.HasValue || candidate.AvailableFrom <= now)
                    && (!candidate.AvailableUntil.HasValue || candidate.AvailableUntil > now))
                .OrderByDescending(candidate => candidate.Version)
                .FirstOrDefaultAsync(cancellationToken);
            return offer == null ? NotFound() : Ok(offer);
        }

        [HttpPost("course/{courseId:guid}")]
        [Authorize]
        public async Task<IActionResult> CreateVersion(
            Guid courseId,
            [FromBody] SaveOfferRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.Offers) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.CanManageCourseAsync(accountId, courseId, cancellationToken)) return Forbid();
            if (decimal.Round(request.PriceAmount, 2) != request.PriceAmount
                || request.AvailableUntil <= request.AvailableFrom
                || !ValidJson(request.TermsJson))
                return BadRequest(new { message = "Review the offer price, dates, and structured terms." });
            var release = await _db.CourseReleases.SingleOrDefaultAsync(candidate =>
                candidate.Id == request.ReleaseId && candidate.CourseId == courseId,
                cancellationToken);
            if (release == null) return BadRequest(new { message = "Choose a published release for this course." });
            if (request.CohortId.HasValue
                && !await _db.CourseCohorts.AnyAsync(cohort =>
                    cohort.Id == request.CohortId.Value
                    && cohort.CourseId == courseId
                    && cohort.ReleaseId == request.ReleaseId,
                    cancellationToken))
                return BadRequest(new { message = "The selected cohort is not bound to this release." });

            var prior = await _db.CourseOffers
                .Where(offer => offer.CourseId == courseId && offer.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var offer in prior) offer.IsActive = false;
            var version = (await _db.CourseOffers
                .Where(offer => offer.CourseId == courseId)
                .MaxAsync(offer => (int?)offer.Version, cancellationToken) ?? 0) + 1;
            var now = UtcNow();
            var created = new CourseOffer
            {
                CourseId = courseId,
                ReleaseId = request.ReleaseId,
                CohortId = request.CohortId,
                Version = version,
                Name = request.Name.Trim(),
                PurchaseType = "OneTime",
                PriceAmount = request.PriceAmount,
                Currency = "ETB",
                IsActive = true,
                AvailableFrom = request.AvailableFrom,
                AvailableUntil = request.AvailableUntil,
                SeatLimit = request.SeatLimit,
                TermsJson = request.TermsJson,
                CreatedByAccountId = accountId,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Add(created);
            var course = await _db.Courses.SingleAsync(candidate => candidate.Id == courseId, cancellationToken);
            course.IsPaid = true;
            course.PriceAmount = created.PriceAmount;
            course.Currency = created.Currency;
            course.UpdatedAt = now;
            course.UpdatedByAccountId = accountId;
            await _db.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(ActiveForCourse), new { courseId }, created);
        }

        private static bool ValidJson(string value)
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                return document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
