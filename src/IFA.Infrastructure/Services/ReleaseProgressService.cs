using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Services
{
    public sealed class ReleaseProgressService : IReleaseProgressService
    {
        private readonly ApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;
        private readonly FeatureOptions _features;

        public ReleaseProgressService(
            ApplicationDbContext db,
            TimeProvider timeProvider,
            IOptions<FeatureOptions> features)
        {
            _db = db;
            _timeProvider = timeProvider;
            _features = features.Value;
        }

        public async Task RecordAsync(
            Guid enrollmentId,
            string itemType,
            Guid itemId,
            decimal progressPercent,
            string state,
            string evidenceJson,
            CancellationToken cancellationToken = default)
        {
            var enrollment = await _db.CourseEnrollments.SingleAsync(candidate => candidate.Id == enrollmentId, cancellationToken);
            if (!enrollment.ReleaseId.HasValue)
            {
                throw new InvalidOperationException("The enrollment is not bound to an immutable release.");
            }
            var progress = await _db.ReleaseItemProgress.SingleOrDefaultAsync(candidate =>
                candidate.EnrollmentId == enrollment.Id
                && candidate.ItemType == itemType
                && candidate.ItemId == itemId,
                cancellationToken);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (progress == null)
            {
                progress = new ReleaseItemProgress
                {
                    EnrollmentId = enrollment.Id,
                    LearnerId = enrollment.LearnerId,
                    ReleaseId = enrollment.ReleaseId.Value,
                    ItemType = itemType,
                    ItemId = itemId
                };
                _db.ReleaseItemProgress.Add(progress);
            }
            progress.State = state;
            progress.ProgressPercent = Math.Clamp(progressPercent, 0, 100);
            progress.EvidenceJson = evidenceJson;
            progress.UpdatedAt = now;
            progress.CompletedAt = progress.ProgressPercent >= 100 || state == "Completed" ? now : null;
            await _db.SaveChangesAsync(cancellationToken);
            await RecalculateAsync(enrollmentId, cancellationToken);
        }

        public async Task<int> RecalculateAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _db.CourseEnrollments.SingleAsync(candidate => candidate.Id == enrollmentId, cancellationToken);
            if (!enrollment.ReleaseId.HasValue)
            {
                return enrollment.ProgressPercentage;
            }
            var releaseId = enrollment.ReleaseId.Value;
            var lessonIds = await _db.ReleaseLessons
                .Where(lesson => _db.ReleaseModules
                    .Where(module => module.ReleaseId == releaseId)
                    .Select(module => module.Id)
                    .Contains(lesson.ReleaseModuleId))
                .Select(lesson => lesson.Id)
                .ToListAsync(cancellationToken);
            var quizIds = await _db.ReleaseQuizzes
                .Where(quiz => _db.ReleaseModules
                    .Where(module => module.ReleaseId == releaseId)
                    .Select(module => module.Id)
                    .Contains(quiz.ReleaseModuleId))
                .Select(quiz => quiz.Id)
                .ToListAsync(cancellationToken);
            var assignmentIds = await _db.ReleaseAssignments
                .Where(assignment => assignment.ReleaseId == releaseId)
                .Select(assignment => assignment.Id)
                .ToListAsync(cancellationToken);
            var allItems = lessonIds.Concat(quizIds).Concat(assignmentIds).ToHashSet();
            var completed = await _db.ReleaseItemProgress
                .Where(progress => progress.EnrollmentId == enrollment.Id
                    && progress.CompletedAt != null
                    && allItems.Contains(progress.ItemId))
                .Select(progress => progress.ItemId)
                .Distinct()
                .CountAsync(cancellationToken);
            var percentage = allItems.Count == 0
                ? 0
                : (int)Math.Round(completed * 100d / allItems.Count, MidpointRounding.AwayFromZero);
            enrollment.ProgressPercentage = Math.Clamp(percentage, 0, 100);
            enrollment.CompletedAt = enrollment.ProgressPercentage == 100
                ? enrollment.CompletedAt ?? _timeProvider.GetUtcNow().UtcDateTime
                : null;
            enrollment.LastAccessedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _db.SaveChangesAsync(cancellationToken);
            if (enrollment.ProgressPercentage == 100 && _features.Certificates)
            {
                await EnsureCompletionAsync(enrollment, cancellationToken);
            }
            return enrollment.ProgressPercentage;
        }

        private async Task EnsureCompletionAsync(
            CourseEnrollment enrollment,
            CancellationToken cancellationToken)
        {
            if (!enrollment.ReleaseId.HasValue
                || await _db.CourseCompletionRecords.AnyAsync(record => record.EnrollmentId == enrollment.Id, cancellationToken))
            {
                return;
            }
            var release = await _db.CourseReleases.SingleAsync(candidate => candidate.Id == enrollment.ReleaseId.Value, cancellationToken);
            var course = await _db.Courses.SingleAsync(candidate => candidate.Id == enrollment.CourseId, cancellationToken);
            var learner = await _db.Learners.SingleAsync(candidate => candidate.Id == enrollment.LearnerId, cancellationToken);
            var evidence = await _db.ReleaseItemProgress
                .Where(progress => progress.EnrollmentId == enrollment.Id)
                .OrderBy(progress => progress.ItemType)
                .ThenBy(progress => progress.ItemId)
                .Select(progress => new
                {
                    progress.ItemType,
                    progress.ItemId,
                    progress.State,
                    progress.ProgressPercent,
                    progress.CompletedAt,
                    progress.EvidenceJson
                })
                .ToListAsync(cancellationToken);
            var completedAt = enrollment.CompletedAt ?? _timeProvider.GetUtcNow().UtcDateTime;
            var snapshotJson = JsonSerializer.Serialize(new
            {
                enrollmentId = enrollment.Id,
                learnerId = learner.Id,
                learnerName = learner.Name,
                courseId = course.Id,
                courseTitle = release.Title,
                releaseId = release.Id,
                releaseVersion = release.Version,
                release.ContentHash,
                completedAt,
                evidence
            });
            var evidenceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson))).ToLowerInvariant();
            var completion = new CourseCompletionRecord
            {
                EnrollmentId = enrollment.Id,
                LearnerId = learner.Id,
                CourseId = course.Id,
                ReleaseId = release.Id,
                SnapshotJson = snapshotJson,
                EvidenceHash = evidenceHash,
                CompletedAt = completedAt,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            using var releaseSnapshot = JsonDocument.Parse(release.SnapshotJson);
            var awardsCertificate = !releaseSnapshot.RootElement.TryGetProperty("awardsCertificate", out var certificateSetting)
                || certificateSetting.GetBoolean();
            _db.CourseCompletionRecords.Add(completion);
            if (!awardsCertificate)
            {
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            var certificate = new CourseCertificate
            {
                CompletionRecordId = completion.Id,
                SerialNumber = $"WF-{completedAt:yyyy}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
                VerificationCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant(),
                Status = "Queued",
                IssuedAt = completedAt
            };
            _db.CourseCertificates.Add(certificate);
            _db.CertificateRenderJobs.Add(new CertificateRenderJob
            {
                CertificateId = certificate.Id,
                State = "Queued",
                AvailableAt = _timeProvider.GetUtcNow().UtcDateTime,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
