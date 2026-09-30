using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public class GeneratedLesson
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ContentMarkdown { get; set; } = string.Empty;
        public int ReadingTimeMinutes { get; set; } = 10;
    }

    public class GeneratedQuestion
    {
        public string Prompt { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectOptionIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string TargetSkillName { get; set; } = string.Empty;
        public string BloomTaxonomyLevel { get; set; } = "Understand";
    }

    public class GeneratedModuleContent
    {
        public List<GeneratedLesson> Lessons { get; set; } = new();
        public string QuizTitle { get; set; } = string.Empty;
        public List<GeneratedQuestion> Questions { get; set; } = new();
    }

    public interface IContentBuilderService
    {
        // Returns null if no valid content could be produced after retry.
        // Module carries Title/Summary/KeyTopics from Course Architect.
        // Research may be empty - must never be required.
        Task<GeneratedModuleContent?> GenerateModuleContentAsync(
            Module module, LearnerProfile profile, ResearchPackage research, CancellationToken ct = default);
    }
}