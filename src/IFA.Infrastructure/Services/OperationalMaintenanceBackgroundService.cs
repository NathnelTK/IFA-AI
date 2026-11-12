using IFA.Infrastructure.Data;
using IFA.Infrastructure.Configuration;
using IFA.Domain.Entities;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class OperationalMaintenanceBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WorkerOptions _options;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<OperationalMaintenanceBackgroundService> _logger;

        public OperationalMaintenanceBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<WorkerOptions> options,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider,
            ILogger<OperationalMaintenanceBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _features = features.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Maintenance.Enabled)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Operational retention cleanup failed; it will be retried.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromMinutes(_options.Maintenance.IntervalMinutes),
                        _timeProvider,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        internal async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (db.Database.IsNpgsql())
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock(hashtext('witforge:operational-maintenance'))",
                    cancellationToken);
            }
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            await db.InternalRequestReplays
                .Where(item => item.ExpiresAt < now)
                .ExecuteDeleteAsync(cancellationToken);
            await db.MfaLoginChallenges
                .Where(item => item.ExpiresAt < now.AddDays(-1))
                .ExecuteDeleteAsync(cancellationToken);
            await db.OAuthAuthorizationCodes
                .Where(item => item.ExpiresAt < now.AddDays(-1))
                .ExecuteDeleteAsync(cancellationToken);
            await db.AccountSecurityTokens
                .Where(item => item.ExpiresAt < now.AddDays(-7))
                .ExecuteDeleteAsync(cancellationToken);
            await db.AuthSessions
                .Where(item => item.ExpiresAt < now.AddDays(-30)
                    || (item.RevokedAt != null && item.RevokedAt < now.AddDays(-30)))
                .ExecuteDeleteAsync(cancellationToken);
            var outboxCutoff = now.AddDays(-_options.Maintenance.ProcessedOutboxRetentionDays);
            await db.OutboxMessages
                .Where(item => item.ProcessedAt != null && item.ProcessedAt < outboxCutoff)
                .ExecuteDeleteAsync(cancellationToken);
            if (_features.UnifiedAuthoring)
            {
                await ReconcileLegacyAuthoringAsync(db, now, cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            if (_features.ImmutableReleases)
            {
                await ReconcileLegacyReleasesAsync(scope.ServiceProvider, db, now, cancellationToken);
            }
        }

        private static async Task ReconcileLegacyAuthoringAsync(
            ApplicationDbContext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var existingCourseIds = await db.AuthoringProjects
                .Select(project => project.CourseId)
                .ToListAsync(cancellationToken);
            var courses = await db.Courses
                .Where(course => !existingCourseIds.Contains(course.Id))
                .ToListAsync(cancellationToken);
            foreach (var course in courses)
            {
                if (!course.WorkspaceId.HasValue)
                {
                    continue;
                }
                var legacyProjects = await db.EducatorProjects
                    .Include(project => project.Sources)
                    .Where(project => project.CourseId == course.Id)
                    .OrderByDescending(project => project.Status == "Published")
                    .ThenByDescending(project => project.Status == "ReadyForReview")
                    .ThenByDescending(project => project.UpdatedAt)
                    .ThenBy(project => project.Id)
                    .ToListAsync(cancellationToken);
                var canonical = legacyProjects.FirstOrDefault();
                if (canonical == null && course.CreatorLearnerId.HasValue)
                {
                    canonical = new EducatorProject
                    {
                        EducatorLearnerId = course.CreatorLearnerId.Value,
                        WorkspaceId = course.WorkspaceId,
                        CourseId = course.Id,
                        Title = course.Title,
                        Description = course.Description,
                        Status = course.IsPublished ? "Published" : "ReadyForReview",
                        CreatedAt = course.CreatedAt,
                        UpdatedAt = course.UpdatedAt ?? course.CreatedAt
                    };
                    db.EducatorProjects.Add(canonical);
                    legacyProjects.Add(canonical);
                }

                var authoring = new AuthoringProject
                {
                    Id = canonical?.Id ?? Guid.NewGuid(),
                    WorkspaceId = course.WorkspaceId.Value,
                    CourseId = course.Id,
                    CreatedByAccountId = course.CreatorLearnerId,
                    LegacyEducatorProjectId = canonical?.Id,
                    Mode = canonical?.Sources.Count > 0 ? AuthoringMode.Materials : AuthoringMode.Manual,
                    State = course.IsPublished ? AuthoringProjectState.Published : AuthoringProjectState.Reviewing,
                    Title = course.Title,
                    CreatedAt = canonical?.CreatedAt ?? course.CreatedAt,
                    UpdatedAt = canonical?.UpdatedAt ?? course.UpdatedAt ?? course.CreatedAt
                };
                db.AuthoringProjects.Add(authoring);
                db.CourseGenerationRequests.Add(new CourseGenerationRequest
                {
                    AuthoringProjectId = authoring.Id,
                    Prompt = course.Description,
                    Category = course.Category,
                    TargetAudience = course.TargetAudience,
                    LearningObjectivesJson = System.Text.Json.JsonSerializer.Serialize(course.LearningObjectives),
                    ConstraintsJson = "{}",
                    CreatedAt = now,
                    UpdatedAt = now
                });
                foreach (var legacy in legacyProjects)
                {
                    db.LegacyProjectAliases.Add(new LegacyProjectAlias
                    {
                        LegacyProjectId = legacy.Id,
                        AuthoringProjectId = authoring.Id,
                        CreatedAt = now
                    });
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        private static async Task ReconcileLegacyReleasesAsync(
            IServiceProvider services,
            ApplicationDbContext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var releaseService = services.GetRequiredService<ICourseReleaseService>();
            var candidates = await db.Courses
                .Where(course => course.IsPublished && course.ActiveReleaseId == null && course.CreatorLearnerId != null)
                .Select(course => new { course.Id, ActorId = course.CreatorLearnerId!.Value })
                .ToListAsync(cancellationToken);
            foreach (var candidate in candidates)
            {
                try
                {
                    await releaseService.PublishAsync(candidate.Id, candidate.ActorId, cancellationToken);
                }
                catch (CourseReleaseValidationException ex)
                {
                    var resourceId = candidate.Id.ToString();
                    if (!await db.AuditEvents.AnyAsync(audit =>
                        audit.Action == "migration.release_backfill_blocked" && audit.ResourceId == resourceId,
                        cancellationToken))
                    {
                        db.AuditEvents.Add(new AuditEvent
                        {
                            Action = "migration.release_backfill_blocked",
                            ResourceType = nameof(Course),
                            ResourceId = resourceId,
                            Outcome = "ReviewRequired",
                            ErrorCode = "publication_validation_failed",
                            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { errors = ex.Errors }),
                            OccurredAt = now
                        });
                        await db.SaveChangesAsync(cancellationToken);
                    }
                }
            }

            var legacyOffers = await db.Courses
                .Where(course => course.IsPaid
                    && course.PriceAmount > 0
                    && course.ActiveReleaseId != null
                    && course.CreatorLearnerId != null
                    && !db.CourseOffers.Any(offer => offer.CourseId == course.Id))
                .ToListAsync(cancellationToken);
            foreach (var course in legacyOffers)
            {
                var cohortId = await db.CourseCohorts
                    .Where(cohort => cohort.CourseId == course.Id && cohort.ReleaseId == course.ActiveReleaseId)
                    .OrderByDescending(cohort => cohort.CreatedAt)
                    .Select(cohort => (Guid?)cohort.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                db.CourseOffers.Add(new CourseOffer
                {
                    CourseId = course.Id,
                    ReleaseId = course.ActiveReleaseId!.Value,
                    CohortId = cohortId,
                    Version = 1,
                    Name = "Legacy one-time enrollment",
                    PurchaseType = "OneTime",
                    PriceAmount = course.PriceAmount,
                    Currency = course.Currency,
                    IsActive = true,
                    AvailableFrom = course.EnrollmentOpensAt,
                    AvailableUntil = course.EnrollmentClosesAt,
                    SeatLimit = course.EnrollmentLimit,
                    TermsJson = "{\"source\":\"legacy\"}",
                    CreatedByAccountId = course.CreatorLearnerId.GetValueOrDefault(),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            if (legacyOffers.Count > 0) await db.SaveChangesAsync(cancellationToken);

            var enrollments = await db.CourseEnrollments
                .Where(enrollment => enrollment.ReleaseId == null && enrollment.Status == "Active")
                .ToListAsync(cancellationToken);
            foreach (var enrollment in enrollments)
            {
                var releaseId = await db.Courses
                    .Where(course => course.Id == enrollment.CourseId)
                    .Select(course => course.ActiveReleaseId)
                    .SingleOrDefaultAsync(cancellationToken);
                if (!releaseId.HasValue) continue;
                enrollment.ReleaseId = releaseId;
                enrollment.CohortId = await db.CourseCohorts
                    .Where(cohort => cohort.CourseId == enrollment.CourseId && cohort.ReleaseId == releaseId.Value)
                    .OrderByDescending(cohort => cohort.CreatedAt)
                    .Select(cohort => (Guid?)cohort.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await MigrateEnrollmentEvidenceAsync(db, enrollment, now, cancellationToken);
                await services.GetRequiredService<IReleaseProgressService>()
                    .RecalculateAsync(enrollment.Id, cancellationToken);
            }
        }

        private static async Task MigrateEnrollmentEvidenceAsync(
            ApplicationDbContext db,
            CourseEnrollment enrollment,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var releaseId = enrollment.ReleaseId!.Value;
            var lessonMap = await db.ReleaseLessons
                .Where(lesson => db.ReleaseModules
                    .Where(module => module.ReleaseId == releaseId)
                    .Select(module => module.Id)
                    .Contains(lesson.ReleaseModuleId))
                .ToDictionaryAsync(lesson => lesson.SourceLessonId, lesson => lesson.Id, cancellationToken);
            var legacyLessons = await db.LessonProgress
                .Where(progress => progress.LearnerId == enrollment.LearnerId
                    && progress.IsCompleted
                    && lessonMap.Keys.Contains(progress.LessonId))
                .ToListAsync(cancellationToken);
            foreach (var legacy in legacyLessons)
            {
                if (await db.ProgressMigrationRecords.AnyAsync(record =>
                    record.SourceType == nameof(LessonProgress) && record.SourceId == legacy.Id,
                    cancellationToken)) continue;
                db.ReleaseItemProgress.Add(new ReleaseItemProgress
                {
                    EnrollmentId = enrollment.Id,
                    LearnerId = enrollment.LearnerId,
                    ReleaseId = releaseId,
                    ItemType = "Lesson",
                    ItemId = lessonMap[legacy.LessonId],
                    State = "Completed",
                    ProgressPercent = 100,
                    EvidenceJson = System.Text.Json.JsonSerializer.Serialize(new { legacyLessonProgressId = legacy.Id }),
                    UpdatedAt = legacy.CompletedAt ?? now,
                    CompletedAt = legacy.CompletedAt ?? now
                });
                db.ProgressMigrationRecords.Add(new ProgressMigrationRecord
                {
                    EnrollmentId = enrollment.Id,
                    ReleaseId = releaseId,
                    SourceType = nameof(LessonProgress),
                    SourceId = legacy.Id,
                    Outcome = "Mapped",
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new { releaseLessonId = lessonMap[legacy.LessonId] }),
                    MigratedAt = now
                });
            }

            var quizMap = await db.ReleaseQuizzes
                .Where(quiz => db.ReleaseModules
                    .Where(module => module.ReleaseId == releaseId)
                    .Select(module => module.Id)
                    .Contains(quiz.ReleaseModuleId))
                .ToDictionaryAsync(quiz => quiz.SourceQuizId, quiz => quiz.Id, cancellationToken);
            var attempts = await db.QuizAttempts
                .Include(attempt => attempt.Answers)
                .Where(attempt => attempt.LearnerId == enrollment.LearnerId && quizMap.Keys.Contains(attempt.QuizId))
                .OrderBy(attempt => attempt.StartedAt)
                .ToListAsync(cancellationToken);
            foreach (var legacy in attempts)
            {
                if (await db.ProgressMigrationRecords.AnyAsync(record =>
                    record.SourceType == nameof(QuizAttempt) && record.SourceId == legacy.Id,
                    cancellationToken)) continue;
                var releaseQuizId = quizMap[legacy.QuizId];
                db.ReleaseQuizAttempts.Add(new ReleaseQuizAttempt
                {
                    EnrollmentId = enrollment.Id,
                    ReleaseQuizId = releaseQuizId,
                    AnswersJson = System.Text.Json.JsonSerializer.Serialize(
                        legacy.Answers.OrderBy(answer => answer.QuestionId).Select(answer => answer.SelectedOptionIndex)),
                    ScorePercentage = legacy.ScorePercentage,
                    IsPassed = legacy.IsPassed,
                    AttemptedAt = legacy.CompletedAt ?? legacy.StartedAt
                });
                if (legacy.IsPassed
                    && !await db.ReleaseItemProgress.AnyAsync(progress =>
                        progress.EnrollmentId == enrollment.Id && progress.ItemType == "Quiz" && progress.ItemId == releaseQuizId,
                        cancellationToken))
                {
                    db.ReleaseItemProgress.Add(new ReleaseItemProgress
                    {
                        EnrollmentId = enrollment.Id,
                        LearnerId = enrollment.LearnerId,
                        ReleaseId = releaseId,
                        ItemType = "Quiz",
                        ItemId = releaseQuizId,
                        State = "Completed",
                        ProgressPercent = 100,
                        EvidenceJson = System.Text.Json.JsonSerializer.Serialize(new { legacyQuizAttemptId = legacy.Id, legacy.ScorePercentage }),
                        UpdatedAt = legacy.CompletedAt ?? legacy.StartedAt,
                        CompletedAt = legacy.CompletedAt ?? legacy.StartedAt
                    });
                }
                db.ProgressMigrationRecords.Add(new ProgressMigrationRecord
                {
                    EnrollmentId = enrollment.Id,
                    ReleaseId = releaseId,
                    SourceType = nameof(QuizAttempt),
                    SourceId = legacy.Id,
                    Outcome = "Mapped",
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new { releaseQuizId }),
                    MigratedAt = now
                });
            }

            var assessments = await db.Assessments
                .Where(assessment => assessment.LearnerId == enrollment.LearnerId
                    && quizMap.Keys.Contains(assessment.QuizId))
                .OrderBy(assessment => assessment.SubmittedAt)
                .ToListAsync(cancellationToken);
            foreach (var legacy in assessments)
            {
                if (await db.ProgressMigrationRecords.AnyAsync(record =>
                    record.SourceType == nameof(Assessment) && record.SourceId == legacy.Id,
                    cancellationToken)) continue;
                var releaseQuizId = quizMap[legacy.QuizId];
                if (legacy.IsPassed
                    && !await db.ReleaseItemProgress.AnyAsync(progress =>
                        progress.EnrollmentId == enrollment.Id && progress.ItemType == "Quiz" && progress.ItemId == releaseQuizId,
                        cancellationToken))
                {
                    db.ReleaseItemProgress.Add(new ReleaseItemProgress
                    {
                        EnrollmentId = enrollment.Id,
                        LearnerId = enrollment.LearnerId,
                        ReleaseId = releaseId,
                        ItemType = "Quiz",
                        ItemId = releaseQuizId,
                        State = "Completed",
                        ProgressPercent = 100,
                        EvidenceJson = System.Text.Json.JsonSerializer.Serialize(new { legacyAssessmentId = legacy.Id, legacy.ScorePercentage }),
                        UpdatedAt = legacy.SubmittedAt,
                        CompletedAt = legacy.SubmittedAt
                    });
                }
                db.ProgressMigrationRecords.Add(new ProgressMigrationRecord
                {
                    EnrollmentId = enrollment.Id,
                    ReleaseId = releaseId,
                    SourceType = nameof(Assessment),
                    SourceId = legacy.Id,
                    Outcome = "Mapped",
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        releaseQuizId,
                        legacy.ScorePercentage,
                        legacy.IsPassed,
                        legacy.AttemptNumber
                    }),
                    MigratedAt = now
                });
            }

            var assignmentMap = await db.ReleaseAssignments
                .Where(assignment => assignment.ReleaseId == releaseId)
                .ToDictionaryAsync(assignment => assignment.SourceAssignmentId, assignment => assignment.Id, cancellationToken);
            var submissions = await db.AssignmentSubmissions
                .Where(submission => submission.LearnerId == enrollment.LearnerId
                    && assignmentMap.Keys.Contains(submission.AssignmentId))
                .ToListAsync(cancellationToken);
            foreach (var submission in submissions)
            {
                var releaseAssignmentId = assignmentMap[submission.AssignmentId];
                submission.ReleaseAssignmentId = releaseAssignmentId;
                if (!await db.ReleaseItemProgress.AnyAsync(progress =>
                    progress.EnrollmentId == enrollment.Id
                    && progress.ItemType == "Assignment"
                    && progress.ItemId == releaseAssignmentId,
                    cancellationToken))
                {
                    db.ReleaseItemProgress.Add(new ReleaseItemProgress
                    {
                        EnrollmentId = enrollment.Id,
                        LearnerId = enrollment.LearnerId,
                        ReleaseId = releaseId,
                        ItemType = "Assignment",
                        ItemId = releaseAssignmentId,
                        State = "Completed",
                        ProgressPercent = 100,
                        EvidenceJson = System.Text.Json.JsonSerializer.Serialize(new { submissionId = submission.Id, submission.Status }),
                        UpdatedAt = submission.SubmittedAt,
                        CompletedAt = submission.SubmittedAt
                    });
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
