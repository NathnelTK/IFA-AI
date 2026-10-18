using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public class SourceConflictFinding
    {
        /// <summary>Duplicate | Conflict | Unrelated</summary>
        public string Kind { get; set; } = "Duplicate";
        public List<Guid> SourceIds { get; set; } = new List<Guid>();
        public string Explanation { get; set; } = string.Empty;
        public List<string> SuggestedResolutions { get; set; } = new List<string>();
    }

    public class SourceAnalysisResult
    {
        public List<SourceConflictFinding> Conflicts { get; set; } = new List<SourceConflictFinding>();
    }

    /// <summary>
    /// Analyzes the extracted content of a project's included sources with one
    /// LLM call and persists the resulting SourceConflict rows.
    /// </summary>
    public interface ISourceAnalysisService
    {
        Task<SourceAnalysisResult> AnalyzeAsync(EducatorProject project, CancellationToken ct = default);
    }
}
