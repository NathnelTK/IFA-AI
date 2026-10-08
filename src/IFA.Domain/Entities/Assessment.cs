using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Assessment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }
        public Guid QuizId { get; set; }
        public int ScorePercentage { get; set; }
        public bool IsPassed { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Learner? Learner { get; set; }
        public Quiz? Quiz { get; set; }
        public ICollection<AssessmentResponse> Responses { get; set; } = new List<AssessmentResponse>();
    }
}
