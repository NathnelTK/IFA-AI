// IFA.Infrastructure/AI/ContentBuilderService.cs
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class ContentBuilderService : IContentBuilderService
    {
        private const int MinLessons = 1;
        private const int MaxLessons = 6;
        private const int MinQuestions = 2;
        private const int MaxQuestions = 10;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private static readonly HashSet<string> ValidBloomLevels = new(StringComparer.OrdinalIgnoreCase)
        { "Remember", "Understand", "Apply", "Analyze", "Evaluate" };

        private readonly ILlmGateway _llm;
        private readonly ILogger<ContentBuilderService> _logger;

        public ContentBuilderService(ILlmGateway llm, ILogger<ContentBuilderService> logger)
        {
            _llm = llm;
            _logger = logger;
        }

        public async Task<GeneratedModuleContent?> GenerateModuleContentAsync(
            Module module, LearnerProfile profile, ResearchPackage research, CancellationToken ct = default)
        {
            var basePrompt = BuildPrompt(module, profile, research);

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                var prompt = attempt == 1
                    ? basePrompt
                    : basePrompt + "\n\nYour previous reply was not valid. Reply with ONLY the JSON object, following the shape exactly.";

                var result = await _llm.CompleteAsync(new LlmCompletionRequest
                {
                    SystemPrompt = PromptRegistry.ContentBuilderSystem,
                    UserPrompt = prompt,
                    Temperature = 0.4,
                    JsonSchemaHint = "see system prompt"
                }, ct);

                if (!result.Success)
                {
                    _logger.LogWarning("Content builder attempt {Attempt}: LLM call failed: {Error}", attempt, result.ErrorMessage);
                    continue;
                }

                var content = TryParse(result.RawText);
                if (content is not null) return content;

                _logger.LogWarning("Content builder attempt {Attempt}: invalid content ({Version}). Raw: {Raw}",
                    attempt, PromptRegistry.ContentBuilderVersion, result.RawText);
            }

            _logger.LogError("Module content generation failed after retry for module {ModuleId}.", module.Id);
            return null;
        }

        private static GeneratedModuleContent? TryParse(string raw)
        {
            try
            {
                var dto = JsonSerializer.Deserialize<ContentDto>(LlmOutput.ExtractJsonObject(raw), JsonOptions);
                if (dto?.Lessons is null || dto.Questions is null) return null;

                var lessons = dto.Lessons
                    .Where(l => LlmOutput.Clean(l.Title) is not null && LlmOutput.Clean(l.ContentMarkdown) is not null)
                    .Take(MaxLessons)
                    .Select(l => new GeneratedLesson
                    {
                        Title = Truncate(LlmOutput.Clean(l.Title)!, 200),
                        Summary = Truncate(LlmOutput.Clean(l.Summary) ?? string.Empty, 500),
                        ContentMarkdown = LlmOutput.Clean(l.ContentMarkdown)!,
                        ReadingTimeMinutes = Math.Clamp(l.ReadingTimeMinutes ?? 10, 5, 60)
                    }).ToList();

                if (lessons.Count < MinLessons) return null;

                var questions = dto.Questions
                    .Where(IsValidQuestion)
                    .Take(MaxQuestions)
                    .Select(q => new GeneratedQuestion
                    {
                        Prompt = Truncate(LlmOutput.Clean(q.Prompt)!, 500),
                        Options = q.Options!.Select(o => Truncate(o.Trim(), 200)).ToList(),
                        CorrectOptionIndex = q.CorrectOptionIndex!.Value,
                        Explanation = Truncate(LlmOutput.Clean(q.Explanation) ?? string.Empty, 500),
                        TargetSkillName = Truncate(LlmOutput.Clean(q.TargetSkillName) ?? "General", 100),
                        BloomTaxonomyLevel = ValidBloomLevels.Contains(q.BloomTaxonomyLevel ?? "")
                            ? q.BloomTaxonomyLevel!
                            : "Understand" // safe default if the model invents a level
                    }).ToList();

                if (questions.Count < MinQuestions) return null;

                return new GeneratedModuleContent
                {
                    Lessons = lessons,
                    QuizTitle = Truncate(LlmOutput.Clean(dto.QuizTitle) ?? "Module Quiz", 150),
                    Questions = questions
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool IsValidQuestion(QuestionDto q) =>
            LlmOutput.Clean(q.Prompt) is not null &&
            q.Options is { Count: >= 2 and <= 6 } &&
            q.Options.Select(o => o.Trim().ToLowerInvariant()).Distinct().Count() == q.Options.Count &&
            q.CorrectOptionIndex is not null &&
            q.CorrectOptionIndex >= 0 &&
            q.CorrectOptionIndex < q.Options.Count; // must be a real index into THIS question's options

        private static string BuildPrompt(Module module, LearnerProfile profile, ResearchPackage research)
        {
            var sb = new StringBuilder("MODULE TO BUILD\n");
            sb.AppendLine($"Title: {module.Title}");
            sb.AppendLine($"Summary: {module.Summary}");
            if (module.KeyTopics.Count > 0)
                sb.AppendLine($"Key topics: {string.Join("; ", module.KeyTopics)}");

            sb.AppendLine().AppendLine("LEARNER CONTEXT");
            sb.AppendLine($"Goal: {profile.Goal}");
            if (!string.IsNullOrWhiteSpace(profile.CurrentLevel)) sb.AppendLine($"Current level: {profile.CurrentLevel}");
            if (!string.IsNullOrWhiteSpace(profile.PreferredLearningStyle)) sb.AppendLine($"Learning style: {profile.PreferredLearningStyle}");
            if (profile.KnownStrengths.Count > 0) sb.AppendLine($"Already knows: {string.Join("; ", profile.KnownStrengths)}");
            if (profile.KnownWeaknesses.Count > 0) sb.AppendLine($"Struggles with: {string.Join("; ", profile.KnownWeaknesses)}");

            sb.AppendLine().AppendLine("RESEARCH (optional context)");
            if (research.AcademicSources.Count == 0)
                sb.AppendLine("No research available. Write from general knowledge of this module's topics.");
            else
                foreach (var s in research.AcademicSources.Take(3))
                    sb.AppendLine($"- {s.Title}: {Truncate(s.Summary, 200)}");

            return sb.ToString();
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

        private class ContentDto
        {
            public List<LessonDto>? Lessons { get; set; }
            public string? QuizTitle { get; set; }
            public List<QuestionDto>? Questions { get; set; }
        }

        private class LessonDto
        {
            public string? Title { get; set; }
            public string? Summary { get; set; }
            public string? ContentMarkdown { get; set; }
            public int? ReadingTimeMinutes { get; set; }
        }

        private class QuestionDto
        {
            public string? Prompt { get; set; }
            public List<string>? Options { get; set; }
            public int? CorrectOptionIndex { get; set; }
            public string? Explanation { get; set; }
            public string? TargetSkillName { get; set; }
            public string? BloomTaxonomyLevel { get; set; }
        }
    }
}