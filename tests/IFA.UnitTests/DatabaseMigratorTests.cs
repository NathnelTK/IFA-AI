using IFA.Infrastructure;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Services;
using IFA.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFA.UnitTests;

public sealed class DatabaseMigratorTests
{
    [Fact]
    public async Task MigrateAsync_SeedsDemoAccountsWhenEnabled()
    {
        await using var connection = new SqliteConnection("Data Source=seed-enabled;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection.ConnectionString);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigratorTests");

        await DatabaseMigrator.MigrateAsync(provider, logger, seedDemoData: true);

        await using (var preparationScope = provider.CreateAsyncScope())
        {
            var preparationDb = preparationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var demoLearner = await preparationDb.Learners.SingleAsync(learner => learner.Email == DemoAccounts.All[0].Email);
            demoLearner.PasswordHash = PasswordHasher.Hash("stale-password");
            demoLearner.DisabledAt = DateTime.UtcNow;
            demoLearner.EmailVerifiedAt = null;

            var educatorSpec = DemoAccounts.All.Single(account => account.Role == "Educator");
            var demoEducator = await preparationDb.Learners.SingleAsync(learner => learner.Email == educatorSpec.Email);
            demoEducator.Role = "Learner";
            var educatorGrant = await preparationDb.AccountRoleGrants.SingleAsync(grant =>
                grant.AccountId == demoEducator.Id && grant.Role == AccountRoleKind.Educator);
            educatorGrant.IsActive = false;
            educatorGrant.RevokedAt = DateTime.UtcNow;
            var educatorMembership = await preparationDb.WorkspaceMemberships.SingleAsync(membership =>
                membership.AccountId == demoEducator.Id);
            educatorMembership.State = WorkspaceMembershipState.Revoked;
            educatorMembership.RevokedAt = DateTime.UtcNow;
            await preparationDb.SaveChangesAsync();
        }

        await DatabaseMigrator.MigrateAsync(provider, logger, seedDemoData: true);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seededLearners = await db.Learners
            .Where(learner => DemoAccounts.All.Select(account => account.Email).Contains(learner.Email))
            .ToListAsync();

        Assert.Equal(DemoAccounts.All.Length, seededLearners.Count);
        var repairedDemoLearner = seededLearners.Single(learner => learner.Email == DemoAccounts.All[0].Email);
        Assert.True(PasswordHasher.Verify(repairedDemoLearner.PasswordHash, DemoAccounts.All[0].Password));
        Assert.Null(repairedDemoLearner.DisabledAt);
        Assert.NotNull(repairedDemoLearner.EmailVerifiedAt);

        var learnerIds = seededLearners.Select(learner => learner.Id).ToArray();
        Assert.Equal(learnerIds.Length, await db.AccountRoleGrants.CountAsync(grant =>
            learnerIds.Contains(grant.AccountId)
            && grant.Role == AccountRoleKind.Learner
            && grant.IsActive
            && grant.RevokedAt == null));

        var educator = seededLearners.Single(learner => learner.Email == DemoAccounts.All.Single(account => account.Role == "Educator").Email);
        Assert.Equal("Educator", educator.Role);
        Assert.Equal("Educator", educator.RegistrationIntent);
        Assert.True(await db.AccountRoleGrants.AnyAsync(grant =>
            grant.AccountId == educator.Id
            && grant.Role == AccountRoleKind.Educator
            && grant.IsActive
            && grant.RevokedAt == null));
        var workspace = await db.EducatorWorkspaces.SingleAsync(candidate => candidate.CreatedByAccountId == educator.Id);
        Assert.Equal(EducatorWorkspaceStatus.Verified, workspace.Status);
        Assert.True(await db.WorkspaceMemberships.AnyAsync(membership =>
            membership.WorkspaceId == workspace.Id
            && membership.AccountId == educator.Id
            && membership.Role == WorkspaceMembershipRole.Owner
            && membership.State == WorkspaceMembershipState.Active
            && membership.RevokedAt == null));

        var seededCourses = await db.Courses.ToListAsync();
        Assert.NotEmpty(seededCourses);
        Assert.All(seededCourses, course =>
        {
            Assert.False(string.IsNullOrWhiteSpace(course.Overview));
            Assert.True(course.CourseAssistantEnabled);
            Assert.True(course.AwardsCertificate);
            Assert.Null(course.AppearanceJson);
            Assert.Equal(0, course.AppearanceVersion);
        });
    }

    [Fact]
    public async Task MigrateAsync_DoesNotSeedDemoAccountsByDefault()
    {
        await using var connection = new SqliteConnection("Data Source=seed-disabled;Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await using var provider = CreateProvider(connection.ConnectionString);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigratorTests");

        await DatabaseMigrator.MigrateAsync(provider, logger);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Empty(await db.Learners.ToListAsync());
    }

    private static ServiceProvider CreateProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_PROVIDER"] = "sqlite"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(connectionString, configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }
}
