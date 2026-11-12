using IFA.Domain.Entities;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IFA.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class DurableWorkerTests
{
    private readonly PostgresFixture _fixture;

    public DurableWorkerTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrationCommandIsIdempotentAndLeavesNoPendingMigrations()
    {
        await using var provider = _fixture.CreateProvider("migration-check", includeWorkers: false);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationTest");

        await DatabaseMigrator.MigrateAsync(provider, logger);
        await DatabaseMigrator.MigrateAsync(provider, logger);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task ConcurrentWorkersClaimDisjointJobsAndFenceWrongTokens()
    {
        await _fixture.ResetAsync();
        await SeedJobsAsync(5);
        await using var firstProvider = _fixture.CreateProvider("worker-a");
        await using var secondProvider = _fixture.CreateProvider("worker-b");
        var first = GenerationWorker(firstProvider);
        var second = GenerationWorker(secondProvider);

        var claims = await Task.WhenAll(
            first.ClaimAsync(CancellationToken.None),
            second.ClaimAsync(CancellationToken.None));
        var all = claims.SelectMany(result => result).ToArray();

        Assert.Equal(5, all.Length);
        Assert.Equal(5, all.Select(lease => lease.JobId).Distinct().Count());

        var owner = claims[0].Count > 0 ? first : second;
        var ownedLease = claims[0].Count > 0 ? claims[0][0] : claims[1][0];
        Assert.False(await owner.RenewLeaseAsync(
            new ModuleGenerationBackgroundService.JobLease(ownedLease.JobId, Guid.NewGuid()),
            CancellationToken.None));
        Assert.True(await owner.RenewLeaseAsync(ownedLease, CancellationToken.None));
    }

    private async Task SeedJobsAsync(int count)
    {
        await using var provider = _fixture.CreateProvider("seed", includeWorkers: false);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var learner = new Learner
        {
            Name = "Worker Test Educator",
            Email = $"worker-{Guid.NewGuid():N}@example.test",
            Role = "Educator"
        };
        var course = new Course
        {
            Title = "Lease Test Course",
            Description = "Exercises durable claims.",
            ShareCode = Guid.NewGuid().ToString("N").ToUpperInvariant(),
            CreatorLearnerId = learner.Id
        };
        var project = new EducatorProject
        {
            EducatorLearnerId = learner.Id,
            CourseId = course.Id,
            Title = course.Title,
            Description = course.Description,
            Status = "Generating"
        };

        db.AddRange(learner, course, project);
        for (var index = 0; index < count; index++)
        {
            db.GenerationJobs.Add(new GenerationJob
            {
                ProjectId = project.Id,
                CourseId = course.Id,
                ModuleIndex = index,
                ModuleTitle = $"Module {index + 1}",
                Status = GenerationJob.QueuedStatus,
                AvailableAt = DateTime.UtcNow,
                MaxAttempts = 3
            });
        }
        await db.SaveChangesAsync();
    }

    private static ModuleGenerationBackgroundService GenerationWorker(IServiceProvider provider) =>
        provider.GetServices<IHostedService>().OfType<ModuleGenerationBackgroundService>().Single();
}
