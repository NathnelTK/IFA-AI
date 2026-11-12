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
    [Route("api/certificates")]
    public sealed class CertificatesController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly IObjectStorageService _storage;
        private readonly FeatureOptions _features;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly TimeProvider _timeProvider;

        public CertificatesController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            IObjectStorageService storage,
            IOptions<FeatureOptions> features,
            IOptions<ObjectStorageOptions> storageOptions,
            TimeProvider timeProvider)
        {
            _db = db;
            _authorization = authorization;
            _storage = storage;
            _features = features.Value;
            _storageOptions = storageOptions.Value;
            _timeProvider = timeProvider;
        }

        public sealed class RevokeRequest
        {
            [Required, StringLength(1000)] public string Reason { get; set; } = string.Empty;
        }

        [HttpGet("verify/{serialNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(
            string serialNumber,
            [FromQuery] string code,
            CancellationToken cancellationToken)
        {
            if (!_features.Certificates) return NotFound();
            var certificate = await _db.CourseCertificates.SingleOrDefaultAsync(candidate =>
                candidate.SerialNumber == serialNumber.ToUpper()
                && candidate.VerificationCode == code.ToLower(),
                cancellationToken);
            if (certificate == null) return NotFound();
            var completion = await _db.CourseCompletionRecords.SingleAsync(candidate => candidate.Id == certificate.CompletionRecordId, cancellationToken);
            using var snapshot = JsonDocument.Parse(completion.SnapshotJson);
            var root = snapshot.RootElement;
            return Ok(new
            {
                valid = certificate.RevokedAt == null && certificate.Status == "Ready",
                certificate.SerialNumber,
                certificate.Status,
                learnerName = root.GetProperty("learnerName").GetString(),
                courseTitle = root.GetProperty("courseTitle").GetString(),
                releaseVersion = root.GetProperty("releaseVersion").GetInt32(),
                completion.CompletedAt,
                certificate.IssuedAt,
                certificate.RevokedAt,
                certificate.RevocationReason,
                completion.EvidenceHash
            });
        }

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> Mine(CancellationToken cancellationToken)
        {
            if (!_features.Certificates) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var certificates = await _db.CourseCompletionRecords
                .Where(record => record.LearnerId == accountId)
                .Join(_db.CourseCertificates,
                    record => record.Id,
                    certificate => certificate.CompletionRecordId,
                    (record, certificate) => new
                    {
                        certificate.Id,
                        certificate.SerialNumber,
                        certificate.Status,
                        certificate.IssuedAt,
                        certificate.RenderedAt,
                        certificate.RevokedAt,
                        record.CourseId,
                        record.ReleaseId,
                        record.CompletedAt,
                        record.SnapshotJson
                    })
                .OrderByDescending(item => item.CompletedAt)
                .ToListAsync(cancellationToken);
            return Ok(certificates.Select(item => new
            {
                item.Id,
                item.SerialNumber,
                item.Status,
                item.IssuedAt,
                item.RenderedAt,
                item.RevokedAt,
                item.CourseId,
                item.ReleaseId,
                item.CompletedAt,
                courseTitle = CourseTitle(item.SnapshotJson)
            }));
        }

        [HttpGet("{id:guid}/download")]
        [Authorize]
        public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        {
            if (!_features.Certificates) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var certificate = await _db.CourseCertificates.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (certificate == null || certificate.Status != "Ready" || certificate.RevokedAt != null || certificate.PdfStorageKey == null)
                return NotFound();
            var completion = await _db.CourseCompletionRecords.SingleAsync(candidate => candidate.Id == certificate.CompletionRecordId, cancellationToken);
            if (completion.LearnerId != accountId
                && !await _authorization.CanManageCourseAsync(accountId, completion.CourseId, cancellationToken)) return Forbid();
            var url = await _storage.CreateReadUrlAsync(
                certificate.PdfStorageKey,
                $"witforge-{certificate.SerialNumber}.pdf",
                TimeSpan.FromMinutes(_storageOptions.DownloadUrlMinutes),
                cancellationToken);
            return Redirect(url);
        }

        [HttpPost("{id:guid}/revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke(
            Guid id,
            [FromBody] RevokeRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.Certificates) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!int.TryParse(User.FindFirst("mfa_level")?.Value, out var mfaLevel) || mfaLevel < 1) return Forbid();
            var certificate = await _db.CourseCertificates.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (certificate == null) return NotFound();
            var completion = await _db.CourseCompletionRecords.SingleAsync(candidate => candidate.Id == certificate.CompletionRecordId, cancellationToken);
            var platformAdmin = await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.PlatformAdmin, cancellationToken);
            if (!platformAdmin && !await _authorization.CanManageCourseAsync(accountId, completion.CourseId, cancellationToken)) return Forbid();
            if (certificate.RevokedAt == null)
            {
                certificate.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
                certificate.RevokedByAccountId = accountId;
                certificate.RevocationReason = request.Reason.Trim();
                certificate.Status = "Revoked";
                await _db.SaveChangesAsync(cancellationToken);
            }
            return NoContent();
        }

        private static string? CourseTitle(string snapshotJson)
        {
            using var document = JsonDocument.Parse(snapshotJson);
            return document.RootElement.GetProperty("courseTitle").GetString();
        }
    }
}
