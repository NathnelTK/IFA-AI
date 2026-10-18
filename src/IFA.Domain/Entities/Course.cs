using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Software Engineering";
        public string TargetAudience { get; set; } = "Exit Exam / General";
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string ProviderName { get; set; } = "IFA AI";
        public string? ProviderLogo { get; set; }
        public string? Badge { get; set; }
        public double Rating { get; set; } = 4.8;
        public string ReviewCount { get; set; } = "1.2k";
        public string EstimatedDuration { get; set; } = "6 weeks";
        public bool IsPublic { get; set; } = false;
        public string ShareCode { get; set; } = string.Empty;
        /// <summary>
        /// Learner-supplied source links (semicolon separated) captured when the
        /// course was created. Scoped to the course so each course's module
        /// generation only sees its own materials.
        /// </summary>
        public string? ExternalMaterials { get; set; }
        public Guid? CreatorLearnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Monetization (educator-published courses)
        public bool IsPaid { get; set; } = false;
        public decimal PriceAmount { get; set; } = 0;
        public string Currency { get; set; } = "ETB";
        /// <summary>Paid courses keep enrollments pending until payment + educator approval.</summary>
        public bool RequiresApproval => IsPaid;
        public string? ChapaReference { get; set; }

        // Cover image (educator link or auto-discovered via Wikimedia/Tavily)
        public string? CoverImageUrl { get; set; }
        /// <summary>User | Auto (Wikimedia/Tavily search) | Generated</summary>
        public string? CoverImageSource { get; set; }

        // Navigation properties
        public Learner? CreatorLearner { get; set; }
        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public ICollection<CourseShareInvite> ShareInvites { get; set; } = new List<CourseShareInvite>();
        public ICollection<ResearchPackage> ResearchPackages { get; set; } = new List<ResearchPackage>();
    }
}
