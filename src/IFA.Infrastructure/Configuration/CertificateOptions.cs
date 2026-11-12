namespace IFA.Infrastructure.Configuration
{
    public sealed class CertificateOptions
    {
        public const string SectionName = "Certificates";

        public string PublicBaseUrl { get; set; } = "https://witforge.com";
    }
}
