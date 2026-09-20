using System;
using System.Collections.Generic;
using IFA.Application.Learning.DTOs;
using IFA.Application.Research.DTOs;

namespace IFA.Application.Courses.DTOs
{
    /// <summary>
    /// Materialized contract for a single module, passed to Model 3 (Fine-Tuned Course Builder).
    /// Contains rich contextual instructions, curriculum constraints, and media candidates.
    /// </summary>
    public class ModuleSpecificationDto
    {
        public Guid? CourseId { get; set; }
        public int ModuleNumber { get; set; } = 1;
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = "Software Engineering Student";
        public LearnerProfileDto? LearnerProfile { get; set; }
        public List<string> LearningObjectives { get; set; } = new();
        public List<string> ResearchFindings { get; set; } = new();
        public List<ExternalResourceDto> Resources { get; set; } = new();
        public List<VideoResourceDto> PreferredVideos { get; set; } = new();
        public List<string> AdaptationConstraints { get; set; } = new();

        public List<LessonSpecificationDto> Lessons { get; set; } = new();
        public QuizSpecificationDto? FormativeQuiz { get; set; }
    }

    public class LessonSpecificationDto
    {
        public int LessonNumber { get; set; } = 1;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ContentMarkdown { get; set; } = string.Empty;
        public int ReadingTimeMinutes { get; set; } = 10;
        public VideoResourceDto? VideoResource { get; set; }
        public AcademicSourceDto? AcademicCitation { get; set; }
        public List<string> PracticalExercises { get; set; } = new();
        public List<string> KeyTakeaways { get; set; } = new();
    }

    public class QuizSpecificationDto
    {
        public string Title { get; set; } = string.Empty;
        public int PassingScorePercentage { get; set; } = 70;
        public List<QuestionSpecificationDto> Questions { get; set; } = new();
    }

    public class QuestionSpecificationDto
    {
        public string Prompt { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectOptionIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string AssociatedSkill { get; set; } = string.Empty;
    }
}
