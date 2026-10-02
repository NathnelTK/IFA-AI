namespace IFA.Domain.Entities;

public class PracticalResource
{
    public Guid Id { get; set; }
    public Guid ResearchPackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty; // Tutorial, Documentation, Code Sample, etc.
    public string Source { get; set; } = string.Empty; // GitHub, Stack Overflow, Documentation site, etc.
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ResearchPackage ResearchPackage { get; set; } = null!;
}