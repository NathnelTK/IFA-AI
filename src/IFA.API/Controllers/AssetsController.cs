using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [EnableRateLimiting("uploads")]
    [Route("api/assets")]
    public sealed partial class AssetsController : BaseApiController
    {
        private const long MultipartThresholdBytes = 50L * 1024 * 1024;
        private const int MultipartPartSizeBytes = 10 * 1024 * 1024;
        private static readonly IReadOnlyDictionary<string, (long MaxBytes, string[] Types)> Policies =
            new Dictionary<string, (long, string[])>(StringComparer.OrdinalIgnoreCase)
            {
                ["CourseCover"] = (10L * 1024 * 1024, ["image/jpeg", "image/png", "image/webp"]),
                ["CourseMaterial"] = (250L * 1024 * 1024, [
                    "application/pdf",
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    "text/plain",
                    "text/markdown",
                    "image/jpeg",
                    "image/png",
                    "image/webp"
                ]),
                ["AssignmentSubmission"] = (25L * 1024 * 1024, [
                    "application/pdf",
                    "application/zip",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "text/plain",
                    "image/jpeg",
                    "image/png"
                ])
            };

        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly IObjectStorageService _storage;
        private readonly FeatureOptions _features;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly TimeProvider _timeProvider;

        public AssetsController(
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

        public sealed class CreateUploadRequest
        {
            public Guid? WorkspaceId { get; set; }
            public Guid? CourseId { get; set; }
            [Required, StringLength(64)]
            public string Purpose { get; set; } = string.Empty;
            [Required, StringLength(256)]
            public string FileName { get; set; } = string.Empty;
            [Required, StringLength(128)]
            public string ContentType { get; set; } = string.Empty;
            [Range(1, long.MaxValue)]
            public long SizeBytes { get; set; }
            [Required, RegularExpression("^[a-fA-F0-9]{64}$")]
            public string Sha256 { get; set; } = string.Empty;
        }

        public sealed class CompleteMultipartRequest
        {
            [MaxLength(10_000)]
            public List<CompletePartRequest> Parts { get; set; } = [];
        }

        public sealed class CompletePartRequest
        {
            [Range(1, 10_000)] public int PartNumber { get; set; }
            [Required, StringLength(256)] public string ETag { get; set; } = string.Empty;
        }

        [HttpPost("uploads")]
        public async Task<IActionResult> CreateUpload(
            [FromBody] CreateUploadRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.AssetUploads) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!Policies.TryGetValue(request.Purpose, out var policy)
                || request.SizeBytes > Math.Min(policy.MaxBytes, _storageOptions.MaxUploadBytes)
                || !policy.Types.Contains(request.ContentType.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { code = "asset_policy_rejected", message = "This file type or size is not allowed for the selected purpose." });
            }

            Guid workspaceId;
            AssetVisibility visibility;
            if (request.Purpose.Equals("AssignmentSubmission", StringComparison.OrdinalIgnoreCase))
            {
                if (!request.CourseId.HasValue) return BadRequest(new { message = "Assignment uploads require a course." });
                var access = await CourseAccess.GetAsync(_db, request.CourseId.Value, accountId, cancellationToken);
                if (access == null || !access.HasContentAccess || access.IsOwner) return Forbid();
                if (!access.Course.WorkspaceId.HasValue) return Conflict(new { message = "This course has not been assigned to a workspace." });
                workspaceId = access.Course.WorkspaceId.Value;
                visibility = AssetVisibility.EnrolledLearners;
            }
            else
            {
                if (!request.WorkspaceId.HasValue
                    || !await _authorization.CanManageWorkspaceAsync(accountId, request.WorkspaceId.Value, cancellationToken))
                {
                    return Forbid();
                }
                workspaceId = request.WorkspaceId.Value;
                visibility = AssetVisibility.Private;
            }

            var now = UtcNow();
            var assetId = Guid.NewGuid();
            var safeFileName = SafeFileName(request.FileName);
            var key = $"quarantine/{workspaceId:N}/{assetId:N}/{safeFileName}";
            var asset = new Asset
            {
                Id = assetId,
                WorkspaceId = workspaceId,
                OwnerAccountId = accountId,
                Purpose = request.Purpose,
                OriginalFileName = safeFileName,
                ContentType = request.ContentType.Trim().ToLowerInvariant(),
                SizeBytes = request.SizeBytes,
                ExpectedSha256 = request.Sha256.ToLowerInvariant(),
                QuarantineStorageKey = key,
                Status = AssetStatus.PendingUpload,
                Visibility = visibility,
                CreatedAt = now,
                UpdatedAt = now
            };
            var upload = new UploadSession
            {
                AssetId = asset.Id,
                State = "Pending",
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(_storageOptions.UploadUrlMinutes)
            };
            _db.Add(asset);
            _db.Add(upload);
            await _db.SaveChangesAsync(cancellationToken);

            if (asset.SizeBytes > MultipartThresholdBytes)
            {
                upload.MultipartUploadId = await _storage.BeginMultipartUploadAsync(key, asset.ContentType, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                return Ok(new
                {
                    asset.Id,
                    uploadSessionId = upload.Id,
                    multipart = true,
                    partSizeBytes = MultipartPartSizeBytes,
                    partCount = (int)Math.Ceiling(asset.SizeBytes / (double)MultipartPartSizeBytes),
                    upload.ExpiresAt,
                    asset.Status
                });
            }
            else
            {
                var signed = await _storage.CreateUploadUrlAsync(
                    key,
                    asset.ContentType,
                    TimeSpan.FromMinutes(_storageOptions.UploadUrlMinutes),
                    cancellationToken);
                return Ok(new
                {
                    asset.Id,
                    uploadSessionId = upload.Id,
                    multipart = false,
                    uploadUrl = signed.Url,
                    requiredHeaders = signed.RequiredHeaders,
                    signed.ExpiresAt,
                    asset.Status
                });
            }
        }

        [HttpPost("uploads/{uploadSessionId:guid}/parts/{partNumber:int}")]
        public async Task<IActionResult> CreatePartUrl(
            Guid uploadSessionId,
            int partNumber,
            CancellationToken cancellationToken)
        {
            if (!_features.AssetUploads) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var upload = await _db.UploadSessions.SingleOrDefaultAsync(candidate => candidate.Id == uploadSessionId, cancellationToken);
            if (upload == null || upload.State != "Pending" || upload.ExpiresAt <= UtcNow() || string.IsNullOrWhiteSpace(upload.MultipartUploadId))
                return NotFound();
            var asset = await _db.Assets.SingleAsync(candidate => candidate.Id == upload.AssetId, cancellationToken);
            if (asset.OwnerAccountId != accountId) return Forbid();
            var expectedParts = (int)Math.Ceiling(asset.SizeBytes / (double)MultipartPartSizeBytes);
            if (partNumber < 1 || partNumber > expectedParts) return BadRequest(new { message = "Part number is outside this upload session." });
            var signed = await _storage.CreatePartUploadUrlAsync(
                asset.QuarantineStorageKey,
                upload.MultipartUploadId,
                partNumber,
                TimeSpan.FromMinutes(_storageOptions.UploadUrlMinutes),
                cancellationToken);
            return Ok(new { uploadUrl = signed.Url, requiredHeaders = signed.RequiredHeaders, signed.ExpiresAt });
        }

        [HttpPost("uploads/{uploadSessionId:guid}/complete")]
        public async Task<IActionResult> CompleteUpload(
            Guid uploadSessionId,
            [FromBody] CompleteMultipartRequest? multipart,
            CancellationToken cancellationToken)
        {
            if (!_features.AssetUploads) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var upload = await _db.UploadSessions.SingleOrDefaultAsync(candidate => candidate.Id == uploadSessionId, cancellationToken);
            if (upload == null) return NotFound();
            var asset = await _db.Assets.SingleAsync(candidate => candidate.Id == upload.AssetId, cancellationToken);
            if (asset.OwnerAccountId != accountId) return Forbid();
            if (upload.State == "Completed") return Ok(new { asset.Id, asset.Status });
            if (upload.ExpiresAt <= UtcNow() || upload.State != "Pending")
            {
                return Conflict(new { code = "upload_session_expired", message = "Start a new upload session." });
            }
            if (!string.IsNullOrWhiteSpace(upload.MultipartUploadId))
            {
                var expectedParts = (int)Math.Ceiling(asset.SizeBytes / (double)MultipartPartSizeBytes);
                if (multipart?.Parts.Count != expectedParts
                    || multipart.Parts.Select(part => part.PartNumber).Distinct().Count() != expectedParts)
                    return BadRequest(new { message = "Every multipart upload part and ETag is required." });
                await _storage.CompleteMultipartUploadAsync(
                    asset.QuarantineStorageKey,
                    upload.MultipartUploadId,
                    multipart.Parts.Select(part => new UploadedPart(part.PartNumber, part.ETag.Trim('"'))).ToArray(),
                    cancellationToken);
                upload.UploadedPartsJson = System.Text.Json.JsonSerializer.Serialize(multipart.Parts);
            }

            var metadata = await _storage.GetMetadataAsync(asset.QuarantineStorageKey, cancellationToken);
            if (metadata == null
                || metadata.SizeBytes != asset.SizeBytes
                || !string.Equals(metadata.ContentType, asset.ContentType, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { code = "upload_incomplete", message = "The uploaded object is missing or has an unexpected size." });
            }
            var now = UtcNow();
            upload.State = "Completed";
            upload.CompletedAt = now;
            asset.Status = AssetStatus.Quarantined;
            asset.UpdatedAt = now;
            _db.Add(new AssetProcessingJob
            {
                AssetId = asset.Id,
                Kind = "Scan",
                State = "Queued",
                AvailableAt = now,
                CorrelationId = HttpContext.TraceIdentifier,
                CreatedAt = now,
                UpdatedAt = now
            });
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { asset.Id, asset.Status });
        }

        [HttpDelete("uploads/{uploadSessionId:guid}")]
        public async Task<IActionResult> AbortUpload(Guid uploadSessionId, CancellationToken cancellationToken)
        {
            if (!_features.AssetUploads) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var upload = await _db.UploadSessions.SingleOrDefaultAsync(candidate => candidate.Id == uploadSessionId, cancellationToken);
            if (upload == null) return NoContent();
            var asset = await _db.Assets.SingleAsync(candidate => candidate.Id == upload.AssetId, cancellationToken);
            if (asset.OwnerAccountId != accountId) return Forbid();
            if (upload.State == "Pending" && !string.IsNullOrWhiteSpace(upload.MultipartUploadId))
            {
                await _storage.AbortMultipartUploadAsync(asset.QuarantineStorageKey, upload.MultipartUploadId, cancellationToken);
            }
            upload.State = "Aborted";
            upload.AbortedAt = UtcNow();
            asset.Status = AssetStatus.Deleted;
            asset.DeletedAt = UtcNow();
            asset.UpdatedAt = UtcNow();
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
        {
            if (!_features.AssetUploads) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var asset = await _db.Assets.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (asset == null) return NotFound();
            if (asset.OwnerAccountId != accountId
                && !await _authorization.CanManageWorkspaceAsync(accountId, asset.WorkspaceId, cancellationToken))
            {
                return Forbid();
            }
            return Ok(new
            {
                asset.Id,
                asset.Purpose,
                asset.OriginalFileName,
                asset.ContentType,
                asset.SizeBytes,
                asset.Status,
                asset.Visibility,
                asset.RejectionCode,
                asset.CreatedAt,
                asset.UpdatedAt
            });
        }

        [HttpPost("{id:guid}/attach/course/{courseId:guid}")]
        public async Task<IActionResult> AttachToCourse(
            Guid id,
            Guid courseId,
            CancellationToken cancellationToken)
        {
            if (!_features.AssetUploads) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.CanManageCourseAsync(accountId, courseId, cancellationToken)) return Forbid();
            var asset = await _db.Assets.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            var course = await _db.Courses.SingleOrDefaultAsync(candidate => candidate.Id == courseId, cancellationToken);
            if (asset == null || course == null) return NotFound();
            if (asset.Status != AssetStatus.Clean
                || asset.WorkspaceId != course.WorkspaceId
                || asset.Purpose is not ("CourseCover" or "CourseMaterial"))
            {
                return Conflict(new { message = "Only a clean course asset from the same workspace can be attached." });
            }

            var referencePurpose = asset.Purpose == "CourseCover" ? "Cover" : $"Material:{asset.Id}";
            if (asset.Purpose == "CourseCover")
            {
                var prior = await _db.AssetReferences.SingleOrDefaultAsync(reference =>
                    reference.EntityType == nameof(Course)
                    && reference.EntityId == course.Id
                    && reference.Purpose == "Cover",
                    cancellationToken);
                if (prior != null) _db.Remove(prior);
                course.CoverAssetId = asset.Id;
                course.CoverImageUrl = $"/api/assets/{asset.Id}/download";
                course.CoverImageSource = "Upload";
            }
            if (!await _db.AssetReferences.AnyAsync(reference =>
                reference.EntityType == nameof(Course)
                && reference.EntityId == course.Id
                && reference.Purpose == referencePurpose,
                cancellationToken))
            {
                _db.Add(new AssetReference
                {
                    AssetId = asset.Id,
                    EntityType = nameof(Course),
                    EntityId = course.Id,
                    Purpose = referencePurpose,
                    CreatedByAccountId = accountId
                });
            }
            course.UpdatedAt = UtcNow();
            course.UpdatedByAccountId = accountId;
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        {
            if (!_features.AssetUploads) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var asset = await _db.Assets.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (asset == null || asset.Status != AssetStatus.Clean || string.IsNullOrWhiteSpace(asset.CleanStorageKey))
            {
                return NotFound();
            }
            var allowed = asset.OwnerAccountId == accountId
                || await _authorization.CanManageWorkspaceAsync(accountId, asset.WorkspaceId, cancellationToken);
            if (!allowed)
            {
                var courseIds = await _db.AssetReferences
                    .Where(reference => reference.AssetId == asset.Id && reference.EntityType == "Course")
                    .Select(reference => reference.EntityId)
                    .ToListAsync(cancellationToken);
                foreach (var courseId in courseIds)
                {
                    var access = await CourseAccess.GetAsync(_db, courseId, accountId, cancellationToken);
                    if (access?.HasContentAccess == true)
                    {
                        allowed = true;
                        break;
                    }
                }
            }
            if (!allowed) return Forbid();

            var url = await _storage.CreateReadUrlAsync(
                asset.CleanStorageKey,
                asset.OriginalFileName,
                TimeSpan.FromMinutes(_storageOptions.DownloadUrlMinutes),
                cancellationToken);
            return Redirect(url);
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static string SafeFileName(string value)
        {
            var name = Path.GetFileName(value.Trim());
            name = UnsafeFileNameCharacters().Replace(name, "-").Trim('-', '.');
            if (string.IsNullOrWhiteSpace(name)) name = "upload.bin";
            return name[..Math.Min(name.Length, 180)];
        }

        [GeneratedRegex("[^A-Za-z0-9._-]+", RegexOptions.CultureInvariant)]
        private static partial Regex UnsafeFileNameCharacters();
    }
}
