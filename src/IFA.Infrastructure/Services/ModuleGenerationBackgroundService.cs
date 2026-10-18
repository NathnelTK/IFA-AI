using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
                jobIds = await db.GenerationJobs
                    .Where(j => j.Status == "Queued")
                    .OrderBy(j => j.CreatedAt)
                    .Take(MaxInFlight)
                    .Select(j => j.Id)
                    .ToListAsync(stoppingToken);

                // Claim guard: mark Running before dispatch so a second poll
                // (or a second host) never picks the same job up again.
                if (jobIds.Count > 0)
                {
                    var jobs = await db.GenerationJobs
                        .Where(j => jobIds.Contains(j.Id))
                        .ToListAsync(stoppingToken);
                    foreach (var job in jobs)
                    {
                        job.Status = "Running";
                        job.StartedAt = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
            }

            if (jobIds.Count == 0) return;

            _logger.LogInformation("Dispatching {Count} module generation job(s).", jobIds.Count);
            await Task.WhenAll(jobIds.Select(id => RunJobAsync(id, stoppingToken)));

            await FinalizeProjectsAsync(jobIds, CancellationToken.None);
        }

        private async Task RunJobAsync(Guid jobId, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var generationService = scope.ServiceProvider.GetRequiredService<CourseGenerationService>();

                var job = await db.GenerationJobs.FirstOrDefaultAsync(j => j.Id == jobId, stoppingToken);
                if (job == null) return;

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
                var module = course?.Modules
                    .OrderBy(m => m.ModuleNumber)
                    .Skip(job.ModuleIndex)
                    .FirstOrDefault();

                if (project?.Blueprint == null || course == null || module == null)
                {
                    job.Status = "Failed";
                    job.Error = "Project, blueprint, course or module row was missing.";
                    job.CompletedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    return;
                }

                var blueprint = JsonSerializer.Deserialize<CourseBlueprintDocument>(
                    project.Blueprint.Json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var blueprintModule = blueprint?.Modules.Skip(job.ModuleIndex).FirstOrDefault();

                var includedSources = project.Sources
                    .Where(s => s.Included)
                    .OrderBy(s => s.CreatedAt)
                    .ToList();

                var researchContext = string.Join("\n\n", includedSources.Select(s =>
                {
                    var content = string.IsNullOrWhiteSpace(s.ExtractedContent)
                        ? "(metadata only)"
                        : s.ExtractedContent![..Math.Min(3000, s.ExtractedContent.Length)];
                    return $"[SOURCE] Title: {s.Title}\nURL: {s.Url}\nExtracted: {content.ReplaceLineEndings(" ")}";
                }));

                var materials = string.IsNullOrWhiteSpace(course.ExternalMaterials)
                    ? new List<string>()
                    : course.ExternalMaterials
                        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(m => m.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                var request = new JitModuleGenerationRequest
                {
                    CourseTitle = course.Title,
                    ModuleNumber = job.ModuleIndex + 1,
                    ModuleTitle = module.Title,
                    TargetGoal = blueprintModule?.Description ?? module.Summary,
                    KeyTopics = blueprintModule?.Chapters
                        .Select(c => c.Title)
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList() ?? new List<string>(),
                    ResearchContext = researchContext.Length > 0
                        ? researchContext
                        : $"No extracted source text available. Build the lessons from the blueprint chapter plan for: {module.Title}.",
                    ExternalMaterials = materials
                };

                var generated = await generationService.GenerateJitModuleAsync(request, stoppingToken);

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
                await db.SaveChangesAsync(CancellationToken.None);
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
                        var message = ex.Message;
                        job.Error = message.Length > 500 ? message[..500] : message;
                        job.CompletedAt = DateTime.UtcNow;
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
                    var anyQueuedOrRunning = jobs.Any(j => j.Status is "Queued" or "Running");
                    if (anyQueuedOrRunning) continue;
                    if (jobs.Count == 0) continue;

                    var allDone = jobs.All(j => j.Status == "Done");
                    var anyDone = jobs.Any(j => j.Status == "Done");

                    if (allDone)
                    {
                        // Only flip + notify when transitioning.
                        if (project.Status != "ReadyForReview")
                        {
                            project.Status = "ReadyForReview";
                            project.UpdatedAt = DateTime.UtcNow;
                            await db.SaveChangesAsync(ct);
                            try
                            {
                                await notifications.SendNotificationAsync(
                                    project.EducatorLearnerId,
                                    "course_generation_complete",
                                    "Course generation complete",
                                    $"All modules for '{project.Title}' were generated. Review and publish when ready.",
                                    $"/educator/projects/{project.Id}",
                                    ct);
                            }
                            catch (Exception notifyEx)
                            {
                                _logger.LogWarning(notifyEx, "Completion notification failed for project {ProjectId}.", projectId);
                            }
                        }
                    }
                    else if (anyDone && jobs.Any(j => j.Status == "Failed"))
                    {
                        // Some modules failed but none are pending: notify once by
                        // stamping a sentinel on the first failed job's Error.
                        var firstFailed = jobs.First(j => j.Status == "Failed");
                        if (firstFailed.Error is null || !firstFailed.Error.Contains("[notified]", StringComparison.Ordinal))
                        {
                            firstFailed.Error = "[notified] " + (firstFailed.Error ?? "Generation failed.");
                            await db.SaveChangesAsync(ct);
                            try
                            {
                                await notifications.SendNotificationAsync(
                                    project.EducatorLearnerId,
                                    "course_generation_partial_failure",
                                    "Some modules failed to generate",
                                    $"Some modules for '{project.Title}' failed to generate. You can retry them from the project page.",
                                    $"/educator/projects/{project.Id}",
                                    ct);
                            }
                            catch (Exception notifyEx)
                            {
                                _logger.LogWarning(notifyEx, "Failure notification failed for project {ProjectId}.", projectId);
                            }
                        }
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
