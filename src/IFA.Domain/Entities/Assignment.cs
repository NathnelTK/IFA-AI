using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Assignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public Guid? ModuleId { get; set; }
        public Guid? LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string InstructionsMarkdown { get; set; } = string.Empty;
        public int DueDaysAfterEnrollment { get; set; } = 7;
        public string RubricCriteriaJson { get; set; } = "[]";
        public int MaxScore { get; set; } = 100;
        public string AllowedFileTypes { get; set; } = ".pdf,.zip,.docx,.txt";
        public int MaxFileSizeMb { get; set; } = 25;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Course? Course { get; set; }
        public Module? Module { get; set; }
        public Lesson? Lesson { get; set; }
        public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }

    public class AssignmentSubmission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssignmentId { get; set; }
        public Guid LearnerId { get; set; }
        public string SubmissionText { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public Guid? AssetId { get; set; }
        public Guid? ReleaseAssignmentId { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Submitted"; // Submitted | Graded | ResubmissionRequested
        public int? Score { get; set; }
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
        public Guid? GradedByEducatorId { get; set; }

        // Navigation properties
        public Assignment? Assignment { get; set; }
        public Learner? Learner { get; set; }
    }
}
