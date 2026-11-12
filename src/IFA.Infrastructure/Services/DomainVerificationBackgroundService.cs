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
    public sealed class DomainVerificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDomainDnsVerifier _dns;
        private readonly IExternalWebsiteVerifier _external;
        private readonly ICustomDomainProvisioner _provisioner;
        private readonly WorkerIdentity _identity;
        private readonly WorkerOptions _options;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<DomainVerificationBackgroundService> _logger;

        public DomainVerificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            IDomainDnsVerifier dns,
            IExternalWebsiteVerifier external,
            ICustomDomainProvisioner provisioner,
            WorkerIdentity identity,
            IOptions<WorkerOptions> options,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider,
            ILogger<DomainVerificationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _dns = dns;
            _external = external;
            _provisioner = provisioner;
            _identity = identity;
            _options = options.Value;
            _features = features.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Domains.Enabled || !_features.PublicPresence)
            {
                return;
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_features.CustomDomains)
                    {
                        foreach (var lease in await ClaimDomainsAsync(stoppingToken))
                        {
                            await VerifyDomainAsync(lease, stoppingToken);
                        }
                    }
                    foreach (var lease in await ClaimExternalSitesAsync(stoppingToken))
                    {
                        await VerifyExternalAsync(lease, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Public domain verification polling failed.");
                }
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Domains.PollSeconds), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task<List<DomainLease>> ClaimDomainsAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = Guid.NewGuid();
            var rows = await db.CustomDomains.FromSqlInterpolated($"""
                WITH candidates AS (
                    SELECT "Id" FROM "CustomDomains"
                    WHERE "DisabledAt" IS NULL
                      AND "Status" IN ('PendingDns', 'Provisioning', 'Failed')
                      AND "NextCheckAt" <= CURRENT_TIMESTAMP
                      AND ("LeaseExpiresAt" IS NULL OR "LeaseExpiresAt" < CURRENT_TIMESTAMP)
                    ORDER BY "NextCheckAt", "CreatedAt"
                    FOR UPDATE SKIP LOCKED
                    LIMIT {_options.Domains.MaxInFlight}
                )
                UPDATE "CustomDomains" AS domain
                SET "LeaseOwner" = {_identity.Value},
                    "LeaseToken" = {token},
                    "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Domains.LeaseSeconds} * INTERVAL '1 second'),
                    "AttemptCount" = domain."AttemptCount" + 1,
                    "UpdatedAt" = CURRENT_TIMESTAMP
                FROM candidates
                WHERE domain."Id" = candidates."Id"
                RETURNING domain.*
                """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return rows.Select(row => new DomainLease(row.Id, token)).ToList();
        }

        private async Task<List<DomainLease>> ClaimExternalSitesAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = Guid.NewGuid();
            var rows = await db.VerifiedExternalWebsites.FromSqlInterpolated($"""
                WITH candidates AS (
                    SELECT "Id" FROM "VerifiedExternalWebsites"
                    WHERE "Status" IN ('Pending', 'Failed')
                      AND "NextCheckAt" <= CURRENT_TIMESTAMP
                      AND ("LeaseExpiresAt" IS NULL OR "LeaseExpiresAt" < CURRENT_TIMESTAMP)
                    ORDER BY "NextCheckAt", "CreatedAt"
                    FOR UPDATE SKIP LOCKED
                    LIMIT {_options.Domains.MaxInFlight}
                )
                UPDATE "VerifiedExternalWebsites" AS site
                SET "LeaseOwner" = {_identity.Value},
                    "LeaseToken" = {token},
                    "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Domains.LeaseSeconds} * INTERVAL '1 second'),
                    "AttemptCount" = site."AttemptCount" + 1,
                    "UpdatedAt" = CURRENT_TIMESTAMP
                FROM candidates
                WHERE site."Id" = candidates."Id"
                RETURNING site.*
                """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return rows.Select(row => new DomainLease(row.Id, token)).ToList();
        }

        private async Task VerifyDomainAsync(DomainLease lease, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var domain = await db.CustomDomains.SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.Id && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (domain == null) return;
            var now = UtcNow();
            try
            {
                var txt = await _dns.HasTxtRecordAsync(domain.Host, domain.VerificationToken, cancellationToken);
                var cname = await _dns.HasCnameRecordAsync(domain.Host, domain.ExpectedCnameTarget, cancellationToken);
                if (!txt || !cname)
                {
                    domain.Status = "PendingDns";
                    domain.FailureCode = !txt ? "txt_record_missing" : "cname_record_missing";
                    domain.NextCheckAt = now.Add(Backoff(domain.AttemptCount));
                }
                else
                {
                    domain.Status = "Provisioning";
                    var ready = await _provisioner.EnsureReadyAsync(domain.Host, cancellationToken);
                    domain.Status = ready ? "Verified" : "Provisioning";
                    domain.FailureCode = null;
                    domain.VerifiedAt = ready ? now : null;
                    domain.NextCheckAt = ready ? DateTime.MaxValue : now.AddMinutes(2);
                }
            }
            catch (Exception ex)
            {
                domain.Status = "Failed";
                domain.FailureCode = "verification_unavailable";
                domain.NextCheckAt = now.Add(Backoff(domain.AttemptCount));
                _logger.LogWarning(ex, "Custom domain verification failed for domain record {DomainId}.", domain.Id);
            }
            ClearLease(domain);
            domain.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task VerifyExternalAsync(DomainLease lease, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var website = await db.VerifiedExternalWebsites.SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.Id && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (website == null) return;
            var now = UtcNow();
            try
            {
                var verified = await _external.VerifyAsync(website.Url, website.VerificationToken, cancellationToken);
                website.Status = verified ? "Verified" : "Pending";
                website.VerifiedAt = verified ? now : null;
                website.FailureCode = verified ? null : "verification_file_missing";
                website.NextCheckAt = verified ? DateTime.MaxValue : now.Add(Backoff(website.AttemptCount));
            }
            catch (Exception ex)
            {
                website.Status = "Failed";
                website.FailureCode = "verification_unavailable";
                website.NextCheckAt = now.Add(Backoff(website.AttemptCount));
                _logger.LogWarning(ex, "External website verification failed for record {WebsiteId}.", website.Id);
            }
            website.LeaseOwner = null;
            website.LeaseToken = null;
            website.LeaseExpiresAt = null;
            website.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static TimeSpan Backoff(int attempt) =>
            TimeSpan.FromMinutes(Math.Min(24 * 60, Math.Pow(2, Math.Clamp(attempt, 0, 10))));

        private static void ClearLease(CustomDomain domain)
        {
            domain.LeaseOwner = null;
            domain.LeaseToken = null;
            domain.LeaseExpiresAt = null;
        }

        private sealed record DomainLease(Guid Id, Guid Token);
    }
}
