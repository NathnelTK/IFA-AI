namespace IFA.Domain.Entities
{
    public class WorkspaceFollow
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid LearnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CourseAnnouncement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid? ReleaseId { get; set; }
        public Guid? CohortId { get; set; }
        public Guid AuthorAccountId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string BodyMarkdown { get; set; } = string.Empty;
        public string Audience { get; set; } = "Enrolled";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
    }

    public class CourseReview
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid ReleaseId { get; set; }
        public Guid LearnerId { get; set; }
        public int Rating { get; set; }
        public string Body { get; set; } = string.Empty;
        public string Status { get; set; } = "Published";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModeratedAt { get; set; }
        public Guid? ModeratedByAccountId { get; set; }
    }

    public class CourseReviewReply
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReviewId { get; set; }
        public Guid AuthorAccountId { get; set; }
        public string Body { get; set; } = string.Empty;
        public string Status { get; set; } = "Published";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CourseReviewReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReviewId { get; set; }
        public Guid ReporterAccountId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public Guid? ResolvedByAccountId { get; set; }
    }

    public class ModerationCase
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResourceType { get; set; } = string.Empty;
        public Guid ResourceId { get; set; }
        public string Status { get; set; } = "Open";
        public string Priority { get; set; } = "Normal";
        public Guid? AssignedModeratorId { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
