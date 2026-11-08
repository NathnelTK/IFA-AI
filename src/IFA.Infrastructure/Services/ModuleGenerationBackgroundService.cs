using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Polls GenerationJob rows and generates one course module per job in the
    /// background after an educator approves a blueprint. Each job runs in its
    /// own DbContext scope so a failure can never corrupt or block the others;
    /// the host loop itself never crashes (everything is guarded).
    /// </summary>
    public class ModuleGenerationBackgroundService : BackgroundService
    {
        private const int MaxInFlight = 3;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ModuleGenerationBackgroundService> _logger;
        private bool _schemaMissingLogged;

        public ModuleGenerationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ModuleGenerationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Module generation background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
                {
                    // Table missing (e.g. the app started against a legacy schema
                    // that startup is still upgrading). Wait quietly instead of
                    // spamming errors — startup self-heals the schema.
                    if (!_schemaMissingLogged)
                    {
                        _schemaMissingLogged = true;
                        _logger.LogWarning(
                            "Generation tables not ready ({Table}); waiting for schema initialization.",
                            ex.TableName ?? "GenerationJobs");
                    }
                }
                catch (Exception ex)
                {
                    // The host must survive ANY per-cycle failure.
                    _logger.LogError(ex, "Module generation poll cycle failed; retrying.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Module generation background service stopped.");
        }

        private async Task PollAsync(CancellationToken stoppingToken)
        {
            List<Guid> jobIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var staleBefore = DateTime.UtcNow.AddHours(-1);
                await db.GenerationJobs.Where(job => job.Status == "Running" && job.StartedAt < staleBefore)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(job => job.Status, "Queued")
                        .SetProperty(job => job.StartedAt, (DateTime?)null)
                        .SetProperty(job => job.Error, "Recovered after an interrupted generation worker."), stoppingToken);

                await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
                List<GenerationJob> jobs;
                if (db.Database.IsNpgsql())
                {
                    jobs = await db.GenerationJobs.FromSqlRaw("""
                        SELECT * FROM "GenerationJobs"
                        WHERE "Status" = 'Queued'
                        ORDER BY "CreatedAt"
                        FOR UPDATE SKIP LOCKED
                        LIMIT {0}
                        """, MaxInFlight)
                        .ToListAsync(stoppingToken);
                }
                else
                {
                    jobs = await db.GenerationJobs.Where(job => job.Status == "Queued")
                        .OrderBy(job => job.CreatedAt).Take(MaxInFlight).ToListAsync(stoppingToken);
                }
                foreach (var job in jobs)
                {
                    job.Status = "Running";
                    job.StartedAt = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(stoppingToken);
                await transaction.CommitAsync(stoppingToken);
                jobIds = jobs.Select(job => job.Id).ToList();
            }

            if (jobIds.Count == 0) return;

            _logger.LogInformation("Dispatching {Count} module generation job(s).", jobIds.Count);
            await Task.WhenAll(jobIds.Select(id => RunJobAsync(id, stoppingToken)));

            await FinalizeProjectsAsync(jobIds, CancellationToken.None);
        }

        private async Task RunJobAsync(Guid jobId, CancellationToken stoppingToken)
        {
            Guid? targetModuleId = null;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var generationService = scope.ServiceProvider.GetRequiredService<CourseGenerationService>();

                var job = await db.GenerationJobs.FirstOrDefaultAsync(j => j.Id == jobId, stoppingToken);
                if (job?.Status != "Running") return;

                var project = await db.EducatorProjects
                    .Include(p => p.Sources)
                    .Include(p => p.Blueprint)
                    .FirstOrDefaultAsync(p => p.Id == job.ProjectId, stoppingToken);
                var course = await db.Courses
                    .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                    .Include(c => c.Modules)
                    .ThenInclude(m => m.Quizzes)
                    .FirstOrDefaultAsync(c => c.Id == job.CourseId, stoppingToken);
                if (project?.Blueprint?.ApprovedAt == null || course == null || project.CourseId != course.Id || course.IsPublished)
                    throw new InvalidOperationException("The approved project must still refer to this unpublished course.");

                var blueprint = BlueprintService.Parse(project.Blueprint.Json, project.Sources);
                if (blueprint == null || job.ModuleIndex < 0 || job.ModuleIndex >= blueprint.Modules.Count)
                    throw new InvalidOperationException("The approved blueprint or its extracted sources are no longer valid. Complete the draft manually.");

                var blueprintModule = blueprint.Modules[job.ModuleIndex];
                // The editor locks structure while jobs are active. Refuse stale ordering/titles instead of guessing a row.
                var module = course.Modules.SingleOrDefault(m => m.ModuleNumber == job.ModuleIndex + 1 && m.Title == job.ModuleTitle);
                if (module == null || blueprintModule.Title != job.ModuleTitle)
                    throw new InvalidOperationException("The module no longer matches its approved generation job. No content was appended.");
                if (module.Lessons.Any() || module.Quizzes.Any())
                    throw new InvalidOperationException("This module already has content. Automatic append retries are disabled; complete the existing draft manually.");
                targetModuleId = module.Id;

                var moduleUrls = blueprintModule.Sources.Select(s => s.Url).ToHashSet(StringComparer.Ordinal);
                var includedSources = SourceAnalysisService.GetAcceptedSources(project.Sources)
                    .Where(s => moduleUrls.Contains(s.Url)).ToList();
                var researchContext = SourceAnalysisService.BuildDigest(includedSources);

                var request = new JitModuleGenerationRequest
                {
                    CourseTitle = course.Title,
                    ModuleNumber = module.ModuleNumber,
                    ModuleTitle = module.Title,
                    TargetGoal = blueprintModule.Description,
                    KeyTopics = blueprintModule.Chapters
                        .Select(c => c.Title)
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList(),
                    ResearchContext = researchContext,
                    ExternalMaterials = includedSources.Select(s => s.Url).Distinct(StringComparer.Ordinal).ToList()
                };

                module.GenerationStatus = ModuleGenerationStatus.Generating;
                await db.SaveChangesAsync(stoppingToken);
                var generated = await generationService.GenerateJitModuleAsync(request, stoppingToken);
                stoppingToken.ThrowIfCancellationRequested();

                // The builder returns a throwaway Module graph; only the lessons
                // and quizzes belong to the tracked module (same detach pattern
                // as ModulesController).
                foreach (var lesson in generated.Lessons)
                {
                    lesson.Module = null;
                    lesson.ModuleId = module.Id;
                    db.Add(lesson);
                }
                foreach (var quiz in generated.Quizzes)
                {
                    quiz.Module = null;
                    foreach (var question in quiz.Questions)
                    {
                        question.Quiz = null;
                    }
                    quiz.ModuleId = module.Id;
                    db.Add(quiz);
                }

                module.GenerationStatus = ModuleGenerationStatus.Ready;
                module.GeneratedAt = DateTime.UtcNow;

                job.Status = "Done";
                job.Error = null;
                job.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generation job {JobId} failed.", jobId);
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var job = await db.GenerationJobs.FirstOrDefaultAsync(j => j.Id == jobId, CancellationToken.None);
                    if (job != null)
                    {
                        job.Status = "Failed";
                        var message = ex is OperationCanceledException
                            ? "Generation was interrupted. Complete this draft manually."
                            : ex is InvalidOperationException
                                ? ex.Message
                                : "Module generation failed. No replacement content was saved; complete the draft manually.";
                        job.Error = message.Length > 500 ? message[..500] : message;
                        job.CompletedAt = DateTime.UtcNow;
                        if (targetModuleId.HasValue)
                        {
                            var module = await db.Modules.FirstOrDefaultAsync(m => m.Id == targetModuleId.Value, CancellationToken.None);
                            if (module?.GenerationStatus == ModuleGenerationStatus.Generating)
                            {
                                module.GenerationStatus = ModuleGenerationStatus.Failed;
                                module.GeneratedAt = null;
                            }
                        }
                        await db.SaveChangesAsync(CancellationToken.None);
                    }
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Could not record failure state for job {JobId}.", jobId);
                }
            }
        }

        private async Task FinalizeProjectsAsync(List<Guid> jobIds, CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var projectIds = await db.GenerationJobs
                    .Where(j => jobIds.Contains(j.Id))
                    .Select(j => j.ProjectId)
                    .Distinct()
                    .ToListAsync(ct);

                foreach (var projectId in projectIds)
                {
                    var project = await db.EducatorProjects
                        .Include(p => p.GenerationJobs)
                        .FirstOrDefaultAsync(p => p.Id == projectId, ct);
                    if (project == null) continue;

                    var jobs = project.GenerationJobs;
                    var anyQueuedOrRunning = jobs.Any(j => j.Status is "Pending" or "Queued" or "Running");
                    if (anyQueuedOrRunning) continue;
                    if (jobs.Count == 0) continue;

                    var allDone = jobs.All(j => j.Status == "Done");
                    var anyDone = jobs.Any(j => j.Status == "Done");

                    var nextStatus = allDone ? "ReadyForReview" : "GenerationFailed";
                    if (project.Status == nextStatus) continue;
                    // Generation only prepares a draft. Publication is a separate educator action.
                    project.Status = nextStatus;
                    project.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                    try
                    {
                        await notifications.SendNotificationAsync(
                            project.EducatorLearnerId,
                            allDone ? "course_generation_complete" : "course_generation_failed",
                            allDone ? "Course generation complete" : anyDone ? "Some modules failed to generate" : "Course generation failed",
                            allDone
                                ? $"All modules for '{project.Title}' were generated. Review the saved draft before publishing."
                                : $"Generation for '{project.Title}' could not finish. The draft and any completed modules are saved; finish the remaining content manually in the editor.",
                            $"/studio/{project.Id}",
                            ct);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx, "Generation notification failed for project {ProjectId}.", projectId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Project finalization after generation failed.");
            }
        }
    }
}
