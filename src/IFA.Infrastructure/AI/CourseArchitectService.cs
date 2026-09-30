using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class CourseArchitectService : ICourseArchitectService
    {
        private const int MinModules = 3;
        private const int MaxModules = 8;

        // AllowReadingFromString: small models sometimes write "5" instead of 5.
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly ILlmGateway _llm;
        private readonly ILogger<CourseArchitectService> _logger;

        public CourseArchitectService(ILlmGateway llm, ILogger<CourseArchitectService> logger)
        {
            _llm = llm;
            _logger = logger;
        }

        public async Task<CoursePipelineProposal?> GenerateBlueprintAsync(
            LearnerProfile profile, ResearchPackage research, CancellationToken ct = default)
        {
            var basePrompt = BuildPrompt(profile, research);

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                var prompt = attempt == 1
                    ? basePrompt
                    : basePrompt + "\n\nYour previous reply was not valid. Reply with ONLY the JSON object.";

                var result = await _llm.CompleteAsync(new LlmCompletionRequest
                {
                    SystemPrompt = PromptRegistry.CourseArchitectSystem,
                    UserPrompt = prompt,
                    Temperature = 0.3,
                    JsonSchemaHint = "see system prompt"
                }, ct);

                if (!result.Success)
                {
                    _logger.LogWarning("Architect attempt {Attempt}: LLM call failed: {Error}", attempt, result.ErrorMessage);
                    continue;
                }

                var proposal = TryParse(result.RawText, profile);
                if (proposal is not null) return proposal;

                _logger.LogWarning("Architect attempt {Attempt}: invalid blueprint ({Version}). Raw: {Raw}",
                    attempt, PromptRegistry.CourseArchitectVersion, result.RawText);
            }

            _logger.LogError("Course blueprint failed after retry for profile {ProfileId}.", profile.Id);
            return null;
        }

        private static CoursePipelineProposal? TryParse(string raw, LearnerProfile profile)
        {
            try
            {
                var dto = JsonSerializer.Deserialize<ArchitectDto>(LlmOutput.ExtractJsonObject(raw), JsonOptions);
                if (dto?.Modules is null) return null;

                var title = LlmOutput.Clean(dto.CourseTitle);
                var modules = dto.Modules
                    .Where(m => LlmOutput.Clean(m.Title) is not null)
                    .Take(MaxModules)
                    .Select((m, i) => new ModuleSummaryDto
                    {
                        ModuleNumber = i + 1, // never trust the model's numbering
                        Title = Truncate(LlmOutput.Clean(m.Title)!, 150),
                        Summary = Truncate(LlmOutput.Clean(m.Summary) ?? string.Empty, 500),
                        EstimatedHours = Math.Clamp(m.EstimatedHours ?? 4, 1, 20),
                        KeyTopics = LlmOutput.CleanList(m.KeyTopics)
                    }).ToList();

                if (title is null || modules.Count < MinModules) return null;

                return new CoursePipelineProposal
                {
                    CourseTitle = Truncate(title, 150),
                    Description = Truncate(LlmOutput.Clean(dto.Description) ?? string.Empty, 500),
                    TargetGoal = LlmOutput.Clean(dto.TargetGoal) ?? profile.Goal,
                    TotalEstimatedHours = modules.Sum(m => m.EstimatedHours),
                    Modules = modules
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string BuildPrompt(LearnerProfile p, ResearchPackage r)
        {
            var sb = new StringBuilder("LEARNER PROFILE\n");
            sb.AppendLine($"Goal: {p.Goal}");
            Line(sb, "Subject", p.SubjectTopic);
            Line(sb, "Current level", p.CurrentLevel);
            Line(sb, "Target outcome", p.TargetOutcome);
            if (p.AvailableStudyHoursPerWeek is not null)
                sb.AppendLine($"Study hours per week: {p.AvailableStudyHoursPerWeek}");
            Line(sb, "Learning style", p.PreferredLearningStyle);
            Line(sb, "Course language", p.PreferredLanguage);
            List(sb, "Constraints", p.Constraints);
            List(sb, "Already strong in", p.KnownStrengths);
            List(sb, "Struggles with", p.KnownWeaknesses);

            sb.AppendLine().AppendLine("RESEARCH (optional context)");
            if (r.AcademicSources.Count == 0)
                sb.AppendLine("No research available. Design the course from the profile alone.");
            else
                foreach (var s in r.AcademicSources.Take(5))
                    sb.AppendLine($"- {s.Title}: {Truncate(s.Summary, 200)}");

            return sb.ToString();
        }

        private static void Line(StringBuilder sb, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) sb.AppendLine($"{label}: {value}");
        }

        private static void List(StringBuilder sb, string label, List<string> values)
        {
            if (values.Count > 0) sb.AppendLine($"{label}: {string.Join("; ", values)}");
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

        private class ArchitectDto
        {
            public string? CourseTitle { get; set; }
            public string? Description { get; set; }
            public string? TargetGoal { get; set; }
            public List<ArchitectModuleDto>? Modules { get; set; }
        }

        private class ArchitectModuleDto
        {
            public string? Title { get; set; }
            public string? Summary { get; set; }
            public int? EstimatedHours { get; set; }
            public List<string>? KeyTopics { get; set; }
        }
    }
}