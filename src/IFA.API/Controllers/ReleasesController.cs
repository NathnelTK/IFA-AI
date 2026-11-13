using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/releases")]
    public sealed class ReleasesController : BaseApiController
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly IReleaseProgressService _progress;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;

        public ReleasesController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            IReleaseProgressService progress,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider)
        {
            _db = db;
            _authorization = authorization;
            _progress = progress;
            _features = features.Value;
            _timeProvider = timeProvider;
        }

        public sealed class QuizSubmissionRequest
        {
            [Required]
            public List<int> Answers { get; set; } = [];
        }

        [HttpGet("course/{courseId:guid}/mine")]
        public async Task<IActionResult> GetMyCourseRelease(Guid courseId, CancellationToken cancellationToken)
        {
            if (!_features.ImmutableReleases) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var enrollment = await _db.CourseEnrollments.SingleOrDefaultAsync(candidate =>
                candidate.CourseId == courseId
                && candidate.LearnerId == accountId
                && candidate.Status == "Active",
                cancellationToken);
            Guid? releaseId = enrollment?.ReleaseId;
            if (!releaseId.HasValue && await _authorization.CanManageCourseAsync(accountId, courseId, cancellationToken))
            {
                releaseId = await _db.Courses
                    .Where(course => course.Id == courseId)
                    .Select(course => course.ActiveReleaseId)
                    .SingleOrDefaultAsync(cancellationToken);
            }
            if (!releaseId.HasValue) return Forbid();
            return await ReleaseResultAsync(releaseId.Value, enrollment, cancellationToken);
        }

        [HttpGet("{releaseId:guid}")]
        public async Task<IActionResult> GetRelease(Guid releaseId, CancellationToken cancellationToken)
        {
            if (!_features.ImmutableReleases) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var release = await _db.CourseReleases.SingleOrDefaultAsync(candidate => candidate.Id == releaseId, cancellationToken);
            if (release == null) return NotFound();
            var enrollment = await _db.CourseEnrollments.SingleOrDefaultAsync(candidate =>
                candidate.ReleaseId == releaseId
                && candidate.LearnerId == accountId
                && candidate.Status == "Active",
                cancellationToken);
            if (enrollment == null && !await _authorization.CanManageCourseAsync(accountId, release.CourseId, cancellationToken))
            {
                return Forbid();
            }
            return await ReleaseResultAsync(releaseId, enrollment, cancellationToken);
        }

        [HttpPost("{releaseId:guid}/lessons/{lessonId:guid}/complete")]
        public async Task<IActionResult> CompleteLesson(
            Guid releaseId,
            Guid lessonId,
            CancellationToken cancellationToken)
        {
            if (!_features.ImmutableReleases) return NotFound();
            var enrollment = await RequireEnrollmentAsync(releaseId, cancellationToken);
            var exists = await _db.ReleaseLessons.AnyAsync(lesson =>
                lesson.Id == lessonId
                && _db.ReleaseModules.Where(module => module.ReleaseId == releaseId)
                    .Select(module => module.Id)
                    .Contains(lesson.ReleaseModuleId),
                cancellationToken);
            if (!exists) return NotFound();
            await _progress.RecordAsync(
                enrollment.Id,
                "Lesson",
                lessonId,
                100,
                "Completed",
                "{}",
                cancellationToken);
            return Ok(new { progressPercentage = await _progress.RecalculateAsync(enrollment.Id, cancellationToken) });
        }

        [HttpPost("{releaseId:guid}/quizzes/{quizId:guid}/attempts")]
        public async Task<IActionResult> SubmitQuiz(
            Guid releaseId,
            Guid quizId,
            [FromBody] QuizSubmissionRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.ImmutableReleases) return NotFound();
            var enrollment = await RequireEnrollmentAsync(releaseId, cancellationToken);
            var quiz = await _db.ReleaseQuizzes.SingleOrDefaultAsync(candidate => candidate.Id == quizId, cancellationToken);
            if (quiz == null
                || !await _db.ReleaseModules.AnyAsync(module => module.Id == quiz.ReleaseModuleId && module.ReleaseId == releaseId, cancellationToken))
            {
                return NotFound();
            }
            var release = await _db.CourseReleases.SingleAsync(candidate => candidate.Id == releaseId, cancellationToken);
            using var snapshot = JsonDocument.Parse(release.SnapshotJson);
            var maxAttempts = snapshot.RootElement.TryGetProperty("maxQuizAttempts", out var maxElement)
                ? maxElement.GetInt32()
                : 3;
            var attempts = await _db.ReleaseQuizAttempts.CountAsync(candidate =>
                candidate.EnrollmentId == enrollment.Id && candidate.ReleaseQuizId == quiz.Id,
                cancellationToken);
            if (attempts >= maxAttempts)
            {
                return Conflict(new { message = "The attempt limit for this release quiz has been reached." });
            }

            var questions = JsonSerializer.Deserialize<List<ReleaseQuestion>>(quiz.DefinitionJson, JsonOptions) ?? [];
            if (questions.Count == 0 || request.Answers.Count != questions.Count)
            {
                return BadRequest(new { message = "Submit one answer for every quiz question." });
            }
            var correct = questions.Select((question, index) => request.Answers[index] == question.CorrectOptionIndex).Count(value => value);
            var score = (int)Math.Round(correct * 100d / questions.Count, MidpointRounding.AwayFromZero);
            var passed = score >= quiz.PassingScorePercentage;
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var attempt = new ReleaseQuizAttempt
            {
                EnrollmentId = enrollment.Id,
                ReleaseQuizId = quiz.Id,
                AnswersJson = JsonSerializer.Serialize(request.Answers),
                ScorePercentage = score,
                IsPassed = passed,
                AttemptedAt = now
            };
            _db.Add(attempt);
            await _db.SaveChangesAsync(cancellationToken);
            await _progress.RecordAsync(
                enrollment.Id,
                "Quiz",
                quiz.Id,
                passed ? 100 : 0,
                passed ? "Completed" : "Attempted",
                JsonSerializer.Serialize(new { attemptId = attempt.Id, score, passed }),
                cancellationToken);
            return Ok(new
            {
                attempt.Id,
                scorePercentage = score,
                isPassed = passed,
                attemptsRemaining = Math.Max(0, maxAttempts - attempts - 1),
                explanations = questions.Select((question, index) => new
                {
                    questionId = question.Id,
                    isCorrect = request.Answers[index] == question.CorrectOptionIndex,
                    question.Explanation
                })
            });
        }

        private async Task<IActionResult> ReleaseResultAsync(
            Guid releaseId,
            CourseEnrollment? enrollment,
            CancellationToken cancellationToken)
        {
            var release = await _db.CourseReleases.SingleAsync(candidate => candidate.Id == releaseId, cancellationToken);
            var modules = await _db.ReleaseModules
                .Where(module => module.ReleaseId == releaseId)
                .OrderBy(module => module.ModuleNumber)
                .ToListAsync(cancellationToken);
            var moduleIds = modules.Select(module => module.Id).ToArray();
            var lessons = await _db.ReleaseLessons
                .Where(lesson => moduleIds.Contains(lesson.ReleaseModuleId))
                .OrderBy(lesson => lesson.LessonNumber)
                .ToListAsync(cancellationToken);
            var quizzes = await _db.ReleaseQuizzes
                .Where(quiz => moduleIds.Contains(quiz.ReleaseModuleId))
                .OrderBy(quiz => quiz.OrderIndex)
                .ToListAsync(cancellationToken);
            var assignments = await _db.ReleaseAssignments
                .Where(assignment => assignment.ReleaseId == releaseId)
                .ToListAsync(cancellationToken);
            var progress = enrollment == null
                ? []
                : await _db.ReleaseItemProgress
                    .Where(item => item.EnrollmentId == enrollment.Id)
                    .ToListAsync(cancellationToken);
            using var snapshot = JsonDocument.Parse(release.SnapshotJson);
            var courseAssistantEnabled = !snapshot.RootElement.TryGetProperty("courseAssistantEnabled", out var assistantElement)
                || assistantElement.GetBoolean();
            var awardsCertificate = !snapshot.RootElement.TryGetProperty("awardsCertificate", out var certificateElement)
                || certificateElement.GetBoolean();
            var appearance = snapshot.RootElement.TryGetProperty("appearance", out var appearanceElement)
                && appearanceElement.ValueKind == JsonValueKind.Object
                    ? appearanceElement.Clone()
                    : (JsonElement?)null;

            return Ok(new
            {
                release.Id,
                release.CourseId,
                release.Version,
                release.Title,
                release.Slug,
                release.PublishedAt,
                courseAssistantEnabled,
                awardsCertificate,
                appearance,
                enrollmentId = enrollment?.Id,
                progressPercentage = enrollment?.ProgressPercentage ?? 0,
                modules = modules.Select(module => new
                {
                    module.Id,
                    module.ModuleNumber,
                    module.Title,
                    module.Summary,
                    module.EstimatedHours,
                    lessons = lessons.Where(lesson => lesson.ReleaseModuleId == module.Id),
                    quizzes = quizzes.Where(quiz => quiz.ReleaseModuleId == module.Id).Select(quiz => new
                    {
                        quiz.Id,
                        quiz.Title,
                        quiz.Kind,
                        quiz.OrderIndex,
                        quiz.PassingScorePercentage,
                        questions = (JsonSerializer.Deserialize<List<ReleaseQuestion>>(quiz.DefinitionJson, JsonOptions) ?? [])
                            .Select(question => new { question.Id, question.Prompt, question.Options })
                    })
                }),
                assignments,
                progress
            });
        }

        private async Task<CourseEnrollment> RequireEnrollmentAsync(Guid releaseId, CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            return await _db.CourseEnrollments.SingleOrDefaultAsync(candidate =>
                candidate.ReleaseId == releaseId
                && candidate.LearnerId == accountId
                && candidate.Status == "Active",
                cancellationToken) ?? throw new CourseAccessException(403, "An active enrollment for this release is required.");
        }

        private sealed class ReleaseQuestion
        {
            public Guid Id { get; set; }
            public string Prompt { get; set; } = string.Empty;
            public List<string> Options { get; set; } = [];
            public int CorrectOptionIndex { get; set; }
            public string Explanation { get; set; } = string.Empty;
        }
    }
}
