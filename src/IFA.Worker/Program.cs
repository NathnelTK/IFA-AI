using IFA.Infrastructure;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

EnvFile.Load();

var builder = Host.CreateApplicationBuilder(args);
var migrateOnly = args.Length == 1 && args[0].Equals("migrate", StringComparison.OrdinalIgnoreCase);
var statusOnly = args.Length == 1 && args[0].Equals("status", StringComparison.OrdinalIgnoreCase);
if (args.Length > 0 && !migrateOnly && !statusOnly)
{
    throw new InvalidOperationException("Supported commands are 'migrate' and 'status'. Omit the command to run workers.");
}
var connectionString = DatabaseConnectionResolver.Resolve(builder.Configuration, forMigrations: migrateOnly || statusOnly);
builder.Services.AddInfrastructure(connectionString, builder.Configuration);
if (!migrateOnly && !statusOnly)
{
    builder.Services.AddWorkerInfrastructure(builder.Configuration);
}

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("WorkerStartup");
logger.LogInformation(
    "Starting Witforge worker. Environment={Environment} BuildSha={BuildSha} Command={Command}",
    builder.Environment.EnvironmentName,
    builder.Configuration["BUILD_SHA"] ?? builder.Configuration["RENDER_GIT_COMMIT"] ?? "local",
    migrateOnly ? "migrate" : statusOnly ? "status" : "run");

if (statusOnly)
{
    var parsed = new NpgsqlConnectionStringBuilder(connectionString);
    await using var scope = host.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
    logger.LogInformation(
        "Database target Host={Host} Port={Port} Database={Database} PendingMigrationCount={PendingMigrationCount}",
        parsed.Host,
        parsed.Port,
        parsed.Database,
        pending.Length);
    foreach (var migration in pending)
    {
        logger.LogWarning("Pending migration: {Migration}", migration);
    }
    Environment.ExitCode = pending.Length == 0 ? 0 : 2;
    return;
}

if (migrateOnly)
{
    var seedDemoData = builder.Environment.IsDevelopment()
        || string.Equals(
            builder.Configuration["ASPNETCORE_ENVIRONMENT"],
            Environments.Development,
            StringComparison.OrdinalIgnoreCase)
        || builder.Configuration.GetValue<bool>("DemoData:Enabled")
        || builder.Configuration.GetValue<bool>("DEMO_DATA_ENABLED");
    await DatabaseMigrator.MigrateAsync(host.Services, logger, seedDemoData: seedDemoData);
    return;
}

await ValidateSchemaAsync(host.Services);
await host.RunAsync();

static async Task ValidateSchemaAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!db.Database.IsNpgsql())
    {
        throw new InvalidOperationException("The durable worker requires PostgreSQL. Use 'migrate' only for local SQLite schema creation.");
    }

    var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
    if (pending.Length > 0)
    {
        throw new InvalidOperationException(
            $"The worker cannot start with pending database migrations: {string.Join(", ", pending)}. Run 'IFA.Worker migrate' first.");
    }
}
