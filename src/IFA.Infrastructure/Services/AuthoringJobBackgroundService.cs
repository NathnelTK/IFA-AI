using System.Text.Json;
using IFA.Application.Common.Exceptions;
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
    public sealed class AuthoringJobBackgroundService : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WorkerIdentity _identity;
        private readonly WorkerOptions _options;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AuthoringJobBackgroundService> _logger;

        public AuthoringJobBackgroundService(
            IServiceScopeFactory scopeFactory,
            WorkerIdentity identity,
            IOptions<WorkerOptions> options,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider,
            ILogger<AuthoringJobBackgroundService> logger)
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
            if (!_options.Authoring.Enabled || !_features.UnifiedAuthoring)
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
                    _logger.LogError(ex, "Authoring job polling failed.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Authoring.PollSeconds), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        internal async Task<List<AuthoringLease>> ClaimAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = Guid.NewGuid();
            if (db.Database.IsNpgsql())
            {
                var jobs = await db.AuthoringJobs.FromSqlInterpolated($"""
                    WITH candidates AS (
                        SELECT "Id"
                        FROM "AuthoringJobs"
                        WHERE ((
                            "State" = 'Queued' AND "AvailableAt" <= CURRENT_TIMESTAMP
                        ) OR (
                            "State" = 'Running' AND "LeaseExpiresAt" < CURRENT_TIMESTAMP
                        ))
                        AND "AttemptCount" < "MaxAttempts"
                        ORDER BY "AvailableAt", "CreatedAt", "Id"
                        FOR UPDATE SKIP LOCKED
                        LIMIT {_options.Authoring.MaxInFlight}
                    )
                    UPDATE "AuthoringJobs" AS job
                    SET "State" = 'Running',
                        "AttemptCount" = job."AttemptCount" + 1,
                        "LeaseOwner" = {_identity.Value},
                        "LeaseToken" = {token},
                        "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Authoring.LeaseSeconds} * INTERVAL '1 second'),
                        "UpdatedAt" = CURRENT_TIMESTAMP,
                        "ErrorCode" = NULL,
                        "ErrorDetail" = NULL
                    FROM candidates
                    WHERE job."Id" = candidates."Id"
                    RETURNING job.*
                    """)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                return jobs.Select(job => new AuthoringLease(job.Id, token)).ToList();
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var now = UtcNow();
            var fallback = await db.AuthoringJobs
                .Where(job => (job.State == AuthoringJobState.Queued && job.AvailableAt <= now)
                    || (job.State == AuthoringJobState.Running && job.LeaseExpiresAt < now))
                .Where(job => job.AttemptCount < job.MaxAttempts)
                .OrderBy(job => job.AvailableAt)
                .Take(_options.Authoring.MaxInFlight)
                .ToListAsync(cancellationToken);
            foreach (var job in fallback)
            {
                job.State = AuthoringJobState.Running;
                job.AttemptCount++;
                job.LeaseOwner = _identity.Value;
                job.LeaseToken = token;
                job.LeaseExpiresAt = now.AddSeconds(_options.Authoring.LeaseSeconds);
                job.UpdatedAt = now;
                job.ErrorCode = null;
                job.ErrorDetail = null;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return fallback.Select(job => new AuthoringLease(job.Id, token)).ToList();
        }

        private async Task ProcessAsync(AuthoringLease lease, CancellationToken stoppingToken)
        {
            using var execution = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var heartbeat = HeartbeatAsync(lease, execution);
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var job = await db.AuthoringJobs.SingleOrDefaultAsync(candidate =>
                    candidate.Id == lease.JobId
                    && candidate.State == AuthoringJobState.Running
                    && candidate.LeaseToken == lease.Token,
                    execution.Token) ?? throw new InvalidOperationException("The authoring lease is no longer owned by this worker.");

                switch (job.Kind)
                {
                    case "GenerateCourseDraft":
                        await GenerateCourseDraftAsync(scope.ServiceProvider, db, job, lease, execution.Token);
                        break;
                    case "AnalyzeSources":
                        await AnalyzeSourcesAsync(scope.ServiceProvider, db, job, lease, execution.Token);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported authoring job kind '{job.Kind}'.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await ReleaseAsync(lease, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authoring job {JobId} failed.", lease.JobId);
                await FailAsync(lease, ex, CancellationToken.None);
            }
            finally
            {
                execution.Cancel();
                try { await heartbeat; } catch (OperationCanceledException) { }
            }
        }

        private async Task GenerateCourseDraftAsync(
            IServiceProvider services,
            ApplicationDbContext db,
            AuthoringJob job,
            AuthoringLease lease,
            CancellationToken cancellationToken)
        {
            var project = await db.AuthoringProjects.SingleAsync(candidate => candidate.Id == job.AuthoringProjectId, cancellationToken);
            var request = await db.CourseGenerationRequests.SingleAsync(candidate => candidate.AuthoringProjectId == project.Id, cancellationToken);
            var course = await db.Courses.Include(candidate => candidate.Modules)
                .SingleAsync(candidate => candidate.Id == project.CourseId, cancellationToken);
            var objectives = JsonSerializer.Deserialize<List<string>>(request.LearningObjectivesJson, JsonOptions) ?? [];
            var generation = services.GetRequiredService<ICourseGenerationService>();
            var proposal = await generation.GenerateCoursePipelineProposalAsync(new CourseGenerationPrompt
            {
                Goal = request.Prompt,
                Subject = course.Title,
                CurrentLevel = course.Level,
                WeeklyStudyHours = 5,
                Category = request.Category,
                TargetAudience = request.TargetAudience,
                LearningObjectives = objectives
            }, researchPackage: null, cancellationToken, ParseMaterials(course.ExternalMaterials));

            await db.Entry(job).ReloadAsync(cancellationToken);
            if (job.LeaseToken != lease.Token || job.State != AuthoringJobState.Running)
            {
                throw new InvalidOperationException("The authoring lease expired before the draft could be saved.");
            }
            if (course.Modules.Count > 0)
            {
                throw new InvalidOperationException("The course outline changed while generation was running.");
            }

            course.Title = proposal.CourseTitle.Trim();
            course.Description = proposal.TargetGoal.Trim();
            course.Category = request.Category;
            course.TargetAudience = request.TargetAudience;
            course.LearningObjectives = objectives;
            course.UpdatedByAccountId = job.ActorAccountId;
            course.UpdatedAt = UtcNow();
            foreach (var moduleProposal in proposal.Modules.OrderBy(module => module.ModuleNumber))
            {
                course.Modules.Add(new Module
                {
                    CourseId = course.Id,
                    ModuleNumber = moduleProposal.ModuleNumber,
                    Title = moduleProposal.Title.Trim(),
                    Summary = moduleProposal.Summary.Trim(),
                    EstimatedHours = moduleProposal.EstimatedHours,
                    GenerationStatus = ModuleGenerationStatus.Blueprint
                });
            }
            await MarkSucceededAsync(db, project, job, AuthoringProjectState.AwaitingApproval, cancellationToken);
        }

        private async Task AnalyzeSourcesAsync(
            IServiceProvider services,
            ApplicationDbContext db,
            AuthoringJob job,
            AuthoringLease lease,
            CancellationToken cancellationToken)
        {
            var project = await db.AuthoringProjects.SingleAsync(candidate => candidate.Id == job.AuthoringProjectId, cancellationToken);
            var legacyProjectId = project.LegacyEducatorProjectId ?? project.Id;
            var legacy = await db.EducatorProjects
                .Include(candidate => candidate.Sources)
                .Include(candidate => candidate.Blueprint)
                .SingleAsync(candidate => candidate.Id == legacyProjectId, cancellationToken);
            var ingestion = services.GetRequiredService<ISourceIngestionService>();
            foreach (var source in legacy.Sources.OrderBy(source => source.CreatedAt))
            {
                await ingestion.IngestAsync(source, cancellationToken);
            }
            await db.SaveChangesAsync(cancellationToken);
            var analysis = services.GetRequiredService<ISourceAnalysisService>();
            await analysis.AnalyzeAsync(legacy, cancellationToken);
            var blueprintService = services.GetRequiredService<IBlueprintService>();
            var blueprint = await blueprintService.GenerateAsync(legacy, cancellationToken);
            var document = BlueprintService.Parse(blueprint.Json, legacy.Sources)
                ?? throw new InvalidOperationException("The generated source blueprint could not be read.");

            await db.Entry(job).ReloadAsync(cancellationToken);
            if (job.LeaseToken != lease.Token || job.State != AuthoringJobState.Running)
            {
                throw new InvalidOperationException("The authoring lease expired before the source blueprint could be saved.");
            }
            var course = await db.Courses.Include(candidate => candidate.Modules)
                .SingleAsync(candidate => candidate.Id == project.CourseId, cancellationToken);
            if (course.Modules.Count > 0)
            {
                throw new InvalidOperationException("The course outline changed while source analysis was running.");
            }
            course.Title = document.Title;
            course.Description = document.Description;
            course.Category = document.Category;
            course.UpdatedAt = UtcNow();
            foreach (var (moduleDocument, index) in document.Modules.Select((value, index) => (value, index)))
            {
                course.Modules.Add(new Module
                {
                    CourseId = course.Id,
                    ModuleNumber = index + 1,
                    Title = moduleDocument.Title,
                    Summary = moduleDocument.Description,
                    EstimatedHours = moduleDocument.EstimatedHours,
                    GenerationStatus = ModuleGenerationStatus.Blueprint
                });
            }
            var authoringSources = await db.AuthoringSources
                .Where(source => source.AuthoringProjectId == project.Id)
                .ToListAsync(cancellationToken);
            foreach (var source in authoringSources)
            {
                var legacySource = legacy.Sources.Single(candidate => candidate.Id == source.LegacyProjectSourceId);
                source.Status = legacySource.Status;
                source.ErrorCode = legacySource.FailureReason == null ? null : "source_extraction_failed";
                source.UpdatedAt = UtcNow();
            }
            await MarkSucceededAsync(db, project, job, AuthoringProjectState.AwaitingApproval, cancellationToken);
        }

        private async Task MarkSucceededAsync(
            ApplicationDbContext db,
            AuthoringProject project,
            AuthoringJob job,
            AuthoringProjectState state,
            CancellationToken cancellationToken)
        {
            var now = UtcNow();
            job.State = AuthoringJobState.Succeeded;
            job.ProgressPercent = 100;
            job.ResultJson = JsonSerializer.Serialize(new { projectId = project.Id, courseId = project.CourseId }, JsonOptions);
            job.CompletedAt = now;
            job.UpdatedAt = now;
            ClearLease(job);
            project.State = state;
            project.FailureCode = null;
            project.FailureDetail = null;
            project.UpdatedAt = now;
            project.Version++;
            if (project.LegacyEducatorProjectId.HasValue)
            {
                var legacy = await db.EducatorProjects.SingleAsync(
                    candidate => candidate.Id == project.LegacyEducatorProjectId.Value,
                    cancellationToken);
                legacy.Status = state == AuthoringProjectState.AwaitingApproval ? "BlueprintReady" : "ReadyForReview";
                legacy.UpdatedAt = now;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task HeartbeatAsync(AuthoringLease lease, CancellationTokenSource execution)
        {
            while (!execution.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Authoring.HeartbeatSeconds), _timeProvider, execution.Token);
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
                        UPDATE "AuthoringJobs"
                        SET "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Authoring.LeaseSeconds} * INTERVAL '1 second'),
                            "UpdatedAt" = CURRENT_TIMESTAMP
                        WHERE "Id" = {lease.JobId}
                          AND "State" = 'Running'
                          AND "LeaseToken" = {lease.Token}
                        """, execution.Token);
                    if (updated == 0) execution.Cancel();
                }
                catch (OperationCanceledException) when (execution.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Authoring heartbeat failed for {JobId}.", lease.JobId);
                    execution.Cancel();
                }
            }
        }

        private async Task FailAsync(AuthoringLease lease, Exception exception, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var job = await db.AuthoringJobs.SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.JobId
                && candidate.State == AuthoringJobState.Running
                && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (job == null) return;
            var permanent = exception is InvalidOperationException && exception is not AiUnavailableException;
            var terminal = permanent || job.AttemptCount >= job.MaxAttempts;
            var now = UtcNow();
            job.State = terminal ? AuthoringJobState.Failed : AuthoringJobState.Queued;
            job.AvailableAt = terminal ? now : now.AddSeconds(Math.Min(300, Math.Pow(2, job.AttemptCount) * 5));
            job.ErrorCode = permanent ? "authoring_invalid_state" : terminal ? "authoring_attempts_exhausted" : "authoring_transient_failure";
            job.ErrorDetail = permanent ? Truncate(exception.Message, 1000) : "The provider operation failed and may be retried.";
            job.CompletedAt = terminal ? now : null;
            job.UpdatedAt = now;
            ClearLease(job);
            var project = await db.AuthoringProjects.SingleAsync(candidate => candidate.Id == job.AuthoringProjectId, cancellationToken);
            if (terminal)
            {
                project.State = AuthoringProjectState.Failed;
                project.FailureCode = job.ErrorCode;
                project.FailureDetail = job.ErrorDetail;
                project.UpdatedAt = now;
                project.Version++;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task ReleaseAsync(AuthoringLease lease, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var job = await db.AuthoringJobs.SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.JobId && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (job == null) return;
            job.State = AuthoringJobState.Queued;
            job.AttemptCount = Math.Max(0, job.AttemptCount - 1);
            job.AvailableAt = UtcNow();
            job.UpdatedAt = UtcNow();
            ClearLease(job);
            await db.SaveChangesAsync(cancellationToken);
        }

        private static void ClearLease(AuthoringJob job)
        {
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static IReadOnlyList<string> ParseMaterials(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

        internal sealed record AuthoringLease(Guid JobId, Guid Token);
    }
}
