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
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public Guid CourseId { get; set; }
        public int ModuleIndex { get; set; }
        public string ModuleTitle { get; set; } = string.Empty;
        /// <summary>Queued | Running | Done | Failed</summary>
        public string Status { get; set; } = "Queued";
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public EducatorProject? Project { get; set; }
    }
}
