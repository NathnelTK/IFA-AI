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
    public sealed class OutboxDispatcherBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WorkerIdentity _identity;
        private readonly WorkerOptions _options;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<OutboxDispatcherBackgroundService> _logger;

        public OutboxDispatcherBackgroundService(
            IServiceScopeFactory scopeFactory,
            WorkerIdentity identity,
            IOptions<WorkerOptions> options,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider,
            ILogger<OutboxDispatcherBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _identity = identity;
            _options = options.Value;
            _features = features.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Outbox.Enabled || !_features.DurableOutbox)
            {
                _logger.LogInformation("Outbox dispatcher is disabled.");
                return;
            }

            _logger.LogInformation("Outbox dispatcher {WorkerId} started.", _identity.Value);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var leases = await ClaimAsync(stoppingToken);
                    foreach (var lease in leases)
                    {
                        await DispatchAsync(lease, stoppingToken);
                    }
                    if (leases.Count > 0)
                    {
                        continue;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox polling failed; the dispatcher will retry.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Outbox.PollSeconds), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            _logger.LogInformation("Outbox dispatcher {WorkerId} stopped.", _identity.Value);
        }

        private async Task<List<OutboxLease>> ClaimAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = Guid.NewGuid();

            if (db.Database.IsNpgsql())
            {
                var messages = await db.OutboxMessages.FromSqlInterpolated($"""
                    WITH candidates AS (
                        SELECT "Id"
                        FROM "OutboxMessages"
                        WHERE "ProcessedAt" IS NULL
                          AND "DeadLetteredAt" IS NULL
                          AND "AvailableAt" <= CURRENT_TIMESTAMP
                          AND "AttemptCount" < "MaxAttempts"
                          AND ("LeaseExpiresAt" IS NULL OR "LeaseExpiresAt" < CURRENT_TIMESTAMP)
                        ORDER BY "AvailableAt", "OccurredAt", "Id"
                        FOR UPDATE SKIP LOCKED
                        LIMIT {_options.Outbox.BatchSize}
                    )
                    UPDATE "OutboxMessages" AS message
                    SET "AttemptCount" = message."AttemptCount" + 1,
                        "LeaseOwner" = {_identity.Value},
                        "LeaseToken" = {token},
                        "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Outbox.LeaseSeconds} * INTERVAL '1 second'),
                        "LastHeartbeatAt" = CURRENT_TIMESTAMP
                    FROM candidates
                    WHERE message."Id" = candidates."Id"
                    RETURNING message.*
                    """)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                return messages.Select(message => new OutboxLease(message.Id, token)).ToList();
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var now = UtcNow();
            var fallback = await db.OutboxMessages
                .Where(message => message.ProcessedAt == null
                    && message.DeadLetteredAt == null
                    && message.AvailableAt <= now
                    && message.AttemptCount < message.MaxAttempts
                    && (message.LeaseExpiresAt == null || message.LeaseExpiresAt < now))
                .OrderBy(message => message.AvailableAt)
                .ThenBy(message => message.OccurredAt)
                .Take(_options.Outbox.BatchSize)
                .ToListAsync(cancellationToken);
            foreach (var message in fallback)
            {
                message.AttemptCount++;
                message.LeaseOwner = _identity.Value;
                message.LeaseToken = token;
                message.LeaseExpiresAt = now.AddSeconds(_options.Outbox.LeaseSeconds);
                message.LastHeartbeatAt = now;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return fallback.Select(message => new OutboxLease(message.Id, token)).ToList();
        }

        private async Task DispatchAsync(OutboxLease lease, CancellationToken stoppingToken)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
                var message = await db.OutboxMessages.SingleOrDefaultAsync(
                    candidate => candidate.Id == lease.MessageId
                        && candidate.ProcessedAt == null
                        && candidate.DeadLetteredAt == null
                        && candidate.LeaseToken == lease.Token,
                    stoppingToken);
                if (message == null)
                {
                    return;
                }

                switch (message.Type)
                {
                    case ModuleGenerationBackgroundService.ProjectStatusOutboxType:
                        await MaterializeProjectNotificationAsync(db, message, stoppingToken);
                        break;
                    case "auth.email":
                        await DeliverAuthEmailAsync(scope.ServiceProvider, message, stoppingToken);
                        break;
                    case "workspace.invitation.email":
                        await DeliverWorkspaceInvitationAsync(scope.ServiceProvider, message, stoppingToken);
                        break;
                    case "course.release.published":
                        await MaterializeReleaseNotificationAsync(db, message, stoppingToken);
                        break;
                    case "course.announcement.published":
                        await ExpandAnnouncementAsync(db, message, stoppingToken);
                        break;
                    case "notification.delivery":
                        await MaterializeNotificationAsync(db, message, stoppingToken);
                        break;
                    default:
                        throw new UnsupportedOutboxMessageException(message.Type);
                }

                message.ProcessedAt = UtcNow();
                ClearLease(message);
                db.AuditEvents.Add(new AuditEvent
                {
                    ActorLearnerId = message.ActorLearnerId,
                    Action = "outbox.dispatched",
                    ResourceType = nameof(OutboxMessage),
                    ResourceId = message.Id.ToString(),
                    Outcome = "Succeeded",
                    CorrelationId = message.CorrelationId,
                    CausationId = message.CausationId,
                    OccurredAt = message.ProcessedAt.Value
                });
                await db.SaveChangesAsync(stoppingToken);
                await transaction.CommitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await ReleaseAsync(lease, decrementAttempt: true, errorCode: "worker_shutdown", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox message {MessageId} failed on worker {WorkerId}.", lease.MessageId, _identity.Value);
                await FailAsync(lease, ex, CancellationToken.None);
            }
        }

        private static async Task DeliverAuthEmailAsync(
            IServiceProvider services,
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<AuthEmailPayload>(message.PayloadJson)
                ?? throw new InvalidOperationException("The authentication email payload is invalid.");
            var protector = services.GetRequiredService<IApplicationSecretProtector>();
            var email = services.GetRequiredService<ITransactionalEmailService>();
            var options = services.GetRequiredService<IOptions<EmailOptions>>().Value;
            var token = protector.Unprotect(payload.ProtectedToken);
            var path = payload.Template == "verify-email" ? "/verify-email" : "/reset-password";
            var link = $"{options.AuthBaseUrl.TrimEnd('/')}{path}?token={Uri.EscapeDataString(token)}";
            var purpose = payload.Template == "verify-email" ? "verify your email" : "reset your password";
            await email.SendAsync(new TransactionalEmail(
                payload.Email,
                payload.Template == "verify-email" ? "Verify your Witforge email" : "Reset your Witforge password",
                $"Hello {payload.Name}, use this link to {purpose}: {link}\n\nThis link expires at {payload.ExpiresAt:O}.",
                $"<p>Hello {System.Net.WebUtility.HtmlEncode(payload.Name)},</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">{System.Net.WebUtility.HtmlEncode(purpose)}</a>.</p><p>This link expires at {payload.ExpiresAt:O}.</p>",
                message.DeduplicationKey),
                cancellationToken);
        }

        private static async Task DeliverWorkspaceInvitationAsync(
            IServiceProvider services,
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<WorkspaceInvitationEmailPayload>(message.PayloadJson)
                ?? throw new InvalidOperationException("The workspace invitation payload is invalid.");
            var protector = services.GetRequiredService<IApplicationSecretProtector>();
            var email = services.GetRequiredService<ITransactionalEmailService>();
            var options = services.GetRequiredService<IOptions<EmailOptions>>().Value;
            var token = protector.Unprotect(payload.ProtectedToken);
            var link = $"{options.AuthBaseUrl.TrimEnd('/')}/workspace-invitations/accept?token={Uri.EscapeDataString(token)}";
            await email.SendAsync(new TransactionalEmail(
                payload.Email,
                $"Join {payload.WorkspaceName} on Witforge",
                $"You were invited to join {payload.WorkspaceName}. Accept the invitation: {link}\n\nThis link expires at {payload.ExpiresAt:O}.",
                $"<p>You were invited to join <strong>{System.Net.WebUtility.HtmlEncode(payload.WorkspaceName)}</strong>.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Accept the invitation</a>.</p><p>This link expires at {payload.ExpiresAt:O}.</p>",
                message.DeduplicationKey),
                cancellationToken);
        }

        private static async Task MaterializeReleaseNotificationAsync(
            ApplicationDbContext db,
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<CourseReleasePublishedPayload>(message.PayloadJson)
                ?? throw new InvalidOperationException("The course release payload is invalid.");
            var ownerId = await db.Courses
                .Where(course => course.Id == payload.CourseId)
                .Select(course => course.CreatorLearnerId)
                .SingleOrDefaultAsync(cancellationToken);
            if (!ownerId.HasValue
                || await db.Notifications.AnyAsync(notification => notification.DedupeKey == message.DeduplicationKey, cancellationToken))
            {
                return;
            }
            db.Notifications.Add(new Notification
            {
                LearnerId = ownerId.Value,
                Type = "course_release_published",
                Title = "Course release published",
                Message = $"Release {payload.Version} is now the active learner version.",
                LinkUrl = "/studio/courses",
                DedupeKey = message.DeduplicationKey,
                CreatedAt = message.OccurredAt
            });
            var workspaceId = await db.Courses
                .Where(course => course.Id == payload.CourseId)
                .Select(course => course.WorkspaceId)
                .SingleOrDefaultAsync(cancellationToken);
            if (workspaceId.HasValue)
            {
                var followers = await db.WorkspaceFollows
                    .Where(follow => follow.WorkspaceId == workspaceId.Value)
                    .Select(follow => follow.LearnerId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                foreach (var followerId in followers)
                {
                    QueueDelivery(db, message, new NotificationDeliveryPayload(
                        followerId,
                        "followed_educator_release",
                        "A followed educator published a course release",
                        $"Release {payload.Version} is now available.",
                        $"/courses/{payload.CourseId}",
                        "FollowedEducatorUpdates"),
                        $"release-follower:{payload.ReleaseId}:{followerId}");
                }
            }
        }

        private static async Task ExpandAnnouncementAsync(
            ApplicationDbContext db,
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<AnnouncementPublishedPayload>(message.PayloadJson)
                ?? throw new InvalidOperationException("The announcement payload is invalid.");
            List<Guid> recipients;
            if (payload.Audience == "Followers")
            {
                var workspaceId = await db.Courses
                    .Where(course => course.Id == payload.CourseId)
                    .Select(course => course.WorkspaceId)
                    .SingleOrDefaultAsync(cancellationToken);
                recipients = workspaceId.HasValue
                    ? await db.WorkspaceFollows.Where(follow => follow.WorkspaceId == workspaceId.Value)
                        .Select(follow => follow.LearnerId).Distinct().ToListAsync(cancellationToken)
                    : [];
            }
            else
            {
                var query = db.CourseEnrollments.Where(enrollment =>
                    enrollment.CourseId == payload.CourseId && enrollment.Status == "Active");
                if (payload.Audience == "Release" && payload.ReleaseId.HasValue)
                    query = query.Where(enrollment => enrollment.ReleaseId == payload.ReleaseId);
                if (payload.Audience == "Cohort" && payload.CohortId.HasValue)
                    query = query.Where(enrollment => enrollment.CohortId == payload.CohortId);
                recipients = await query.Select(enrollment => enrollment.LearnerId).Distinct().ToListAsync(cancellationToken);
            }
            foreach (var learnerId in recipients)
            {
                QueueDelivery(db, message, new NotificationDeliveryPayload(
                    learnerId,
                    "course_announcement",
                    payload.Title,
                    "A course announcement was published.",
                    $"/courses/{payload.CourseId}",
                    payload.Audience == "Followers" ? "FollowedEducatorUpdates" : "Announcements"),
                    $"announcement:{payload.Id}:{learnerId}");
            }
        }

        private static async Task MaterializeNotificationAsync(
            ApplicationDbContext db,
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<NotificationDeliveryPayload>(message.PayloadJson)
                ?? throw new InvalidOperationException("The notification delivery payload is invalid.");
            var settings = await db.LearnerSettings.SingleOrDefaultAsync(
                candidate => candidate.LearnerId == payload.LearnerId,
                cancellationToken);
            var allowed = payload.Preference switch
            {
                "Announcements" => settings?.Announcements ?? true,
                "FollowedEducatorUpdates" => settings?.FollowedEducatorUpdates ?? true,
                "ReviewReplies" => settings?.ReviewReplies ?? true,
                _ => true
            };
            if (!allowed
                || await db.Notifications.AnyAsync(notification => notification.DedupeKey == message.DeduplicationKey, cancellationToken))
            {
                return;
            }
            db.Notifications.Add(new Notification
            {
                LearnerId = payload.LearnerId,
                Type = payload.Type,
                Title = payload.Title,
                Message = payload.Message,
                LinkUrl = payload.LinkUrl,
                DedupeKey = message.DeduplicationKey,
                CreatedAt = message.OccurredAt
            });
        }

        private static void QueueDelivery(
            ApplicationDbContext db,
            OutboxMessage source,
            NotificationDeliveryPayload payload,
            string deduplicationKey)
        {
            db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "notification.delivery",
                AggregateType = nameof(Notification),
                AggregateId = payload.LearnerId,
                DeduplicationKey = deduplicationKey,
                PayloadJson = JsonSerializer.Serialize(payload),
                OccurredAt = source.OccurredAt,
                AvailableAt = source.OccurredAt,
                CorrelationId = source.CorrelationId,
                CausationId = source.Id.ToString(),
                ActorLearnerId = source.ActorLearnerId
            });
        }

        private static async Task MaterializeProjectNotificationAsync(
            ApplicationDbContext db,
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<ModuleGenerationBackgroundService.ProjectStatusNotification>(
                message.PayloadJson) ?? throw new InvalidOperationException("The generation notification payload is invalid.");
            if (await db.Notifications.AnyAsync(notification => notification.DedupeKey == message.DeduplicationKey, cancellationToken))
            {
                return;
            }

            db.Notifications.Add(new Notification
            {
                LearnerId = payload.LearnerId,
                Type = payload.AllDone ? "course_generation_complete" : "course_generation_failed",
                Title = payload.AllDone
                    ? "Course generation complete"
                    : payload.AnyDone
                        ? "Some modules failed to generate"
                        : "Course generation failed",
                Message = payload.AllDone
                    ? $"All modules for '{payload.ProjectTitle}' were generated. Review the saved draft before publishing."
                    : $"Generation for '{payload.ProjectTitle}' could not finish. The draft and completed modules are saved.",
                LinkUrl = $"/studio/{payload.ProjectId}",
                DedupeKey = message.DeduplicationKey,
                CreatedAt = message.OccurredAt
            });
        }

        private async Task FailAsync(OutboxLease lease, Exception exception, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var message = await db.OutboxMessages.SingleOrDefaultAsync(
                candidate => candidate.Id == lease.MessageId
                    && candidate.ProcessedAt == null
                    && candidate.DeadLetteredAt == null
                    && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (message == null)
            {
                return;
            }

            var now = UtcNow();
            var permanent = exception is UnsupportedOutboxMessageException or JsonException or InvalidOperationException;
            var exhausted = message.AttemptCount >= message.MaxAttempts;
            message.LastErrorCode = permanent ? "outbox_invalid_message" : "outbox_dispatch_failed";
            message.LastError = exception.Message.Length <= 1000 ? exception.Message : exception.Message[..1000];
            message.AvailableAt = now.Add(RetryDelay(message.AttemptCount));
            message.DeadLetteredAt = permanent || exhausted ? now : null;
            ClearLease(message);
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task ReleaseAsync(
            OutboxLease lease,
            bool decrementAttempt,
            string errorCode,
            CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var message = await db.OutboxMessages.SingleOrDefaultAsync(
                candidate => candidate.Id == lease.MessageId && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (message == null)
            {
                return;
            }
            if (decrementAttempt)
            {
                message.AttemptCount = Math.Max(0, message.AttemptCount - 1);
            }
            message.AvailableAt = UtcNow();
            message.LastErrorCode = errorCode;
            ClearLease(message);
            await db.SaveChangesAsync(cancellationToken);
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static void ClearLease(OutboxMessage message)
        {
            message.LeaseOwner = null;
            message.LeaseToken = null;
            message.LeaseExpiresAt = null;
            message.LastHeartbeatAt = null;
        }

        private static TimeSpan RetryDelay(int attempt) =>
            TimeSpan.FromSeconds(Math.Min(900, Math.Pow(2, Math.Clamp(attempt, 1, 10)) * 5));

        private sealed record OutboxLease(Guid MessageId, Guid Token);

        private sealed record AuthEmailPayload(
            Guid AccountId,
            string Email,
            string Name,
            string Template,
            string ProtectedToken,
            DateTime ExpiresAt);

        private sealed record WorkspaceInvitationEmailPayload(
            Guid InvitationId,
            string Email,
            string WorkspaceName,
            string ProtectedToken,
            DateTime ExpiresAt);

        private sealed record CourseReleasePublishedPayload(Guid CourseId, Guid ReleaseId, int Version);
        private sealed record AnnouncementPublishedPayload(
            Guid Id,
            Guid CourseId,
            Guid? ReleaseId,
            Guid? CohortId,
            string Audience,
            string Title);
        private sealed record NotificationDeliveryPayload(
            Guid LearnerId,
            string Type,
            string Title,
            string Message,
            string LinkUrl,
            string Preference);

        private sealed class UnsupportedOutboxMessageException : Exception
        {
            public UnsupportedOutboxMessageException(string type)
                : base($"Outbox message type '{type}' is not supported by this worker.")
            {
            }
        }
    }
}
