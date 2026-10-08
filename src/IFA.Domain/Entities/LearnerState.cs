using System;

namespace IFA.Domain.Entities
{
    public class LearnerState
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }
        public Guid? CurrentCourseId { get; set; }
        public Guid? CurrentModuleId { get; set; }
        public int ActiveStreakDays { get; set; } = 1;
        public double TotalHoursLearned { get; set; } = 0.0;
        public int CompletedLessonsCount { get; set; } = 0;
        public int CompletedQuizzesCount { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Learner? Learner { get; set; }
    }
}
