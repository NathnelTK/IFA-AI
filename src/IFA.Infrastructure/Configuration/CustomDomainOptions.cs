namespace IFA.Infrastructure.Configuration
{
    public sealed class CustomDomainOptions
    {
        public const string SectionName = "CustomDomains";

        public string DnsOverHttpsUrl { get; set; } = "https://cloudflare-dns.com/dns-query";
        public string VercelApiUrl { get; set; } = "https://api.vercel.com";
        public string VercelToken { get; set; } = string.Empty;
        public string VercelProjectId { get; set; } = string.Empty;
        public string VercelTeamId { get; set; } = string.Empty;
        public string CnameTarget { get; set; } = "domains.witforge.com";
    }
}
