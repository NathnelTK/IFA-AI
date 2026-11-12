using System;

namespace IFA.Domain.Entities
{
    /// <summary>
    /// One background job that generates a single course module from the
    /// approved blueprint. Isolated per module so one failure never blocks or
    /// corrupts the rest of the course.
    /// </summary>
    public class GenerationJob
    {
        public const string QueuedStatus = "Queued";
        public const string RunningStatus = "Running";
        public const string DoneStatus = "Done";
        public const string FailedStatus = "Failed";
        public const string BlueprintModuleKind = "BlueprintModule";
        public const string JitModuleKind = "JitModule";

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public Guid? AuthoringJobId { get; set; }
        public Guid CourseId { get; set; }
        public int ModuleIndex { get; set; }
        public string ModuleTitle { get; set; } = string.Empty;
        public string Kind { get; set; } = BlueprintModuleKind;
        public string? RequestJson { get; set; }
        /// <summary>Queued | Running | Done | Failed</summary>
        public string Status { get; set; } = QueuedStatus;
        public string? Error { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 3;
        public string? LeaseOwner { get; set; }
        public Guid? LeaseToken { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
        public DateTime? LastHeartbeatAt { get; set; }
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
        public Guid? ActorLearnerId { get; set; }

        // Navigation properties
        public EducatorProject? Project { get; set; }
    }

    public sealed class QueuedModuleGenerationPayload
    {
        public Guid ModuleId { get; set; }
        public Guid LearnerId { get; set; }
    }
}
