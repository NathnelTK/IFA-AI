using System;

namespace IFA.Domain.Entities
{
    // ONE answer within ONE attempt. This is what PR 5.2 (Skill Gap
    // Detection) will query: join QuizAnswer -> Question to find out
    // exactly which TargetSkillName this learner is weak in.
    public class QuizAnswer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid QuizAttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public int SelectedOptionIndex { get; set; }
        public bool IsCorrect { get; set; }

        public QuizAttempt? QuizAttempt { get; set; }
        public Question? Question { get; set; }
    }
}