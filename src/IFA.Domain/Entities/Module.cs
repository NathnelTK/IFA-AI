using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Module
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public int ModuleNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int EstimatedHours { get; set; } = 4;
        public bool IsGenerated { get; set; } = false; // JIT status
        public bool IsCompleted { get; set; } = false;
        public DateTime? GeneratedAt { get; set; }

        // Navigation properties
        public Course? Course { get; set; }
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public Quiz? ModuleQuiz { get; set; }
    }
}
