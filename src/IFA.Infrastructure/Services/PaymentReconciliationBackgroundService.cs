using System.Text.Json;
using IFA.Domain.Entities;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class PaymentReconciliationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IChapaPaymentService _chapa;
        private readonly WorkerIdentity _identity;
        private readonly WorkerOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PaymentReconciliationBackgroundService> _logger;

        public PaymentReconciliationBackgroundService(
            IServiceScopeFactory scopeFactory,
            IChapaPaymentService chapa,
            WorkerIdentity identity,
            IOptions<WorkerOptions> options,
            TimeProvider timeProvider,
            ILogger<PaymentReconciliationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _chapa = chapa;
            _identity = identity;
            _options = options.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Payments.Enabled || !_chapa.IsConfigured) return;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var leases = await ClaimAsync(stoppingToken);
                    await Task.WhenAll(leases.Select(lease => ReconcileAsync(lease, stoppingToken)));
                    if (leases.Count > 0) continue;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Payment reconciliation polling failed.");
                }
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Payments.PollSeconds), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task<List<PaymentLease>> ClaimAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = Guid.NewGuid();
            var rows = await db.Payments.FromSqlInterpolated($"""
                WITH candidates AS (
                    SELECT "Id" FROM "Payments"
                    WHERE "Status" IN ('Pending', 'Initializing')
                      AND "NextVerificationAt" <= CURRENT_TIMESTAMP
                      AND ("LeaseExpiresAt" IS NULL OR "LeaseExpiresAt" < CURRENT_TIMESTAMP)
                    ORDER BY "NextVerificationAt", "CreatedAt"
                    FOR UPDATE SKIP LOCKED
                    LIMIT {_options.Payments.MaxInFlight}
                )
                UPDATE "Payments" AS payment
                SET "LeaseOwner" = {_identity.Value},
                    "LeaseToken" = {token},
                    "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Payments.LeaseSeconds} * INTERVAL '1 second'),
                    "VerificationAttemptCount" = payment."VerificationAttemptCount" + 1
                FROM candidates
                WHERE payment."Id" = candidates."Id"
                RETURNING payment.*
                """)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return rows.Select(row => new PaymentLease(row.Id, token)).ToList();
        }

        private async Task ReconcileAsync(PaymentLease lease, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var payment = await db.Payments.SingleOrDefaultAsync(candidate =>
                    candidate.Id == lease.Id && candidate.LeaseToken == lease.Token,
                    cancellationToken);
                if (payment == null) return;
                var verification = await _chapa.VerifyAsync(payment.ProviderTxRef, cancellationToken);
                var now = UtcNow();
                if (verification.Success
                    && verification.Amount == payment.Amount
                    && string.Equals(verification.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    var enrollment = await db.CourseEnrollments.SingleOrDefaultAsync(candidate =>
                        candidate.CourseId == payment.CourseId && candidate.LearnerId == payment.LearnerId,
                        cancellationToken);
                    if (enrollment == null
                        || payment.ReleaseId.HasValue && enrollment.ReleaseId != payment.ReleaseId
                        || payment.CohortId.HasValue && enrollment.CohortId != payment.CohortId)
                    {
                        payment.Status = "Failed";
                        payment.FailureCode = "enrollment_snapshot_mismatch";
                    }
                    else
                    {
                        payment.Status = "Completed";
                        payment.CompletedAt = now;
                        payment.ProviderTransactionId = verification.ProviderTransactionId;
                        payment.VerificationPayloadHash = verification.PayloadHash;
                        payment.FailureCode = null;
                        if (enrollment.Status == "PendingPayment")
                        {
                            var mode = await db.Courses.Where(course => course.Id == payment.CourseId)
                                .Select(course => course.EnrollmentMode).SingleAsync(cancellationToken);
                            enrollment.Status = mode == "ApprovalRequired" ? "PendingApproval" : "Active";
                        }
                        db.OutboxMessages.Add(new OutboxMessage
                        {
                            Type = "notification.delivery",
                            AggregateType = nameof(Payment),
                            AggregateId = payment.Id,
                            DeduplicationKey = $"payment-completed:{payment.Id}:{payment.LearnerId}",
                            PayloadJson = JsonSerializer.Serialize(new
                            {
                                LearnerId = payment.LearnerId,
                                Type = "payment_completed",
                                Title = "Payment confirmed",
                                Message = "Your course payment was verified.",
                                LinkUrl = $"/courses/{payment.CourseId}",
                                Preference = "Announcements"
                            }),
                            OccurredAt = now,
                            AvailableAt = now
                        });
                        db.ProductActivityEvents.Add(new ProductActivityEvent
                        {
                            EventType = "payment.completed",
                            ActorAccountId = payment.LearnerId,
                            CourseId = payment.CourseId,
                            ReleaseId = payment.ReleaseId,
                            MetadataJson = JsonSerializer.Serialize(new { payment.Amount, payment.Currency, payment.OfferId }),
                            OccurredAt = now
                        });
                    }
                }
                else if (verification.Status is "failed" or "cancelled")
                {
                    payment.Status = "Failed";
                    payment.FailureCode = "provider_declined";
                }
                else
                {
                    payment.NextVerificationAt = now.AddMinutes(Math.Min(60, Math.Pow(2, Math.Clamp(payment.VerificationAttemptCount, 0, 6))));
                }
                payment.LeaseOwner = null;
                payment.LeaseToken = null;
                payment.LeaseExpiresAt = null;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payment reconciliation failed for payment {PaymentId}.", lease.Id);
                await ReleaseAsync(lease, CancellationToken.None);
            }
        }

        private async Task ReleaseAsync(PaymentLease lease, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var payment = await db.Payments.SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.Id && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (payment == null) return;
            payment.LeaseOwner = null;
            payment.LeaseToken = null;
            payment.LeaseExpiresAt = null;
            payment.NextVerificationAt = UtcNow().AddMinutes(2);
            await db.SaveChangesAsync(cancellationToken);
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
        private sealed record PaymentLease(Guid Id, Guid Token);
    }
}
