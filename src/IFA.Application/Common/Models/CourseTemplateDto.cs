using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IFA.Application.Common.Models
{
    public class CourseTemplateLessonDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;
        [StringLength(2000)]
        public string Summary { get; set; } = string.Empty;
        [StringLength(20000)]
        public string ContentMarkdown { get; set; } = string.Empty;
        [Range(1, 1440)]
        public int ReadingTimeMinutes { get; set; } = 15;
    }

    public class CourseTemplateModuleDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;
        [StringLength(2000)]
        public string Summary { get; set; } = string.Empty;
        [Range(1, 1000)]
        public int EstimatedHours { get; set; } = 4;
        [MaxLength(100)]
        public List<CourseTemplateLessonDto> Lessons { get; set; } = new List<CourseTemplateLessonDto>();
        public string QuizTitle { get; set; } = string.Empty;
        public List<string> QuizQuestions { get; set; } = new List<string>();
    }

    public class CourseTemplateDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public string EstimatedDuration { get; set; } = string.Empty;
        public string Level { get; set; } = "Beginner to Intermediate";
        public List<string> LearningObjectives { get; set; } = new();
        public string CompletionRule { get; set; } = "Complete every lesson and pass all configured quizzes. Assignments receive separate instructor feedback.";
        public bool RequirePassingQuizzes { get; set; } = true;
        public List<string> AssessmentSuggestions { get; set; } = new();
        public string AccentColor { get; set; } = "#1B3D2F";
        public string ThemeMode { get; set; } = "pine";
        public string Icon { get; set; } = "Code";
        public List<string> KeyFeatures { get; set; } = new List<string>();
        public List<CourseTemplateModuleDto> Modules { get; set; } = new List<CourseTemplateModuleDto>();
        public string SampleAssignmentTitle { get; set; } = string.Empty;
        public string SampleAssignmentInstructions { get; set; } = string.Empty;
    }
}
