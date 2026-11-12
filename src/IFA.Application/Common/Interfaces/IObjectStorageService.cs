namespace IFA.Application.Common.Interfaces
{
    public sealed record SignedUploadUrl(
        string Url,
        IReadOnlyDictionary<string, string> RequiredHeaders,
        DateTime ExpiresAt);

    public sealed record StoredObjectMetadata(long SizeBytes, string? ContentType, string? ETag);
    public sealed record UploadedPart(int PartNumber, string ETag);

    public interface IObjectStorageService
    {
        Task<SignedUploadUrl> CreateUploadUrlAsync(
            string key,
            string contentType,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default);

        Task<StoredObjectMetadata?> GetMetadataAsync(
            string key,
            CancellationToken cancellationToken = default);

        Task<string> BeginMultipartUploadAsync(
            string key,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<SignedUploadUrl> CreatePartUploadUrlAsync(
            string key,
            string uploadId,
            int partNumber,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default);

        Task CompleteMultipartUploadAsync(
            string key,
            string uploadId,
            IReadOnlyList<UploadedPart> parts,
            CancellationToken cancellationToken = default);

        Task AbortMultipartUploadAsync(
            string key,
            string uploadId,
            CancellationToken cancellationToken = default);

        Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

        Task PromoteAsync(
            string sourceKey,
            string destinationKey,
            CancellationToken cancellationToken = default);

        Task PutAsync(
            string key,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(string key, CancellationToken cancellationToken = default);

        Task<string> CreateReadUrlAsync(
            string key,
            string? downloadFileName,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default);
    }
}
