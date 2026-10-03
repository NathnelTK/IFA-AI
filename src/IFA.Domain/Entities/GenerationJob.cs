
namespace IFA.Domain.Entities
{
    public enum GenerationJobStatus { Queued, Running, Completed, Failed }

    public enum GenerationStep
    {
        NotStarted, CheckingProfile, Researching, DesigningCourse,
        SelectingModule, BuildingContent, Done
    }

    public class GenerationJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearnerId { get; set; }

        // Captured at job creation, NOT re-looked-up at execution time.
        // If the learner starts a new intake conversation while this job
        // is queued, we must still finish building a course from THIS
        // profile, not whatever happens to be "latest" by the time the
        // worker picks the job up.
        public Guid LearnerProfileId { get; set; }

        public GenerationJobStatus Status { get; set; } = GenerationJobStatus.Queued;
        public GenerationStep CurrentStep { get; set; } = GenerationStep.NotStarted;
        public Guid? ResultingCourseId { get; set; }
        public string? ErrorMessage { get; set; }
        public int AttemptCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Learner? Learner { get; set; }
    }
}