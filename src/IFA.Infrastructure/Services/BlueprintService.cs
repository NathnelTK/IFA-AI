using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.AI;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Builds the AI course blueprint for an educator project with one LLM call
    /// over ALL included sources. The persisted JSON is information-full: every
    /// module names its exact source URLs and every chapter lists its own
    /// sourceUrls, so the background generator never has to guess.
    /// </summary>
    public class BlueprintService : IBlueprintService
    {
        private readonly ILlmGateway _llmGateway;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<BlueprintService> _logger;

        public BlueprintService(
            ILlmGateway llmGateway,
            IApplicationDbContext context,
            ILogger<BlueprintService> logger)
        {
            _llmGateway = llmGateway;
            _context = context;
            _logger = logger;
        }

        public async Task<CourseBlueprint> GenerateAsync(EducatorProject project, CancellationToken ct = default)
        {
            var sources = project.Sources
                .Where(s => s.Included)
                .OrderBy(s => s.CreatedAt)
                .ToList();

            if (sources.Count == 0)
            {
                throw new InvalidOperationException("No included sources to build a blueprint from.");
            }

            var digest = SourceAnalysisService.BuildDigest(sources);
            var userPrompt =
                $"Project title: {project.Title}\n" +
                $"Project description: {project.Description}\n\n" +
                $"Sources (use these EXACT URLs when mapping sources):\n\n{digest}";

            CourseBlueprintDocument? document = null;
            for (var attempt = 0; attempt < 2 && document == null; attempt++)
            {
                try
                {
                    var prompt = attempt == 0
                        ? PromptRegistry.Educator_BlueprintSystemPrompt
                        : PromptRegistry.Educator_BlueprintSystemPrompt +
                          "\n\nSTRICT REMINDER: reply with ONLY the raw JSON object, no prose and no code fences. " +
                          "Every module MUST include its sources array and every chapter MUST include sourceUrls.";
                    var raw = await _llmGateway.CompleteAsync(prompt, userPrompt, LlmRole.Model2_Architect, ct);
                    document = Parse(raw);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Blueprint LLM attempt {Attempt} failed for project {ProjectId}.", attempt + 1, project.Id);
                }
            }

            if (document == null || document.Modules.Count == 0)
            {
                throw new InvalidOperationException("The AI could not produce a valid course blueprint from these sources.");
            }

            // Replace any previous blueprint for this project.
            var existing = await _context.CourseBlueprints
                .FirstOrDefaultAsync(b => b.ProjectId == project.Id, ct);
            if (existing != null)
            {
                existing.Json = JsonSerializer.Serialize(document);
                existing.CreatedAt = DateTime.UtcNow;
                existing.ApprovedAt = null;
                await _context.SaveChangesAsync(ct);
                return existing;
            }

            var blueprint = new CourseBlueprint
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Json = JsonSerializer.Serialize(document),
                CreatedAt = DateTime.UtcNow
            };
            _context.Add(blueprint);
            await _context.SaveChangesAsync(ct);
            return blueprint;
        }

        internal static CourseBlueprintDocument? Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try
            {
                var trimmed = raw.Trim();
                if (trimmed.StartsWith("```"))
                {
                    trimmed = Regex.Replace(trimmed, "^```[a-zA-Z]*", "").Trim();
                    if (trimmed.EndsWith("```")) trimmed = trimmed[..^3].Trim();
                }

                var start = trimmed.IndexOf('{');
                var end = trimmed.LastIndexOf('}');
                if (start < 0 || end <= start) return null;
                trimmed = trimmed[start..(end + 1)];

                var doc = JsonSerializer.Deserialize<CourseBlueprintDocument>(trimmed,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (doc == null || doc.Modules.Count == 0) return null;

                doc.Title = string.IsNullOrWhiteSpace(doc.Title) ? "Untitled Course" : doc.Title.Trim();
                doc.Category = string.IsNullOrWhiteSpace(doc.Category) ? "General" : doc.Category.Trim();
                doc.EstimatedDuration = string.IsNullOrWhiteSpace(doc.EstimatedDuration)
                    ? $"{doc.Modules.Sum(m => Math.Max(1, m.EstimatedHours))} hours"
                    : doc.EstimatedDuration.Trim();

                for (var i = 0; i < doc.Modules.Count; i++)
                {
                    var module = doc.Modules[i];
                    module.Title = string.IsNullOrWhiteSpace(module.Title) ? $"Module {i + 1}" : module.Title.Trim();
                    if (module.EstimatedHours <= 0) module.EstimatedHours = 4;
                    foreach (var chapter in module.Chapters)
                    {
                        chapter.Title = string.IsNullOrWhiteSpace(chapter.Title) ? "Chapter" : chapter.Title.Trim();
                    }
                }

                return doc;
            }
            catch
            {
                return null;
            }
        }
    }
}
