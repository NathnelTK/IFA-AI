using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class CourseDiscussion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid? LessonId { get; set; }
        public Guid LearnerId { get; set; }
        public Guid? ParentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsInstructorReply { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Course? Course { get; set; }
        public Lesson? Lesson { get; set; }
        public Learner? Learner { get; set; }
        public CourseDiscussion? Parent { get; set; }
        public ICollection<CourseDiscussion> Replies { get; set; } = new List<CourseDiscussion>();
    }
}
