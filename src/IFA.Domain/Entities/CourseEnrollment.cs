using System;

namespace IFA.Domain.Entities
{
    public class CourseEnrollment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid LearnerId { get; set; }
        public int ProgressPercentage { get; set; } = 0;
        public int CompletedModulesCount { get; set; } = 0;
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Course? Course { get; set; }
        public Learner? Learner { get; set; }
    }
}
