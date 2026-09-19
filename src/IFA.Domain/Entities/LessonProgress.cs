

using IFA.Domain.Entities;

public class LessonProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LessonId { get; set; }
    public Guid LearnerId { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }

    public Lesson? Lesson { get; set; }
    public Learner? Learner { get; set; }
}