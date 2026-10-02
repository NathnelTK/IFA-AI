using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Learner
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public string Role { get; set; } = "Learner";
        public string AvatarUrl { get; set; } = string.Empty;
        public int OverallProgress { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public LearnerProfile? Profile { get; set; }
        public LearnerState? State { get; set; }
        public LearnerSettings? Settings { get; set; }
        public ICollection<SkillMetric> Skills { get; set; } = new List<SkillMetric>();
        public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public ICollection<Course> AuthoredCourses { get; set; } = new List<Course>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
        public ICollection<LearningActivity> Activities { get; set; } = new List<LearningActivity>();
    }
}
