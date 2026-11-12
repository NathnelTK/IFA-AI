namespace IFA.Domain.Entities
{
    public class EducatorPublicProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public Guid? PhotoAssetId { get; set; }
        public string ExpertiseJson { get; set; } = "[]";
        public string LinksJson { get; set; } = "[]";
        public string ThemeJson { get; set; } = "{}";
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
    }

    public class OrganizationSite
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid? LogoAssetId { get; set; }
        public string ThemeJson { get; set; } = "{}";
        public string SeoJson { get; set; } = "{}";
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
    }

    public enum OrganizationPageType
    {
        Home,
        Courses,
        About,
        Instructors,
        Updates,
        Testimonials,
        Faq,
        Contact
    }

    public class OrganizationSitePage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationSiteId { get; set; }
        public OrganizationPageType PageType { get; set; }
        public bool IsVisible { get; set; } = true;
        public int NavigationOrder { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SchemaJson { get; set; } = "{}";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CustomDomain
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Host { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string VerificationToken { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public DateTime NextCheckAt { get; set; } = DateTime.UtcNow;
        public string? LeaseOwner { get; set; }
        public Guid? LeaseToken { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
        public string ExpectedCnameTarget { get; set; } = "domains.witforge.com";
        public string? FailureCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        public DateTime? DisabledAt { get; set; }
    }

    public class VerifiedExternalWebsite
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string VerificationToken { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public DateTime NextCheckAt { get; set; } = DateTime.UtcNow;
        public string? LeaseOwner { get; set; }
        public Guid? LeaseToken { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        public string? FailureCode { get; set; }
    }
}
