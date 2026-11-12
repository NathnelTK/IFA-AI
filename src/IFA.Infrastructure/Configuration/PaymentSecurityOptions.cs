namespace IFA.Infrastructure.Configuration
{
    public sealed class PaymentSecurityOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string ReturnOrigins { get; set; } = string.Empty;
    }
}
