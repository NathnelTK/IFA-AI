using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Application.Common.Services;
using IFA.Domain.Entities;
using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Services
{
    public sealed class CourseReleaseService : ICourseReleaseService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;

        public CourseReleaseService(ApplicationDbContext db, TimeProvider timeProvider)
        {
            _db = db;
            _timeProvider = timeProvider;
        }

        public async Task<CourseRelease> PublishAsync(
            Guid courseId,
            Guid actorAccountId,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);
            var course = await _db.Courses
                .Include(candidate => candidate.Modules.OrderBy(module => module.ModuleNumber))
                .ThenInclude(module => module.Lessons.OrderBy(lesson => lesson.LessonNumber))
                .Include(candidate => candidate.Modules)
                .ThenInclude(module => module.Quizzes.OrderBy(quiz => quiz.OrderIndex))
                .ThenInclude(quiz => quiz.Questions)
                .Include(candidate => candidate.Assignments)
                .SingleOrDefaultAsync(candidate => candidate.Id == courseId, cancellationToken)
                ?? throw new InvalidOperationException("The course does not exist.");
            var errors = CoursePublication.Validate(course);
            if (errors.Count > 0)
            {
                throw new CourseReleaseValidationException(errors);
            }

            var version = (await _db.CourseReleases
                .Where(release => release.CourseId == course.Id)
                .MaxAsync(release => (int?)release.Version, cancellationToken) ?? 0) + 1;
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var snapshot = new
            {
                course.Id,
                course.Title,
                course.Slug,
                course.Description,
                course.Overview,
                course.Category,
                course.TargetAudience,
                course.EstimatedDuration,
                course.Level,
                course.Language,
                course.LearningObjectives,
                course.CompletionRule,
                course.RequirePassingQuizzes,
                course.CourseAssistantEnabled,
                course.AwardsCertificate,
                course.PassingScorePercentage,
                course.MaxQuizAttempts,
                course.Prerequisites,
                course.AccentColor,
                course.ThemeMode,
                Appearance = string.IsNullOrWhiteSpace(course.AppearanceJson)
                    ? (JsonElement?)null
                    : JsonSerializer.Deserialize<JsonElement>(course.AppearanceJson),
                course.AppearanceVersion,
                course.CoverAssetId,
                course.CoverImageUrl,
                course.ProviderName,
                course.InstructorTitle,
                course.InstructorBio,
                modules = course.Modules.OrderBy(module => module.ModuleNumber).Select(module => new
                {
                    module.Id,
                    module.ModuleNumber,
                    module.Title,
                    module.Summary,
                    module.EstimatedHours,
                    lessons = module.Lessons.OrderBy(lesson => lesson.LessonNumber).Select(lesson => new
                    {
                        lesson.Id,
                        lesson.LessonNumber,
                        lesson.Title,
                        lesson.Summary,
                        lesson.ContentMarkdown,
                        lesson.ReadingTimeMinutes,
                        lesson.YouTubeVideoId
                    }),
                    quizzes = module.Quizzes.OrderBy(quiz => quiz.OrderIndex).Select(quiz => new
                    {
                        quiz.Id,
                        quiz.Title,
                        quiz.Kind,
                        quiz.OrderIndex,
                        quiz.PassingScorePercentage,
                        questions = quiz.Questions.OrderBy(question => question.Id).Select(question => new
                        {
                            question.Id,
                            question.Prompt,
                            question.Options,
                            question.CorrectOptionIndex,
                            question.Explanation,
                            question.TargetSkillName,
                            question.BloomTaxonomyLevel
                        })
                    })
                }),
                assignments = course.Assignments.OrderBy(assignment => assignment.CreatedAt).ThenBy(assignment => assignment.Id).Select(assignment => new
                {
                    assignment.Id,
                    assignment.ModuleId,
                    assignment.LessonId,
                    assignment.Title,
                    assignment.InstructionsMarkdown,
                    assignment.DueDaysAfterEnrollment,
                    assignment.RubricCriteriaJson,
                    assignment.MaxScore,
                    assignment.AllowedFileTypes,
                    assignment.MaxFileSizeMb
                })
            };
            var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
            var contentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson))).ToLowerInvariant();
            var release = new CourseRelease
            {
                CourseId = course.Id,
                Version = version,
                SupersedesReleaseId = course.ActiveReleaseId,
                PublishedByAccountId = actorAccountId,
                Title = course.Title,
                Slug = course.Slug ?? course.Id.ToString("N"),
                SnapshotJson = snapshotJson,
                ContentHash = contentHash,
                Status = "Published",
                CreatedAt = now,
                PublishedAt = now
            };
            _db.CourseReleases.Add(release);

            var moduleMap = new Dictionary<Guid, ReleaseModule>();
            foreach (var module in course.Modules.OrderBy(module => module.ModuleNumber))
            {
                var releasedModule = new ReleaseModule
                {
                    ReleaseId = release.Id,
                    SourceModuleId = module.Id,
                    ModuleNumber = module.ModuleNumber,
                    Title = module.Title,
                    Summary = module.Summary,
                    EstimatedHours = module.EstimatedHours
                };
                moduleMap[module.Id] = releasedModule;
                _db.ReleaseModules.Add(releasedModule);
                foreach (var lesson in module.Lessons.OrderBy(lesson => lesson.LessonNumber))
                {
                    var assetIds = await _db.AssetReferences
                        .Where(reference => reference.EntityType == nameof(Lesson) && reference.EntityId == lesson.Id)
                        .Select(reference => reference.AssetId)
                        .ToListAsync(cancellationToken);
                    _db.ReleaseLessons.Add(new ReleaseLesson
                    {
                        ReleaseModuleId = releasedModule.Id,
                        SourceLessonId = lesson.Id,
                        LessonNumber = lesson.LessonNumber,
                        Title = lesson.Title,
                        Summary = lesson.Summary,
                        ContentMarkdown = lesson.ContentMarkdown,
                        ReadingTimeMinutes = lesson.ReadingTimeMinutes,
                        YouTubeVideoId = lesson.YouTubeVideoId,
                        AssetReferencesJson = JsonSerializer.Serialize(assetIds, JsonOptions)
                    });
                }
                foreach (var quiz in module.Quizzes.OrderBy(quiz => quiz.OrderIndex))
                {
                    _db.ReleaseQuizzes.Add(new ReleaseQuiz
                    {
                        ReleaseModuleId = releasedModule.Id,
                        SourceQuizId = quiz.Id,
                        Title = quiz.Title,
                        Kind = quiz.Kind.ToString(),
                        OrderIndex = quiz.OrderIndex,
                        PassingScorePercentage = quiz.PassingScorePercentage,
                        DefinitionJson = JsonSerializer.Serialize(quiz.Questions.OrderBy(question => question.Id).Select(question => new
                        {
                            question.Id,
                            question.Prompt,
                            question.Options,
                            question.CorrectOptionIndex,
                            question.Explanation,
                            question.TargetSkillName,
                            question.BloomTaxonomyLevel
                        }), JsonOptions)
                    });
                }
            }
            foreach (var assignment in course.Assignments.OrderBy(assignment => assignment.CreatedAt))
            {
                _db.ReleaseAssignments.Add(new ReleaseAssignment
                {
                    ReleaseId = release.Id,
                    SourceAssignmentId = assignment.Id,
                    ReleaseModuleId = assignment.ModuleId.HasValue && moduleMap.TryGetValue(assignment.ModuleId.Value, out var module)
                        ? module.Id
                        : null,
                    Title = assignment.Title,
                    InstructionsMarkdown = assignment.InstructionsMarkdown,
                    DueDaysAfterEnrollment = assignment.DueDaysAfterEnrollment,
                    MaxScore = assignment.MaxScore,
                    RubricCriteriaJson = assignment.RubricCriteriaJson,
                    AllowedFileTypes = assignment.AllowedFileTypes,
                    MaxFileSizeMb = assignment.MaxFileSizeMb
                });
            }

            if (course.ActiveReleaseId.HasValue)
            {
                var previous = await _db.CourseReleases.SingleAsync(
                    candidate => candidate.Id == course.ActiveReleaseId.Value,
                    cancellationToken);
                previous.Status = "Superseded";
            }
            course.ActiveReleaseId = release.Id;
            course.IsPublished = true;
            course.IsPublic = course.Visibility == "Public";
            course.UpdatedAt = now;
            course.UpdatedByAccountId = actorAccountId;
            var authoring = await _db.AuthoringProjects.SingleOrDefaultAsync(
                candidate => candidate.CourseId == course.Id,
                cancellationToken);
            if (authoring != null)
            {
                authoring.State = AuthoringProjectState.Published;
                authoring.UpdatedAt = now;
                authoring.Version++;
            }
            _db.CourseCohorts.Add(new CourseCohort
            {
                CourseId = course.Id,
                ReleaseId = release.Id,
                Name = $"Open enrollment · release {version}",
                Status = "Open",
                EnrollmentOpensAt = course.EnrollmentOpensAt,
                EnrollmentClosesAt = course.EnrollmentClosesAt,
                EnrollmentLimit = course.EnrollmentLimit,
                CreatedAt = now
            });
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "course.release.published",
                AggregateType = nameof(CourseRelease),
                AggregateId = release.Id,
                DeduplicationKey = $"course-release-published:{release.Id}",
                PayloadJson = JsonSerializer.Serialize(new { courseId = course.Id, releaseId = release.Id, version }, JsonOptions),
                OccurredAt = now,
                AvailableAt = now,
                ActorLearnerId = actorAccountId
            });
            _db.ProductActivityEvents.Add(new ProductActivityEvent
            {
                EventType = "course.release.published",
                ActorAccountId = actorAccountId,
                WorkspaceId = course.WorkspaceId,
                CourseId = course.Id,
                ReleaseId = release.Id,
                MetadataJson = JsonSerializer.Serialize(new { release.Version, release.ContentHash }),
                OccurredAt = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return release;
        }
    }

    public sealed class CourseReleaseValidationException : InvalidOperationException
    {
        public CourseReleaseValidationException(IReadOnlyList<string> errors)
            : base("The course is not ready to publish.")
        {
            Errors = errors;
        }

        public IReadOnlyList<string> Errors { get; }
    }
}
