namespace IFA.Infrastructure.Configuration
{
    public sealed class AuthOptions
    {
        public const string SectionName = "Auth";

        public string Issuer { get; set; } = "Witforge.Auth";
        public string LegacyAudience { get; set; } = "Witforge.LegacyClient";
        public int SessionHours { get; set; } = 12;
        public int InternalRequestMaxSkewSeconds { get; set; } = 90;
        public string BffSigningSecret { get; set; } = string.Empty;
        public string ContextPepper { get; set; } = string.Empty;
        public string DataProtectionKey { get; set; } = string.Empty;
        public string[] CentralHosts { get; set; } = ["auth.witforge.com"];
        public string[] AppHosts { get; set; } = ["app.witforge.com"];
        public string[] PublicHosts { get; set; } = ["witforge.com", "www.witforge.com"];
        public List<SsoClientOptions> Clients { get; set; } = [];
    }

    public sealed class SsoClientOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string TargetHost { get; set; } = string.Empty;
        public string TargetSite { get; set; } = "App";
        public string[] RedirectUris { get; set; } = [];
    }
}
