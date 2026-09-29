using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public class Quiz
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int PassingScorePercentage { get; set; } = 70;

        // LastScorePercentage / IsPassed / CompletedAt removed —
        // see QuizAttempt. A quiz can be attempted by many learners
        // (and retaken by the same learner), so score/pass state
        // can't be a single value on the Quiz itself.

        public Module? Module { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    }
}