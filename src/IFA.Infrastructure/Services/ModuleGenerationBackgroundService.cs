using System.Text.Json;
using IFA.Application.Common.Exceptions;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class ModuleGenerationBackgroundService : BackgroundService
    {
        internal const string ProjectStatusOutboxType = "generation.project.status";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WorkerIdentity _identity;
        private readonly WorkerOptions _options;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<ModuleGenerationBackgroundService> _logger;

        public ModuleGenerationBackgroundService(
            IServiceScopeFactory scopeFactory,
            WorkerIdentity identity,
            IOptions<WorkerOptions> options,
            IOptions<FeatureOptions> features,
            TimeProvider timeProvider,
            ILogger<ModuleGenerationBackgroundService> logger)
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
            if (!_options.Generation.Enabled || !_features.CourseGeneration)
            {
                _logger.LogInformation("Module generation worker is disabled.");
                return;
            }

            _logger.LogInformation("Module generation worker {WorkerId} started.", _identity.Value);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var leases = await ClaimAsync(stoppingToken);
                    if (leases.Count > 0)
                    {
                        await Task.WhenAll(leases.Select(lease => RunJobAsync(lease, stoppingToken)));
                        continue;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Module generation polling failed; the worker will retry.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Generation.PollSeconds), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Module generation worker {WorkerId} stopped.", _identity.Value);
        }

        internal async Task<List<JobLease>> ClaimAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = Guid.NewGuid();

            if (db.Database.IsNpgsql())
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE "GenerationJobs"
                    SET "Status" = {GenerationJob.FailedStatus},
                        "ErrorCode" = 'generation_attempts_exhausted',
                        "Error" = 'Module generation exhausted its retry allowance.',
                        "CompletedAt" = CURRENT_TIMESTAMP,
                        "LeaseOwner" = NULL,
                        "LeaseToken" = NULL,
                        "LeaseExpiresAt" = NULL,
                        "LastHeartbeatAt" = NULL
                    WHERE "Status" = {GenerationJob.RunningStatus}
                      AND "LeaseExpiresAt" < CURRENT_TIMESTAMP
                      AND "AttemptCount" >= "MaxAttempts"
                    """, cancellationToken);

                var jobs = await db.GenerationJobs.FromSqlInterpolated($"""
                    WITH candidates AS (
                        SELECT "Id"
                        FROM "GenerationJobs"
                        WHERE (
                            "Status" = {GenerationJob.QueuedStatus}
                            AND "AvailableAt" <= CURRENT_TIMESTAMP
                            AND "AttemptCount" < "MaxAttempts"
                        ) OR (
                            "Status" = {GenerationJob.RunningStatus}
                            AND "LeaseExpiresAt" < CURRENT_TIMESTAMP
                            AND "AttemptCount" < "MaxAttempts"
                        )
                        ORDER BY "AvailableAt", "CreatedAt", "Id"
                        FOR UPDATE SKIP LOCKED
                        LIMIT {_options.Generation.MaxInFlight}
                    )
                    UPDATE "GenerationJobs" AS job
                    SET "Status" = {GenerationJob.RunningStatus},
                        "AttemptCount" = job."AttemptCount" + 1,
                        "StartedAt" = CURRENT_TIMESTAMP,
                        "LeaseOwner" = {_identity.Value},
                        "LeaseToken" = {token},
                        "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Generation.LeaseSeconds} * INTERVAL '1 second'),
                        "LastHeartbeatAt" = CURRENT_TIMESTAMP,
                        "Error" = NULL,
                        "ErrorCode" = NULL,
                        "CompletedAt" = NULL
                    FROM candidates
                    WHERE job."Id" = candidates."Id"
                    RETURNING job.*
                    """)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                return jobs.Select(job => new JobLease(job.Id, token)).ToList();
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var now = UtcNow();
            var jobsFallback = await db.GenerationJobs
                .Where(job =>
                    (job.Status == GenerationJob.QueuedStatus && job.AvailableAt <= now)
                    || (job.Status == GenerationJob.RunningStatus && job.LeaseExpiresAt < now))
                .Where(job => job.AttemptCount < job.MaxAttempts)
                .OrderBy(job => job.AvailableAt)
                .ThenBy(job => job.CreatedAt)
                .Take(_options.Generation.MaxInFlight)
                .ToListAsync(cancellationToken);
            foreach (var job in jobsFallback)
            {
                job.Status = GenerationJob.RunningStatus;
                job.AttemptCount++;
                job.StartedAt = now;
                job.LeaseOwner = _identity.Value;
                job.LeaseToken = token;
                job.LeaseExpiresAt = now.AddSeconds(_options.Generation.LeaseSeconds);
                job.LastHeartbeatAt = now;
                job.Error = null;
                job.ErrorCode = null;
                job.CompletedAt = null;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return jobsFallback.Select(job => new JobLease(job.Id, token)).ToList();
        }

        private async Task RunJobAsync(JobLease lease, CancellationToken stoppingToken)
        {
            Guid? moduleId = null;
            using var execution = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var heartbeat = HeartbeatAsync(lease, execution);

            try
            {
                var prepared = await PrepareAsync(lease, execution.Token);
                moduleId = prepared.ModuleId;

                await using var scope = _scopeFactory.CreateAsyncScope();
                var generation = scope.ServiceProvider.GetRequiredService<ICourseGenerationService>();
                var result = await generation.GenerateJitModuleAsync(prepared.Request, execution.Token);
                execution.Token.ThrowIfCancellationRequested();

                await CompleteAsync(lease, prepared, result, execution.Token);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await ReleaseForShutdownAsync(lease, moduleId);
            }
            catch (OperationCanceledException)
            {
                await RecordFailureAsync(
                    lease,
                    moduleId,
                    new TimeoutException("The generation lease heartbeat could not be maintained."),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generation job {JobId} failed on worker {WorkerId}.", lease.JobId, _identity.Value);
                await RecordFailureAsync(lease, moduleId, ex, CancellationToken.None);
            }
            finally
            {
                execution.Cancel();
                try
                {
                    await heartbeat;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task<PreparedJob> PrepareAsync(JobLease lease, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var job = await db.GenerationJobs.SingleOrDefaultAsync(
                candidate => candidate.Id == lease.JobId
                    && candidate.Status == GenerationJob.RunningStatus
                    && candidate.LeaseToken == lease.Token,
                cancellationToken) ?? throw new LeaseLostException();

            var project = await db.EducatorProjects
                .Include(candidate => candidate.Sources)
                .Include(candidate => candidate.Blueprint)
                .SingleOrDefaultAsync(candidate => candidate.Id == job.ProjectId, cancellationToken);
            var course = await db.Courses
                .Include(candidate => candidate.Modules)
                .ThenInclude(module => module.Lessons)
                .Include(candidate => candidate.Modules)
                .ThenInclude(module => module.Quizzes)
                .SingleOrDefaultAsync(candidate => candidate.Id == job.CourseId, cancellationToken);

            if (project == null
                || course == null
                || project.CourseId != course.Id
                || course.IsPublished)
            {
                throw new InvalidOperationException("The generation project must still refer to this unpublished course.");
            }

            if (job.Kind == GenerationJob.JitModuleKind)
            {
                return await PrepareJitModuleAsync(
                    scope.ServiceProvider,
                    db,
                    job,
                    project,
                    course,
                    cancellationToken);
            }
            if (job.Kind != GenerationJob.BlueprintModuleKind || project.Blueprint?.ApprovedAt == null)
            {
                throw new InvalidOperationException("The generation job kind or approved blueprint is invalid.");
            }

            var blueprint = BlueprintService.Parse(project.Blueprint.Json, project.Sources);
            if (blueprint == null || job.ModuleIndex < 0 || job.ModuleIndex >= blueprint.Modules.Count)
            {
                throw new InvalidOperationException("The approved blueprint or its extracted sources are no longer valid.");
            }

            var blueprintModule = blueprint.Modules[job.ModuleIndex];
            var module = course.Modules.SingleOrDefault(candidate =>
                candidate.ModuleNumber == job.ModuleIndex + 1 && candidate.Title == job.ModuleTitle);
            if (module == null || blueprintModule.Title != job.ModuleTitle)
            {
                throw new InvalidOperationException("The module no longer matches its approved generation job.");
            }
            if (module.Lessons.Any() || module.Quizzes.Any())
            {
                throw new InvalidOperationException("This module already has content; automatic append was refused.");
            }

            var moduleUrls = blueprintModule.Sources.Select(source => source.Url).ToHashSet(StringComparer.Ordinal);
            var includedSources = SourceAnalysisService.GetAcceptedSources(project.Sources)
                .Where(source => moduleUrls.Contains(source.Url))
                .ToList();

            module.GenerationStatus = ModuleGenerationStatus.Generating;
            await db.SaveChangesAsync(cancellationToken);

            return new PreparedJob(
                project.Id,
                module.Id,
                new JitModuleGenerationRequest
                {
                    CourseTitle = course.Title,
                    ModuleNumber = module.ModuleNumber,
                    ModuleTitle = module.Title,
                    TargetGoal = blueprintModule.Description,
                    KeyTopics = blueprintModule.Chapters
                        .Select(chapter => chapter.Title)
                        .Where(title => !string.IsNullOrWhiteSpace(title))
                        .ToList(),
                    ResearchContext = SourceAnalysisService.BuildDigest(includedSources),
                    ExternalMaterials = includedSources
                        .Select(source => source.Url)
                        .Distinct(StringComparer.Ordinal)
                        .ToList()
                });
        }

        private async Task<PreparedJob> PrepareJitModuleAsync(
            IServiceProvider services,
            ApplicationDbContext db,
            GenerationJob job,
            EducatorProject project,
            Course course,
            CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<QueuedModuleGenerationPayload>(job.RequestJson ?? string.Empty)
                ?? throw new InvalidOperationException("The queued module generation request is invalid.");
            var authorization = services.GetRequiredService<IPlatformAuthorizationService>();
            if (job.ActorLearnerId != payload.LearnerId
                || !await authorization.CanManageCourseAsync(payload.LearnerId, course.Id, cancellationToken))
            {
                throw new InvalidOperationException("The queued generation actor no longer owns this course.");
            }

            var module = course.Modules.SingleOrDefault(candidate =>
                candidate.Id == payload.ModuleId
                && candidate.ModuleNumber == job.ModuleIndex + 1
                && candidate.Title == job.ModuleTitle);
            if (module == null)
            {
                throw new InvalidOperationException("The queued module no longer matches this course.");
            }
            if (module.Lessons.Any() || module.Quizzes.Any())
            {
                throw new InvalidOperationException("This module already has content; automatic append was refused.");
            }

            module.GenerationStatus = ModuleGenerationStatus.Generating;
            await db.SaveChangesAsync(cancellationToken);

            var researchService = services.GetRequiredService<IResearchService>();
            var adaptiveEngine = services.GetRequiredService<IAdaptiveEngine>();
            var research = await researchService.GetResearchForCourseAsync(course.Id, cancellationToken);
            if (research == null)
            {
                try
                {
                    research = await researchService.ConductResearchAsync(
                        course.Title,
                        payload.LearnerId,
                        course.Id,
                        cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not AiUnavailableException)
                {
                    _logger.LogWarning(ex, "Research for course {CourseId} failed; generating from the saved module summary.", course.Id);
                }
            }

            var contextParts = new List<string>();
            foreach (var source in research?.Sources ?? [])
            {
                contextParts.Add(source.SourceType switch
                {
                    "Academic" => $"[PAPER] Title: {source.Title}\nAuthors: {source.Authors}\nYear: {source.PublishedYear?.ToString() ?? "unknown"}\nURL: {source.Url}\nAbstract: {source.Snippet}",
                    "Video" => $"[VIDEO] Title: {source.Title}\nChannel: {source.Authors}\nURL: {source.Url}",
                    "Graph" => $"[GRAPH] Title: {source.Title}\nURL: {source.Url}",
                    "Image" => $"[IMAGE] Title: {source.Title}\nURL: {source.Url}",
                    _ => $"[SOURCE] Title: {source.Title}\nURL: {source.Url}\nSummary: {source.Snippet}"
                });
            }

            var weakAreas = await adaptiveEngine.GetAdaptiveConstraintsForNextModuleAsync(payload.LearnerId, course.Id);
            return new PreparedJob(project.Id, module.Id, new JitModuleGenerationRequest
            {
                CourseTitle = course.Title,
                ModuleNumber = module.ModuleNumber,
                ModuleTitle = module.Title,
                TargetGoal = module.Summary,
                PriorQuizWeakAreas = weakAreas,
                ResearchContext = contextParts.Count > 0
                    ? string.Join("\n\n", contextParts)
                    : $"No external sources were available. Build these lessons from the module summary: {module.Summary}",
                ExternalMaterials = ParseExternalMaterials(course.ExternalMaterials)
            });
        }

        private static List<string> ParseExternalMaterials(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(item => item.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

        private async Task CompleteAsync(
            JobLease lease,
            PreparedJob prepared,
            GeneratedModuleResult generated,
            CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var job = await db.GenerationJobs.SingleOrDefaultAsync(
                candidate => candidate.Id == lease.JobId
                    && candidate.Status == GenerationJob.RunningStatus
                    && candidate.LeaseToken == lease.Token,
                cancellationToken) ?? throw new LeaseLostException();
            var module = await db.Modules
                .Include(candidate => candidate.Lessons)
                .Include(candidate => candidate.Quizzes)
                .SingleAsync(candidate => candidate.Id == prepared.ModuleId, cancellationToken);
            if (module.Lessons.Any() || module.Quizzes.Any())
            {
                throw new InvalidOperationException("The target module changed while generation was running.");
            }

            foreach (var lesson in generated.Lessons)
            {
                lesson.Module = null;
                lesson.ModuleId = module.Id;
                db.Add(lesson);
            }
            foreach (var quiz in generated.Quizzes)
            {
                quiz.Module = null;
                quiz.ModuleId = module.Id;
                foreach (var question in quiz.Questions)
                {
                    question.Quiz = null;
                }
                db.Add(quiz);
            }

            var now = UtcNow();
            module.GenerationStatus = ModuleGenerationStatus.Ready;
            module.GeneratedAt = now;
            job.Status = GenerationJob.DoneStatus;
            job.Error = null;
            job.ErrorCode = null;
            job.CompletedAt = now;
            ClearLease(job);
            db.AuditEvents.Add(new AuditEvent
            {
                ActorLearnerId = job.ActorLearnerId,
                Action = "generation.module.completed",
                ResourceType = nameof(GenerationJob),
                ResourceId = job.Id.ToString(),
                Outcome = "Succeeded",
                CorrelationId = job.CorrelationId,
                CausationId = job.CausationId,
                OccurredAt = now
            });
            await db.SaveChangesAsync(cancellationToken);

            await UpdateAuthoringParentAsync(db, job, now, cancellationToken);
            await FinalizeProjectAsync(db, prepared.ProjectId, job, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private async Task FinalizeProjectAsync(
            ApplicationDbContext db,
            Guid projectId,
            GenerationJob completedJob,
            DateTime now,
            CancellationToken cancellationToken)
        {
            EducatorProject? project;
            if (db.Database.IsNpgsql())
            {
                project = await db.EducatorProjects
                    .FromSqlInterpolated($"SELECT * FROM \"EducatorProjects\" WHERE \"Id\" = {projectId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
            }
            else
            {
                project = await db.EducatorProjects.SingleOrDefaultAsync(candidate => candidate.Id == projectId, cancellationToken);
            }
            if (project == null)
            {
                return;
            }

            var statuses = await db.GenerationJobs
                .Where(candidate => candidate.ProjectId == projectId
                    && (!completedJob.AuthoringJobId.HasValue || candidate.AuthoringJobId == completedJob.AuthoringJobId))
                .Select(candidate => candidate.Status)
                .ToListAsync(cancellationToken);
            if (statuses.Count == 0 || statuses.Any(status => status is GenerationJob.QueuedStatus or GenerationJob.RunningStatus))
            {
                return;
            }

            var allDone = statuses.All(status => status == GenerationJob.DoneStatus);
            var anyDone = statuses.Any(status => status == GenerationJob.DoneStatus);
            var nextStatus = allDone ? "ReadyForReview" : "GenerationFailed";
            if (project.Status == nextStatus)
            {
                return;
            }

            project.Status = nextStatus;
            project.UpdatedAt = now;
            var dedupeKey = $"generation-project:{project.Id}:{nextStatus}";
            if (!await db.OutboxMessages.AnyAsync(message => message.DeduplicationKey == dedupeKey, cancellationToken))
            {
                db.OutboxMessages.Add(new OutboxMessage
                {
                    Type = ProjectStatusOutboxType,
                    AggregateType = nameof(EducatorProject),
                    AggregateId = project.Id,
                    DeduplicationKey = dedupeKey,
                    PayloadJson = JsonSerializer.Serialize(new ProjectStatusNotification(
                        project.EducatorLearnerId,
                        project.Id,
                        project.Title,
                        allDone,
                        anyDone)),
                    OccurredAt = now,
                    AvailableAt = now,
                    CorrelationId = completedJob.CorrelationId,
                    CausationId = completedJob.Id.ToString(),
                    ActorLearnerId = completedJob.ActorLearnerId
                });
            }

            db.AuditEvents.Add(new AuditEvent
            {
                ActorLearnerId = completedJob.ActorLearnerId,
                Action = "generation.project.finalized",
                ResourceType = nameof(EducatorProject),
                ResourceId = project.Id.ToString(),
                Outcome = nextStatus,
                CorrelationId = completedJob.CorrelationId,
                CausationId = completedJob.Id.ToString(),
                OccurredAt = now
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task HeartbeatAsync(JobLease lease, CancellationTokenSource execution)
        {
            while (!execution.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.Generation.HeartbeatSeconds),
                        _timeProvider,
                        execution.Token);
                    if (!await RenewLeaseAsync(lease, execution.Token))
                    {
                        _logger.LogWarning("Generation job {JobId} lost its lease; cancelling provider work.", lease.JobId);
                        execution.Cancel();
                    }
                }
                catch (OperationCanceledException) when (execution.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Heartbeat failed for generation job {JobId}; cancelling provider work.", lease.JobId);
                    execution.Cancel();
                }
            }
        }

        internal async Task<bool> RenewLeaseAsync(JobLease lease, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            int updated;
            if (db.Database.IsNpgsql())
            {
                updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE "GenerationJobs"
                    SET "LastHeartbeatAt" = CURRENT_TIMESTAMP,
                        "LeaseExpiresAt" = CURRENT_TIMESTAMP + ({_options.Generation.LeaseSeconds} * INTERVAL '1 second')
                    WHERE "Id" = {lease.JobId}
                      AND "Status" = {GenerationJob.RunningStatus}
                      AND "LeaseToken" = {lease.Token}
                    """, cancellationToken);
            }
            else
            {
                var now = UtcNow();
                updated = await db.GenerationJobs
                    .Where(job => job.Id == lease.JobId
                        && job.Status == GenerationJob.RunningStatus
                        && job.LeaseToken == lease.Token)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(job => job.LastHeartbeatAt, now)
                        .SetProperty(job => job.LeaseExpiresAt, now.AddSeconds(_options.Generation.LeaseSeconds)),
                        cancellationToken);
            }
            return updated == 1;
        }

        private async Task RecordFailureAsync(
            JobLease lease,
            Guid? moduleId,
            Exception exception,
            CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var job = await db.GenerationJobs.SingleOrDefaultAsync(
                candidate => candidate.Id == lease.JobId
                    && candidate.Status == GenerationJob.RunningStatus
                    && candidate.LeaseToken == lease.Token,
                cancellationToken);
            if (job == null)
            {
                return;
            }

            var permanent = exception is InvalidOperationException or LeaseLostException;
            var exhausted = job.AttemptCount >= Math.Min(job.MaxAttempts, _options.Generation.MaxAttempts);
            var terminal = permanent || exhausted;
            var now = UtcNow();
            job.Status = terminal ? GenerationJob.FailedStatus : GenerationJob.QueuedStatus;
            job.ErrorCode = exception switch
            {
                LeaseLostException => "generation_lease_lost",
                InvalidOperationException => "generation_invalid_state",
                _ when exhausted => "generation_attempts_exhausted",
                _ => "generation_transient_failure"
            };
            job.Error = exception is InvalidOperationException
                ? Truncate(exception.Message, 500)
                : terminal
                    ? "Module generation failed after bounded retries."
                    : "A transient generation failure will be retried.";
            job.AvailableAt = terminal ? now : now.Add(RetryDelay(job.AttemptCount));
            job.CompletedAt = terminal ? now : null;
            ClearLease(job);

            if (moduleId.HasValue)
            {
                var module = await db.Modules.SingleOrDefaultAsync(candidate => candidate.Id == moduleId.Value, cancellationToken);
                if (module?.GenerationStatus == ModuleGenerationStatus.Generating)
                {
                    module.GenerationStatus = terminal ? ModuleGenerationStatus.Failed : ModuleGenerationStatus.Blueprint;
                    module.GeneratedAt = null;
                }
            }

            db.AuditEvents.Add(new AuditEvent
            {
                ActorLearnerId = job.ActorLearnerId,
                Action = terminal ? "generation.module.failed" : "generation.module.retry_scheduled",
                ResourceType = nameof(GenerationJob),
                ResourceId = job.Id.ToString(),
                Outcome = terminal ? "Failed" : "Retrying",
                ErrorCode = job.ErrorCode,
                CorrelationId = job.CorrelationId,
                CausationId = job.CausationId,
                OccurredAt = now
            });
            await db.SaveChangesAsync(cancellationToken);
            if (terminal)
            {
                await UpdateAuthoringParentAsync(db, job, now, cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }

        private static async Task UpdateAuthoringParentAsync(
            ApplicationDbContext db,
            GenerationJob child,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (!child.AuthoringJobId.HasValue)
            {
                return;
            }
            var parent = await db.AuthoringJobs.SingleOrDefaultAsync(
                candidate => candidate.Id == child.AuthoringJobId.Value,
                cancellationToken);
            if (parent == null)
            {
                return;
            }
            var statuses = await db.GenerationJobs
                .Where(candidate => candidate.AuthoringJobId == parent.Id)
                .Select(candidate => candidate.Status)
                .ToListAsync(cancellationToken);
            if (statuses.Count == 0)
            {
                return;
            }

            var terminalCount = statuses.Count(status => status is GenerationJob.DoneStatus or GenerationJob.FailedStatus);
            parent.ProgressPercent = (int)Math.Round(terminalCount * 100d / statuses.Count);
            parent.UpdatedAt = now;
            if (statuses.Any(status => status is GenerationJob.QueuedStatus or GenerationJob.RunningStatus))
            {
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            var allDone = statuses.All(status => status == GenerationJob.DoneStatus);
            parent.State = allDone ? AuthoringJobState.Succeeded : AuthoringJobState.Failed;
            parent.CompletedAt = now;
            parent.ErrorCode = allDone ? null : "module_generation_failed";
            parent.ErrorDetail = allDone ? null : "One or more course modules failed after bounded retries.";
            var project = await db.AuthoringProjects.SingleAsync(
                candidate => candidate.Id == parent.AuthoringProjectId,
                cancellationToken);
            project.State = allDone ? AuthoringProjectState.Reviewing : AuthoringProjectState.Failed;
            project.FailureCode = parent.ErrorCode;
            project.FailureDetail = parent.ErrorDetail;
            project.UpdatedAt = now;
            project.Version++;
            if (project.LegacyEducatorProjectId.HasValue)
            {
                var legacy = await db.EducatorProjects.SingleAsync(
                    candidate => candidate.Id == project.LegacyEducatorProjectId.Value,
                    cancellationToken);
                legacy.Status = allDone ? "ReadyForReview" : "GenerationFailed";
                legacy.UpdatedAt = now;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        private async Task ReleaseForShutdownAsync(JobLease lease, Guid? moduleId)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var job = await db.GenerationJobs.SingleOrDefaultAsync(
                candidate => candidate.Id == lease.JobId
                    && candidate.Status == GenerationJob.RunningStatus
                    && candidate.LeaseToken == lease.Token,
                CancellationToken.None);
            if (job == null)
            {
                return;
            }

            job.Status = GenerationJob.QueuedStatus;
            job.AttemptCount = Math.Max(0, job.AttemptCount - 1);
            job.AvailableAt = UtcNow();
            job.StartedAt = null;
            job.ErrorCode = "worker_shutdown";
            job.Error = "Generation was returned to the queue during worker shutdown.";
            ClearLease(job);
            if (moduleId.HasValue)
            {
                var module = await db.Modules.SingleOrDefaultAsync(candidate => candidate.Id == moduleId.Value, CancellationToken.None);
                if (module?.GenerationStatus == ModuleGenerationStatus.Generating)
                {
                    module.GenerationStatus = ModuleGenerationStatus.Blueprint;
                }
            }
            await db.SaveChangesAsync(CancellationToken.None);
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static void ClearLease(GenerationJob job)
        {
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;
            job.LastHeartbeatAt = null;
        }

        private static TimeSpan RetryDelay(int attempt) =>
            TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Clamp(attempt, 1, 8)) * 5));

        private static string Truncate(string value, int length) =>
            value.Length <= length ? value : value[..length];

        internal sealed record JobLease(Guid JobId, Guid Token);
        private sealed record PreparedJob(Guid ProjectId, Guid ModuleId, JitModuleGenerationRequest Request);

        internal sealed record ProjectStatusNotification(
            Guid LearnerId,
            Guid ProjectId,
            string ProjectTitle,
            bool AllDone,
            bool AnyDone);

        private sealed class LeaseLostException : Exception
        {
            public LeaseLostException() : base("The worker no longer owns this generation lease.")
            {
            }
        }
    }
}
