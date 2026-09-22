using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    public enum ModuleGenerationStatus
    {
        Blueprint,   // designed by Course Architect, content not yet built
        Generating,  // Content Builder is currently working on it
        Ready,       // fully generated, safe to show the learner
        Failed       // generation attempt failed — retry, don't serve blank content
    }

    public class Module
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourseId { get; set; }
        public int ModuleNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int EstimatedHours { get; set; } = 4;

        // Replaces bool IsGenerated. A plain bool can't represent
        // "currently generating" — which matters if a learner double
        // clicks "Continue" and fires two requests for the same module.
        public ModuleGenerationStatus GenerationStatus { get; set; } = ModuleGenerationStatus.Blueprint;
        public bool IsGenerated => GenerationStatus == ModuleGenerationStatus.Ready;
        public DateTime? GeneratedAt { get; set; }

        // IsCompleted removed for the same per-learner reason as Lesson/Quiz.
        // "Is this module complete" is now: all its Lessons have a
        // LessonProgress row for this learner, AND its Quiz has a
        // passing QuizAttempt for this learner. Computed, not stored,
        // to avoid two sources of truth going out of sync.

        public Course? Course { get; set; }
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public Quiz? ModuleQuiz { get; set; }
    }
}