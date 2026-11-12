namespace IFA.Infrastructure.Configuration
{
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        public string DeliveryMode { get; set; } = "Development";
        public string ApiUrl { get; set; } = "https://api.resend.com/emails";
        public string ApiKey { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string AuthBaseUrl { get; set; } = "http://localhost:5173";
    }
}
