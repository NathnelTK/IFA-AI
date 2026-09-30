using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? SourceLearnerProfileId { get; set; }
        public Guid? SourceResearchPackageId { get; set; }
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
        public Guid? CreatorLearnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Learner? CreatorLearner { get; set; }
        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public ICollection<CourseShareInvite> ShareInvites { get; set; } = new List<CourseShareInvite>();
    }
}
