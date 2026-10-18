using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public class BlueprintModuleSource
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class BlueprintChapter
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> SourceUrls { get; set; } = new List<string>();
    }

    public class BlueprintModule
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int EstimatedHours { get; set; } = 4;
        public List<BlueprintModuleSource> Sources { get; set; } = new List<BlueprintModuleSource>();
        public List<BlueprintChapter> Chapters { get; set; } = new List<BlueprintChapter>();
    }

    public class CourseBlueprintDocument
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string EstimatedDuration { get; set; } = string.Empty;
        public List<BlueprintModule> Modules { get; set; } = new List<BlueprintModule>();

        // Convenience accessors used by the educator controller / background
        // generation (not part of the persisted JSON shape).
        public List<string> GetModuleKeyTopics(BlueprintModule module) =>
            module.Chapters.Select(c => c.Title).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
    }

    /// <summary>
    /// Designs the AI course blueprint for an educator project from the
    /// included, extracted sources.
    /// </summary>
    public interface IBlueprintService
    {
        /// <exception cref="InvalidOperationException">The LLM could not produce a parseable blueprint.</exception>
        Task<CourseBlueprint> GenerateAsync(EducatorProject project, CancellationToken ct = default);
    }
}
