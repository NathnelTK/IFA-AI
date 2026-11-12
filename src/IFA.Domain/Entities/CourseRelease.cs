namespace IFA.Domain.Entities
{
    public class CourseRelease
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public int Version { get; set; }
        public Guid? SupersedesReleaseId { get; set; }
        public Guid PublishedByAccountId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string SnapshotJson { get; set; } = "{}";
        public string ContentHash { get; set; } = string.Empty;
        public string Status { get; set; } = "Published";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RetiredAt { get; set; }
    }

    public class ReleaseModule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReleaseId { get; set; }
        public Guid SourceModuleId { get; set; }
        public int ModuleNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int EstimatedHours { get; set; }
    }

    public class ReleaseLesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReleaseModuleId { get; set; }
        public Guid SourceLessonId { get; set; }
        public int LessonNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ContentMarkdown { get; set; } = string.Empty;
        public int ReadingTimeMinutes { get; set; }
        public string? YouTubeVideoId { get; set; }
        public string AssetReferencesJson { get; set; } = "[]";
    }

    public class ReleaseQuiz
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReleaseModuleId { get; set; }
        public Guid SourceQuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Kind { get; set; } = "Exam";
        public int OrderIndex { get; set; }
        public int PassingScorePercentage { get; set; }
        public string DefinitionJson { get; set; } = "{}";
    }

    public class ReleaseAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReleaseId { get; set; }
        public Guid SourceAssignmentId { get; set; }
        public Guid? ReleaseModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string InstructionsMarkdown { get; set; } = string.Empty;
        public int DueDaysAfterEnrollment { get; set; }
        public int MaxScore { get; set; }
        public string RubricCriteriaJson { get; set; } = "[]";
        public string AllowedFileTypes { get; set; } = string.Empty;
        public int MaxFileSizeMb { get; set; }
    }

    public class CourseCohort
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid ReleaseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public DateTime? EnrollmentOpensAt { get; set; }
        public DateTime? EnrollmentClosesAt { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int? EnrollmentLimit { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ReleaseItemProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EnrollmentId { get; set; }
        public Guid LearnerId { get; set; }
        public Guid ReleaseId { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public Guid ItemId { get; set; }
        public string State { get; set; } = "Started";
        public decimal ProgressPercent { get; set; }
        public string EvidenceJson { get; set; } = "{}";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }

    public class ReleaseQuizAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EnrollmentId { get; set; }
        public Guid ReleaseQuizId { get; set; }
        public string AnswersJson { get; set; } = "[]";
        public int ScorePercentage { get; set; }
        public bool IsPassed { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProgressMigrationRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EnrollmentId { get; set; }
        public Guid ReleaseId { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public Guid SourceId { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public string DetailJson { get; set; } = "{}";
        public DateTime MigratedAt { get; set; } = DateTime.UtcNow;
    }
}
