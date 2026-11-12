using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class R2ObjectStorageService : IObjectStorageService, IDisposable
    {
        private readonly Lazy<AmazonS3Client> _client;
        private readonly ObjectStorageOptions _options;

        public R2ObjectStorageService(IOptions<ObjectStorageOptions> options)
        {
            _options = options.Value;
            _client = new Lazy<AmazonS3Client>(CreateClient, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private AmazonS3Client Client => _client.Value;

        private AmazonS3Client CreateClient()
        {
            if (!Uri.TryCreate(_options.ServiceUrl, UriKind.Absolute, out _)
                || string.IsNullOrWhiteSpace(_options.AccessKeyId)
                || string.IsNullOrWhiteSpace(_options.SecretAccessKey)
                || string.IsNullOrWhiteSpace(_options.Bucket))
            {
                throw new InvalidOperationException("Object storage is not configured.");
            }
            var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);
            return new AmazonS3Client(credentials, new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            });
        }

        public Task<SignedUploadUrl> CreateUploadUrlAsync(
            string key,
            string contentType,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expiresAt = DateTime.UtcNow.Add(lifetime);
            var url = Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                Verb = HttpVerb.PUT,
                ContentType = contentType,
                Expires = expiresAt
            });
            return Task.FromResult(new SignedUploadUrl(
                url,
                new Dictionary<string, string> { ["Content-Type"] = contentType },
                expiresAt));
        }

        public async Task<StoredObjectMetadata?> GetMetadataAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = _options.Bucket,
                    Key = key
                }, cancellationToken);
                return new StoredObjectMetadata(response.ContentLength, response.Headers.ContentType, response.ETag);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<string> BeginMultipartUploadAsync(
            string key,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var response = await Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                ContentType = contentType
            }, cancellationToken);
            return response.UploadId;
        }

        public Task<SignedUploadUrl> CreatePartUploadUrlAsync(
            string key,
            string uploadId,
            int partNumber,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expiresAt = DateTime.UtcNow.Add(lifetime);
            var url = Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                Verb = HttpVerb.PUT,
                UploadId = uploadId,
                PartNumber = partNumber,
                Expires = expiresAt
            });
            return Task.FromResult(new SignedUploadUrl(url, new Dictionary<string, string>(), expiresAt));
        }

        public async Task CompleteMultipartUploadAsync(
            string key,
            string uploadId,
            IReadOnlyList<UploadedPart> parts,
            CancellationToken cancellationToken = default)
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                UploadId = uploadId
            };
            request.AddPartETags(parts.OrderBy(part => part.PartNumber)
                .Select(part => new PartETag(part.PartNumber, part.ETag)));
            await Client.CompleteMultipartUploadAsync(request, cancellationToken);
        }

        public async Task AbortMultipartUploadAsync(
            string key,
            string uploadId,
            CancellationToken cancellationToken = default)
        {
            await Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                UploadId = uploadId
            }, cancellationToken);
        }

        public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            var response = await Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken);
            return response.ResponseStream;
        }

        public async Task PromoteAsync(
            string sourceKey,
            string destinationKey,
            CancellationToken cancellationToken = default)
        {
            await Client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = _options.Bucket,
                SourceKey = sourceKey,
                DestinationBucket = _options.Bucket,
                DestinationKey = destinationKey,
                MetadataDirective = S3MetadataDirective.COPY
            }, cancellationToken);
            await DeleteAsync(sourceKey, cancellationToken);
        }

        public async Task PutAsync(
            string key,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            await Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false
            }, cancellationToken);
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            await Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken);
        }

        public Task<string> CreateReadUrlAsync(
            string key,
            string? downloadFileName,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(lifetime)
            };
            if (!string.IsNullOrWhiteSpace(downloadFileName))
            {
                request.ResponseHeaderOverrides.ContentDisposition =
                    $"attachment; filename=\"{downloadFileName.Replace("\"", string.Empty)}\"";
            }
            return Task.FromResult(Client.GetPreSignedURL(request));
        }

        public void Dispose()
        {
            if (_client.IsValueCreated) _client.Value.Dispose();
        }
    }
}
