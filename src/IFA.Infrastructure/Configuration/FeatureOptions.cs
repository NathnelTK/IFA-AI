namespace IFA.Infrastructure.Configuration
{
    public sealed class FeatureOptions
    {
        public const string SectionName = "Features";

        public bool CourseGeneration { get; set; } = true;
        public bool DurableOutbox { get; set; } = true;
        public bool Workspaces { get; set; }
        public bool OpaqueSessions { get; set; }
        public bool UnifiedAuthoring { get; set; }
        public bool AssetUploads { get; set; }
        public bool ImmutableReleases { get; set; }
        public bool PublicPresence { get; set; }
        public bool CustomDomains { get; set; }
        public bool Social { get; set; }
        public bool Certificates { get; set; }
        public bool Offers { get; set; }
        public bool RedesignedShells { get; set; }
    }
}
