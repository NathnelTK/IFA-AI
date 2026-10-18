using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    /// <summary>
    /// An educator workspace for turning linked learning materials into a
    /// structured course. Materials are links only — no file storage.
    /// </summary>
    public class EducatorProject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EducatorLearnerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Draft | Researching | Analyzing | AwaitingResolution | BlueprintReady |
        /// Generating | ReadyForReview | Published | Failed
        /// </summary>
        public string Status { get; set; } = "Draft";
        public Guid? CourseId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Learner? EducatorLearner { get; set; }
        public Course? Course { get; set; }
        public ICollection<ProjectSource> Sources { get; set; } = new List<ProjectSource>();
        public ICollection<SourceConflict> Conflicts { get; set; } = new List<SourceConflict>();
        public CourseBlueprint? Blueprint { get; set; }
        public ICollection<GenerationJob> GenerationJobs { get; set; } = new List<GenerationJob>();
    }
}
