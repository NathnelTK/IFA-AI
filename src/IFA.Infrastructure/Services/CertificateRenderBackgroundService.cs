using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
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
    public sealed class CertificateRenderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICertificateRenderer _renderer;
        private readonly IObjectStorageService _storage;
        private readonly WorkerIdentity _identity;
        private readonly WorkerOptions _options;
        private readonly FeatureOptions _features;
        private readonly CertificateOptions _certificateOptions;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<CertificateRenderBackgroundService> _logger;

        public CertificateRenderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ICertificateRenderer renderer,
            IObjectStorageService storage,
            WorkerIdentity identity,
            IOptions<WorkerOptions> options,
            IOptions<FeatureOptions> features,
            IOptions<CertificateOptions> certificateOptions,
            TimeProvider timeProvider,
            ILogger<CertificateRenderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _renderer = renderer;
            _storage = storage;
            _identity = identity;
            _options = options.Value;
            _features = features.Value;
            _certificateOptions = certificateOptions.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Certificates.Enabled || !_features.Certificates) return;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var leases = await ClaimAsync(stoppingToken);
                    await Task.WhenAll(leases.Select(lease => ProcessAsync(lease, stoppingToken)));
                    if (leases.Count > 0) continue;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Certificate rendering poll failed.");
                }
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Certificates.PollSeconds), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task<List<JobLease>> ClaimAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = Guid.NewGuid();
            var rows = await db.CertificateRenderJobs.FromSqlInterpolated($"""
                WITH candidates AS (
                    SELECT "Id" FROM "CertificateRenderJobs"
                    WHERE (("State" = 'Queued' AND "AvailableAt" <= CURRENT_TIMESTAMP)
                       OR ("State" = 'Running' AND "LeaseExpiresAt" < CURRENT_TIMESTAMP))
                      AND "AttemptCount" < "MaxAttempts"
                    ORDER BY "AvailableAt", "CreatedAt"
                    FOR UPDATE SKIP LOCKED
                    LIMIT {_options.Certificates.MaxInFlight}
                )
                UPDATE "CertificateRenderJobs" AS job
                SET "State" = 'Running',
                    "AttemptCount" = job."AttemptCount" + 1,
                    "LeaseOwner" = {_identity.Value},
                    "LeaseToken" = {token},
                    "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Certificates.LeaseSeconds} * INTERVAL '1 second'),
                    "UpdatedAt" = CURRENT_TIMESTAMP
                FROM candidates
                WHERE job."Id" = candidates."Id"
                RETURNING job.*
                """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return rows.Select(row => new JobLease(row.Id, token)).ToList();
        }

        private async Task ProcessAsync(JobLease lease, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var job = await db.CertificateRenderJobs.SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.Id && candidate.LeaseToken == lease.Token && candidate.State == "Running",
                cancellationToken);
            if (job == null) return;
            try
            {
                var certificate = await db.CourseCertificates.SingleAsync(candidate => candidate.Id == job.CertificateId, cancellationToken);
                var completion = await db.CourseCompletionRecords.SingleAsync(candidate => candidate.Id == certificate.CompletionRecordId, cancellationToken);
                var workspaceId = await db.Courses
                    .Where(course => course.Id == completion.CourseId)
                    .Select(course => course.WorkspaceId)
                    .SingleAsync(cancellationToken)
                    ?? throw new InvalidOperationException("The certificate course has no workspace.");
                var verificationUrl = $"{_certificateOptions.PublicBaseUrl.TrimEnd('/')}/certificates/verify/{certificate.SerialNumber}?code={certificate.VerificationCode}";
                await using var pdf = await _renderer.RenderAsync(
                    completion.SnapshotJson,
                    certificate.SerialNumber,
                    verificationUrl,
                    cancellationToken);
                var storageKey = $"private/{workspaceId:N}/certificates/{certificate.Id:N}.pdf";
                await _storage.PutAsync(storageKey, pdf, "application/pdf", cancellationToken);

                await db.Entry(job).ReloadAsync(cancellationToken);
                if (job.LeaseToken != lease.Token || job.State != "Running") return;
                var now = UtcNow();
                certificate.Status = "Ready";
                certificate.PdfStorageKey = storageKey;
                certificate.RenderedAt = now;
                certificate.RenderingErrorCode = null;
                job.State = "Succeeded";
                job.CompletedAt = now;
                job.UpdatedAt = now;
                ClearLease(job);
                db.OutboxMessages.Add(new OutboxMessage
                {
                    Type = "notification.delivery",
                    AggregateType = nameof(CourseCertificate),
                    AggregateId = certificate.Id,
                    DeduplicationKey = $"certificate-ready:{certificate.Id}:{completion.LearnerId}",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        LearnerId = completion.LearnerId,
                        Type = "certificate_ready",
                        Title = "Your certificate is ready",
                        Message = "Download or verify your course completion certificate.",
                        LinkUrl = $"/certificates/{certificate.Id}",
                        Preference = "Announcements"
                    }),
                    OccurredAt = now,
                    AvailableAt = now,
                    CausationId = job.Id.ToString()
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Certificate render job {JobId} failed.", job.Id);
                var now = UtcNow();
                var terminal = job.AttemptCount >= job.MaxAttempts;
                job.State = terminal ? "Failed" : "Queued";
                job.AvailableAt = terminal ? now : now.AddSeconds(Math.Min(600, Math.Pow(2, job.AttemptCount) * 10));
                job.ErrorCode = terminal ? "certificate_render_failed" : "certificate_render_retry";
                job.ErrorDetail = ex.Message[..Math.Min(ex.Message.Length, 1000)];
                job.CompletedAt = terminal ? now : null;
                job.UpdatedAt = now;
                ClearLease(job);
                var certificate = await db.CourseCertificates.SingleAsync(candidate => candidate.Id == job.CertificateId, cancellationToken);
                certificate.Status = terminal ? "Failed" : "Queued";
                certificate.RenderingErrorCode = job.ErrorCode;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        private static void ClearLease(CertificateRenderJob job)
        {
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
        private sealed record JobLease(Guid Id, Guid Token);
    }
}
