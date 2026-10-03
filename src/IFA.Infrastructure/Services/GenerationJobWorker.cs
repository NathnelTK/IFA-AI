using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    public class GenerationJobWorker : BackgroundService
    {
        private readonly IGenerationJobQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GenerationJobWorker> _logger;

        public GenerationJobWorker(IGenerationJobQueue queue, IServiceScopeFactory scopeFactory, ILogger<GenerationJobWorker> logger)
        {
            _queue = queue; _scopeFactory = scopeFactory; _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Crash/restart recovery: anything left Queued or Running from
            // a previous process run gets put back on the queue before we
            // process anything new. This is what makes the in-memory queue
            // safe to use - the database, not the queue, is the real record.
            await RequeueUnfinishedJobsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                Guid jobId;
                try
                {
                    jobId = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break; // app is shutting down
                }

                // Scoped services (DbContext etc.) need a fresh scope per job -
                // BackgroundService itself is a long-lived singleton.
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<ICourseGenerationOrchestrator>();

                try
                {
                    await orchestrator.RunJobAsync(jobId, stoppingToken);
                }
                catch (Exception ex)
                {
                    // Defensive: RunJobAsync already catches its own errors
                    // and marks the job Failed. This catch only guards
                    // against something going wrong OUTSIDE that try block.
                    _logger.LogError(ex, "Unhandled error processing job {JobId}.", jobId);
                }
            }
        }

        private async Task RequeueUnfinishedJobsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var unfinished = await db.GenerationJobs
                .Where(j => j.Status == GenerationJobStatus.Queued || j.Status == GenerationJobStatus.Running)
                .ToListAsync(ct);

            foreach (var job in unfinished)
            {
                _logger.LogInformation("Recovering job {JobId} (was {Status}) after restart.", job.Id, job.Status);
                job.Status = GenerationJobStatus.Queued; // reset so RunJobAsync starts it cleanly
                await _queue.EnqueueAsync(job.Id, ct);
            }

            if (unfinished.Count > 0)
                await db.SaveChangesAsync(ct);
        }
    }
}