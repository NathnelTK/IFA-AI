namespace IFA.Domain.Entities;

public class AcademicEvidence
{
    public Guid Id { get; set; }
    public Guid ResearchPackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string Doi { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime PublicationDate { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ResearchPackage ResearchPackage { get; set; } = null!;
}