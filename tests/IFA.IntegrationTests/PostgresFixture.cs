using DotNet.Testcontainers.Builders;
using IFA.Infrastructure;
using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace IFA.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-U", "postgres"))
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var provider = CreateProvider("migration-fixture", includeWorkers: false);
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("IntegrationMigration");
        await DatabaseMigrator.MigrateAsync(provider, logger);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public ServiceProvider CreateProvider(string workerId, bool includeWorkers = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            ["DB_PROVIDER"] = "postgres",
            ["WORKER_INSTANCE_ID"] = workerId,
            ["Workers:Generation:MaxInFlight"] = "3",
            ["Workers:Generation:PollSeconds"] = "60",
            ["Workers:Generation:LeaseSeconds"] = "120",
            ["Workers:Generation:HeartbeatSeconds"] = "30",
            ["Workers:Generation:MaxAttempts"] = "3",
            ["Workers:Outbox:Enabled"] = "false"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(ConnectionString, configuration);
        if (includeWorkers)
        {
            services.AddWorkerInfrastructure(configuration);
        }
        return services.BuildServiceProvider(validateScopes: true);
    }

    public async Task ResetAsync()
    {
        await using var provider = CreateProvider("reset", includeWorkers: false);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Learners\" CASCADE");
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
