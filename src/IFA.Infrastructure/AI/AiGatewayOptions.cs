using System.Collections.Generic;

namespace IFA.Infrastructure.AI
{
    public class AiGatewayOptions
    {
        public ProviderOptions Gemini { get; set; } = new();

        public ProviderOptions Groq { get; set; } = new();

        public List<string> ProviderFallbackOrder { get; set; } = new() { "Gemini", "Groq" };

        public int RequestsPerMinutePerProvider { get; set; } = 30;

        public bool EnableOfflineFallback { get; set; } = true;
    }

    public class ProviderOptions
    {
        public bool Enabled { get; set; } = true;

        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 60;
    }
}
