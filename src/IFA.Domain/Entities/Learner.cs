using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Learner
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int OverallProgress { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<SkillMetric> Skills { get; set; } = new List<SkillMetric>();
        public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public ICollection<Course> AuthoredCourses { get; set; } = new List<Course>();
    }
}
