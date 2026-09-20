using System;
using System.Collections.Generic;

namespace IFA.Application.Courses.DTOs
{
    /// <summary>
    /// Contract produced by Model 2 (Course Architect).
    /// Represents the full personalized course structure, where Module 1
    /// is flagged as ready for generation while subsequent modules remain
    /// lightweight blueprints until just-in-time materialization.
    /// </summary>
    public class CourseBlueprintDto
    {
        public Guid? CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TargetGoal { get; set; } = string.Empty;
        public int EstimatedTotalHours { get; set; }
        public string PedagogicalApproach { get; set; } = "Concept -> Concrete Example -> Practical Activity";
        public List<ModuleBlueprintDto> Modules { get; set; } = new();
    }

    public class ModuleBlueprintDto
    {
        public int ModuleNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        /// <summary>
        /// Status: "READY_FOR_GENERATION", "BLUEPRINT", "IN_PROGRESS", "COMPLETED"
        /// </summary>
        public string Status { get; set; } = "BLUEPRINT";
        public int EstimatedHours { get; set; }
        public List<string> KeyTopics { get; set; } = new();
        public List<string> LearningObjectives { get; set; } = new();
        public List<string> PrerequisiteTopics { get; set; } = new();
    }
}
