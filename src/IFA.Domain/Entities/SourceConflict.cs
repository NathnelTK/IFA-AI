using System;
using System.Collections.Generic;

namespace IFA.Domain.Entities
{
    /// <summary>
    /// A duplicate-concept, contradiction, or off-topic finding between project
    /// sources, plus the educator's resolution.
    /// </summary>
    public class SourceConflict
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        /// <summary>Duplicate | Conflict | Unrelated</summary>
        public string Kind { get; set; } = "Duplicate";
        public string SourceIds { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        /// <summary>JSON list of AI-proposed resolution options.</summary>
        public string? SuggestedResolutions { get; set; }
        /// <summary>Keep | Remove | Merge | ManualNote</summary>
        public string? Resolution { get; set; }
        public string? ManualNote { get; set; }
        /// <summary>Null until the educator resolves it. Ai | User</summary>
        public string? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public EducatorProject? Project { get; set; }
    }
}
