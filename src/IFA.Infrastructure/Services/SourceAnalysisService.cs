using System;
using System.Collections.Generic;
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
    /// One LLM call over the digest of a project's included sources to find
    /// duplicate concepts, contradictions and off-topic material. Persists the
    /// findings as SourceConflict rows (previous unresolved rows are cleared).
    /// </summary>
    public class SourceAnalysisService : ISourceAnalysisService
    {
        private readonly ILlmGateway _llmGateway;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<SourceAnalysisService> _logger;

        public SourceAnalysisService(
            ILlmGateway llmGateway,
            IApplicationDbContext context,
            ILogger<SourceAnalysisService> logger)
        {
            _llmGateway = llmGateway;
            _context = context;
            _logger = logger;
        }

        public async Task<SourceAnalysisResult> AnalyzeAsync(EducatorProject project, CancellationToken ct = default)
        {
            var sources = project.Sources
                .Where(s => s.Included)
                .OrderBy(s => s.CreatedAt)
                .ToList();

            if (sources.Count == 0)
            {
                await ClearUnresolvedConflictsAsync(project.Id, ct);
                return new SourceAnalysisResult();
            }

            var digest = BuildDigest(sources);

            List<ConflictFindingDto>? findings = null;
            try
            {
                var raw = await _llmGateway.CompleteAsync(
                    PromptRegistry.Educator_SourceAnalysisSystemPrompt, digest, LlmRole.Model2_Architect, ct);
                findings = ParseFindings(raw);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Source analysis LLM call failed for project {ProjectId}.", project.Id);
            }

            if (findings == null)
            {
                // One retry with a stricter instruction before giving up.
                try
                {
                    var raw = await _llmGateway.CompleteAsync(
                        PromptRegistry.Educator_SourceAnalysisSystemPrompt,
                        digest + "\n\nSTRICT REMINDER: Reply with ONLY the raw JSON array, no prose, no code fences.",
                        LlmRole.Model2_Architect, ct);
                    findings = ParseFindings(raw);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Source analysis retry failed for project {ProjectId}.", project.Id);
                }
            }

            var result = new SourceAnalysisResult();
            if (findings == null)
            {
                // Analysis is best-effort: no conflicts found does not block the pipeline.
                return result;
            }

            await ClearUnresolvedConflictsAsync(project.Id, ct);

            foreach (var finding in findings)
            {
                var sourceIds = finding.SourceIndexes
                    .Where(i => i >= 0 && i < sources.Count)
                    .Select(i => sources[i].Id)
                    .Distinct()
                    .ToList();

                if (sourceIds.Count == 0) continue;

                var kind = NormalizeKind(finding.Kind);
                var conflict = new SourceConflict
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Kind = kind,
                    SourceIds = string.Join(";", sourceIds),
                    Explanation = Truncate(finding.Explanation, 2000) ?? "Potential overlap detected between sources.",
                    SuggestedResolutions = JsonSerializer.Serialize(
                        (finding.SuggestedResolutions ?? new List<string>()).Take(5).ToList()),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Add(conflict);
                result.Conflicts.Add(new SourceConflictFinding
                {
                    Kind = kind,
                    SourceIds = sourceIds,
                    Explanation = conflict.Explanation,
                    SuggestedResolutions = (finding.SuggestedResolutions ?? new List<string>()).Take(5).ToList()
                });
            }

            await _context.SaveChangesAsync(ct);
            return result;
        }

        private async Task ClearUnresolvedConflictsAsync(Guid projectId, CancellationToken ct)
        {
            var unresolved = await _context.SourceConflicts
                .Where(c => c.ProjectId == projectId && c.ResolvedAt == null)
                .ToListAsync(ct);
            foreach (var conflict in unresolved)
            {
                _context.Remove(conflict);
            }
            if (unresolved.Count > 0)
            {
                await _context.SaveChangesAsync(ct);
            }
        }

        internal static string BuildDigest(List<ProjectSource> sources)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Analyze these learning sources. For each, an index, URL, title and extracted text sample follow.");
            sb.AppendLine();
            for (var i = 0; i < sources.Count; i++)
            {
                var s = sources[i];
                var content = string.IsNullOrWhiteSpace(s.ExtractedContent)
                    ? "(no text extracted â€” metadata only)"
                    : s.ExtractedContent![..Math.Min(4000, s.ExtractedContent.Length)];
                sb.AppendLine($"[{i}] URL: {s.Url}");
                sb.AppendLine($"    Title: {s.Title}");
                sb.AppendLine($"    Extracted: {content.ReplaceLineEndings(" ")}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        internal static List<ConflictFindingDto>? ParseFindings(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try
            {
                var json = ExtractJsonSubstring(raw);
                if (json == null) return null;

                List<ConflictFindingDto>? list = null;
                try
                {
                    list = JsonSerializer.Deserialize<List<ConflictFindingDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    // Maybe wrapped: { "conflicts": [...] }
                }

                if (list == null)
                {
                    var wrapped = JsonSerializer.Deserialize<ConflictListDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    list = wrapped?.Conflicts;
                }

                return list?
                    .Where(f => f != null && f.SourceIndexes is { Count: > 0 })
                    .ToList();
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtractJsonSubstring(string raw)
        {
            raw = raw.Trim();
            if (raw.StartsWith("```"))
            {
                raw = Regex.Replace(raw, "^```[a-zA-Z]*", "").Trim();
                if (raw.EndsWith("```")) raw = raw[..^3].Trim();
            }
            var start = raw.IndexOf('[');
            var end = raw.LastIndexOf(']');
            if (start >= 0 && end > start) return raw[start..(end + 1)];
            return null;
        }

        private static string NormalizeKind(string? kind)
        {
            return kind?.Trim().ToLowerInvariant() switch
            {
                "conflict" => "Conflict",
                "unrelated" => "Unrelated",
                _ => "Duplicate"
            };
        }

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? null
            : value.Length <= max ? value
            : value[..max];

        internal class ConflictFindingDto
        {
            public string? Kind { get; set; }
            public List<int>? SourceIndexes { get; set; }
            public string? Explanation { get; set; }
            public List<string>? SuggestedResolutions { get; set; }
        }

        internal class ConflictListDto
        {
            public List<ConflictFindingDto>? Conflicts { get; set; }
        }
    }
}
