namespace IFA.Infrastructure.Configuration
{
    public sealed class ObjectStorageOptions
    {
        public const string SectionName = "ObjectStorage";

        public string ServiceUrl { get; set; } = string.Empty;
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        public string PublicBaseUrl { get; set; } = string.Empty;
        public int UploadUrlMinutes { get; set; } = 15;
        public int DownloadUrlMinutes { get; set; } = 10;
        public long MaxUploadBytes { get; set; } = 250L * 1024 * 1024;
    }
}
