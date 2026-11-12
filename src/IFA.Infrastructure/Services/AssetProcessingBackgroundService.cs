using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using System.Text;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class AssetProcessingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IObjectStorageService _storage;
        private readonly IMalwareScanner _scanner;
        private readonly WorkerIdentity _identity;
        private readonly WorkerOptions _options;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AssetProcessingBackgroundService> _logger;

        public AssetProcessingBackgroundService(
            IServiceScopeFactory scopeFactory,
            IObjectStorageService storage,
            IMalwareScanner scanner,
            WorkerIdentity identity,
            IOptions<WorkerOptions> options,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider,
            ILogger<AssetProcessingBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _storage = storage;
            _scanner = scanner;
            _identity = identity;
            _options = options.Value;
            _features = features.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Assets.Enabled || !_features.AssetUploads)
            {
                return;
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var leases = await ClaimAsync(stoppingToken);
                    if (leases.Count > 0)
                    {
                        await Task.WhenAll(leases.Select(lease => ProcessAsync(lease, stoppingToken)));
                        continue;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Asset processing polling failed.");
                }
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Assets.PollSeconds), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task<List<AssetLease>> ClaimAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = Guid.NewGuid();
            var jobs = await db.AssetProcessingJobs.FromSqlInterpolated($"""
                WITH candidates AS (
                    SELECT "Id"
                    FROM "AssetProcessingJobs"
                    WHERE (("State" = 'Queued' AND "AvailableAt" <= CURRENT_TIMESTAMP)
                       OR ("State" = 'Running' AND "LeaseExpiresAt" < CURRENT_TIMESTAMP))
                      AND "AttemptCount" < "MaxAttempts"
                    ORDER BY "AvailableAt", "CreatedAt", "Id"
                    FOR UPDATE SKIP LOCKED
                    LIMIT {_options.Assets.MaxInFlight}
                )
                UPDATE "AssetProcessingJobs" AS job
                SET "State" = 'Running',
                    "AttemptCount" = job."AttemptCount" + 1,
                    "LeaseOwner" = {_identity.Value},
                    "LeaseToken" = {token},
                    "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Assets.LeaseSeconds} * INTERVAL '1 second'),
                    "UpdatedAt" = CURRENT_TIMESTAMP,
                    "ErrorCode" = NULL,
                    "ErrorDetail" = NULL
                FROM candidates
                WHERE job."Id" = candidates."Id"
                RETURNING job.*
                """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return jobs.Select(job => new AssetLease(job.Id, token)).ToList();
        }

        private async Task ProcessAsync(AssetLease lease, CancellationToken stoppingToken)
        {
            using var execution = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var heartbeat = HeartbeatAsync(lease, execution);
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var job = await db.AssetProcessingJobs.SingleAsync(candidate =>
                    candidate.Id == lease.JobId
                    && candidate.State == "Running"
                    && candidate.LeaseToken == lease.Token,
                    execution.Token);
                var asset = await db.Assets.SingleAsync(candidate => candidate.Id == job.AssetId, execution.Token);
                if (job.Kind == "Extract")
                {
                    await ExtractAsync(scope.ServiceProvider, db, job, asset, lease, execution.Token);
                    return;
                }
                if (job.Kind != "Scan")
                {
                    throw new InvalidOperationException($"Unsupported asset job kind '{job.Kind}'.");
                }
                if (asset.Status is not (AssetStatus.Quarantined or AssetStatus.Scanning))
                {
                    throw new InvalidOperationException("Only quarantined assets can be scanned.");
                }
                asset.Status = AssetStatus.Scanning;
                asset.UpdatedAt = UtcNow();
                await db.SaveChangesAsync(execution.Token);

                await using var content = await _storage.OpenReadAsync(asset.QuarantineStorageKey, execution.Token);
                var result = await _scanner.ScanAsync(content, execution.Token);
                await db.Entry(job).ReloadAsync(execution.Token);
                if (job.LeaseToken != lease.Token || job.State != "Running")
                {
                    return;
                }

                var now = UtcNow();
                asset.VerifiedSha256 = result.Sha256;
                asset.ScannedAt = now;
                asset.UpdatedAt = now;
                if (!result.Sha256.Equals(asset.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    asset.Status = AssetStatus.Rejected;
                    asset.RejectionCode = "checksum_mismatch";
                    await _storage.DeleteAsync(asset.QuarantineStorageKey, execution.Token);
                }
                else if (!result.IsClean)
                {
                    asset.Status = AssetStatus.Infected;
                    asset.RejectionCode = "malware_detected";
                    await _storage.DeleteAsync(asset.QuarantineStorageKey, execution.Token);
                }
                else
                {
                    asset.CleanStorageKey = $"private/{asset.WorkspaceId:N}/{asset.Id:N}/{asset.OriginalFileName}";
                    await _storage.PromoteAsync(asset.QuarantineStorageKey, asset.CleanStorageKey, execution.Token);
                    asset.Status = AssetStatus.Clean;
                    asset.RejectionCode = null;
                    if (asset.Purpose == "CourseMaterial" && SupportsExtraction(asset.ContentType, asset.OriginalFileName))
                    {
                        db.AssetProcessingJobs.Add(new AssetProcessingJob
                        {
                            AssetId = asset.Id,
                            Kind = "Extract",
                            State = "Queued",
                            AvailableAt = now,
                            CorrelationId = job.CorrelationId,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }

                job.State = "Succeeded";
                job.CompletedAt = now;
                job.UpdatedAt = now;
                ClearLease(job);
                db.AuditEvents.Add(new AuditEvent
                {
                    ActorLearnerId = asset.OwnerAccountId,
                    Action = asset.Status == AssetStatus.Clean ? "asset.scan_clean" : "asset.scan_rejected",
                    ResourceType = nameof(Asset),
                    ResourceId = asset.Id.ToString(),
                    Outcome = asset.Status.ToString(),
                    ErrorCode = asset.RejectionCode,
                    CorrelationId = job.CorrelationId,
                    OccurredAt = now
                });
                await db.SaveChangesAsync(execution.Token);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await ReleaseAsync(lease, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Asset processing job {JobId} failed.", lease.JobId);
                await FailAsync(lease, ex, CancellationToken.None);
            }
            finally
            {
                execution.Cancel();
                try { await heartbeat; } catch (OperationCanceledException) { }
            }
        }

        private async Task ExtractAsync(
            IServiceProvider services,
            ApplicationDbContext db,
            AssetProcessingJob job,
            Asset asset,
            AssetLease lease,
            CancellationToken cancellationToken)
        {
            if (asset.Status != AssetStatus.Clean || string.IsNullOrWhiteSpace(asset.CleanStorageKey))
            {
                throw new InvalidOperationException("Only a clean asset can be extracted.");
            }
            var extractor = services.GetRequiredService<IAssetContentExtractor>();
            await using var source = await _storage.OpenReadAsync(asset.CleanStorageKey, cancellationToken);
            var text = await extractor.ExtractAsync(source, asset.ContentType, asset.OriginalFileName, cancellationToken);
            var key = $"derived/{asset.WorkspaceId:N}/{asset.Id:N}/extracted.txt";
            await using var textStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            await _storage.PutAsync(key, textStream, "text/plain; charset=utf-8", cancellationToken);

            await db.Entry(job).ReloadAsync(cancellationToken);
            if (job.LeaseToken != lease.Token || job.State != "Running")
            {
                return;
            }
            var derivative = await db.AssetDerivatives.SingleOrDefaultAsync(candidate =>
                candidate.AssetId == asset.Id && candidate.Kind == "ExtractedText",
                cancellationToken);
            if (derivative == null)
            {
                derivative = new AssetDerivative
                {
                    AssetId = asset.Id,
                    Kind = "ExtractedText",
                    CreatedAt = UtcNow()
                };
                db.AssetDerivatives.Add(derivative);
            }
            derivative.StorageKey = key;
            derivative.ContentType = "text/plain";
            derivative.SizeBytes = textStream.Length;
            derivative.MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { characters = text.Length });
            var sources = await db.AuthoringSources.Where(candidate => candidate.AssetId == asset.Id).ToListAsync(cancellationToken);
            foreach (var authoringSource in sources)
            {
                authoringSource.ExtractedTextReference = key;
                authoringSource.Status = "Extracted";
                authoringSource.ErrorCode = null;
                authoringSource.UpdatedAt = UtcNow();
                var project = await db.AuthoringProjects.SingleAsync(
                    candidate => candidate.Id == authoringSource.AuthoringProjectId,
                    cancellationToken);
                var legacyProjectId = project.LegacyEducatorProjectId ?? project.Id;
                var legacySource = authoringSource.LegacyProjectSourceId.HasValue
                    ? await db.ProjectSources.SingleOrDefaultAsync(
                        candidate => candidate.Id == authoringSource.LegacyProjectSourceId.Value,
                        cancellationToken)
                    : null;
                if (legacySource == null)
                {
                    legacySource = new ProjectSource
                    {
                        Id = authoringSource.Id,
                        ProjectId = legacyProjectId,
                        Url = $"asset:{asset.Id}",
                        Type = LegacySourceType(asset.ContentType),
                        Title = asset.OriginalFileName,
                        AddedBy = "User",
                        Included = true,
                        CreatedAt = authoringSource.CreatedAt
                    };
                    db.ProjectSources.Add(legacySource);
                    authoringSource.LegacyProjectSourceId = legacySource.Id;
                }
                legacySource.ExtractedContent = text[..Math.Min(text.Length, 12_000)];
                legacySource.Status = "Extracted";
                legacySource.FailureReason = null;
            }
            job.State = "Succeeded";
            job.CompletedAt = UtcNow();
            job.UpdatedAt = UtcNow();
            ClearLease(job);
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task HeartbeatAsync(AssetLease lease, CancellationTokenSource execution)
        {
            while (!execution.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Assets.HeartbeatSeconds), _timeProvider, execution.Token);
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
                        UPDATE "AssetProcessingJobs"
                        SET "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Assets.LeaseSeconds} * INTERVAL '1 second'),
                            "UpdatedAt" = CURRENT_TIMESTAMP
                        WHERE "Id" = {lease.JobId} AND "State" = 'Running' AND "LeaseToken" = {lease.Token}
                        """, execution.Token);
                    if (updated == 0) execution.Cancel();
                }
                catch (OperationCanceledException) when (execution.IsCancellationRequested)
                {
                    return;
                }
                catch
                {
                    execution.Cancel();
                }
            }
        }

        private async Task FailAsync(AssetLease lease, Exception exception, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var job = await db.AssetProcessingJobs.SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.JobId && candidate.State == "Running" && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (job == null) return;
            var terminal = exception is InvalidOperationException || job.AttemptCount >= job.MaxAttempts;
            var now = UtcNow();
            job.State = terminal ? "Failed" : "Queued";
            job.AvailableAt = terminal ? now : now.AddSeconds(Math.Min(600, Math.Pow(2, job.AttemptCount) * 10));
            job.ErrorCode = exception is InvalidOperationException ? "asset_invalid_state" : terminal ? "asset_scan_attempts_exhausted" : "asset_scan_unavailable";
            job.ErrorDetail = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            job.CompletedAt = terminal ? now : null;
            job.UpdatedAt = now;
            ClearLease(job);
            var asset = await db.Assets.SingleAsync(candidate => candidate.Id == job.AssetId, cancellationToken);
            if (job.Kind == "Extract")
            {
                asset.Status = AssetStatus.Clean;
                asset.RejectionCode = null;
                if (terminal)
                {
                    var sources = await db.AuthoringSources.Where(source => source.AssetId == asset.Id).ToListAsync(cancellationToken);
                    foreach (var source in sources)
                    {
                        source.Status = "Failed";
                        source.ErrorCode = job.ErrorCode;
                        source.UpdatedAt = now;
                    }
                }
            }
            else
            {
                asset.Status = terminal ? AssetStatus.Rejected : AssetStatus.Quarantined;
                asset.RejectionCode = terminal ? job.ErrorCode : null;
            }
            asset.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task ReleaseAsync(AssetLease lease, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var job = await db.AssetProcessingJobs.SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.JobId && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (job == null) return;
            job.State = "Queued";
            job.AttemptCount = Math.Max(0, job.AttemptCount - 1);
            job.AvailableAt = UtcNow();
            job.UpdatedAt = UtcNow();
            ClearLease(job);
            await db.SaveChangesAsync(cancellationToken);
        }

        private static void ClearLease(AssetProcessingJob job)
        {
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static bool SupportsExtraction(string contentType, string fileName) =>
            contentType is "application/pdf"
                or "application/vnd.openxmlformats-officedocument.presentationml.presentation"
                or "text/plain"
                or "text/markdown"
            || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);

        private static string LegacySourceType(string contentType) => contentType switch
        {
            "application/pdf" => "Pdf",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "Ppt",
            _ when contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) => "Image",
            _ => "Other"
        };

        private sealed record AssetLease(Guid JobId, Guid Token);
    }
}
