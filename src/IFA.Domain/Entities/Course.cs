using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Overview { get; set; } = string.Empty;
        public string Category { get; set; } = "Software Engineering";
        public string TargetAudience { get; set; } = "Exit Exam / General";
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string ProviderName { get; set; } = "Witforge AI";
        public string? ProviderLogo { get; set; }
        public string? Badge { get; set; }
        public double Rating { get; set; }
        public string ReviewCount { get; set; } = "0";
        public string EstimatedDuration { get; set; } = "6 weeks";
        public bool IsPublic { get; set; } = false;
        public bool IsPublished { get; set; }
        public string Visibility { get; set; } = "Public"; // Public | Unlisted | Private
        public string Level { get; set; } = "Beginner";
        public string Language { get; set; } = "English";
        public List<string> LearningObjectives { get; set; } = new();
        public bool RequirePassingQuizzes { get; set; } = true;
        public bool CourseAssistantEnabled { get; set; } = true;
        public bool AwardsCertificate { get; set; } = true;
        public string CompletionRule { get; set; } = "Complete every required lesson and pass required assessments.";
        public int? EnrollmentLimit { get; set; }
        public DateTime? EnrollmentOpensAt { get; set; }
        public DateTime? EnrollmentClosesAt { get; set; }
        public string ShareCode { get; set; } = string.Empty;
        /// <summary>
        /// Learner-supplied source links (semicolon separated) captured when the
        /// course was created. Scoped to the course so each course's module
        /// generation only sees its own materials.
        /// </summary>
        public string? ExternalMaterials { get; set; }
        public Guid? CreatorLearnerId { get; set; }
        public Guid? WorkspaceId { get; set; }
        public string? Slug { get; set; }
        public Guid? CreatedByAccountId { get; set; }
        public Guid? UpdatedByAccountId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Monetization (educator-published courses)
        public bool IsPaid { get; set; } = false;
        public decimal PriceAmount { get; set; } = 0;
        public string Currency { get; set; } = "ETB";
        /// <summary>Approval mode is independent from payment. Paid approval is blocked until refund support exists.</summary>
        public bool RequiresApproval => EnrollmentMode == "ApprovalRequired";
        public string? ChapaReference { get; set; }

        // Course branding & configuration
        public string AccentColor { get; set; } = "#1B3D2F";
        public string ThemeMode { get; set; } = "pine";
        public string? AppearanceJson { get; set; }
        public int AppearanceVersion { get; set; }
        public DateTime? AppearanceUpdatedAt { get; set; }
        public string EnrollmentMode { get; set; } = "Open"; // Open | ApprovalRequired | InviteOnly
        public int PassingScorePercentage { get; set; } = 70;
        public int MaxQuizAttempts { get; set; } = 3;
        public string? Prerequisites { get; set; }
        public string? InstructorBio { get; set; }
        public string? InstructorTitle { get; set; }

        // Cover image (educator link or auto-discovered via Wikimedia/Tavily)
        public string? CoverImageUrl { get; set; }
        public Guid? CoverAssetId { get; set; }
        public Guid? ActiveReleaseId { get; set; }
        /// <summary>User | Auto (Wikimedia/Tavily search) | Generated</summary>
        public string? CoverImageSource { get; set; }

        // Navigation properties
        public Learner? CreatorLearner { get; set; }
        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public ICollection<CourseShareInvite> ShareInvites { get; set; } = new List<CourseShareInvite>();
        public ICollection<ResearchPackage> ResearchPackages { get; set; } = new List<ResearchPackage>();
        public ICollection<LiveSession> LiveSessions { get; set; } = new List<LiveSession>();
        public ICollection<CourseDiscussion> Discussions { get; set; } = new List<CourseDiscussion>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
