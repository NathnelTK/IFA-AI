namespace IFA.Infrastructure.Configuration
{
    public sealed class OcrOptions
    {
        public const string SectionName = "Ocr";

        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 60;
    }
}
