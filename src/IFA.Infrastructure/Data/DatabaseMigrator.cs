using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Data
{
    public static class DatabaseMigrator
    {
        public static async Task MigrateAsync(
            IServiceProvider services,
            ILogger logger,
            CancellationToken cancellationToken = default,
            bool seedDemoData = false)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (db.Database.IsSqlite())
            {
                logger.LogWarning("Creating the local SQLite schema. Production migration ownership requires PostgreSQL.");
                await db.Database.EnsureCreatedAsync(cancellationToken);
                await SeedDemoDataAsync(db, logger, seedDemoData);
                return;
            }

            if (!db.Database.IsNpgsql())
            {
                throw new InvalidOperationException("The migration command supports only PostgreSQL and local SQLite.");
            }

            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);
            await using var acquire = connection.CreateCommand();
            acquire.CommandText = "SELECT pg_advisory_lock(hashtext('witforge:database-migrations'))";
            await acquire.ExecuteNonQueryAsync(cancellationToken);

            try
            {
                await LegacyWitforgeSchemaBridge.TryReconcileAsync(db, logger, cancellationToken);
                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
                if (pending.Length > 0)
                {
                    logger.LogInformation("Applying {Count} EF migration(s): {Migrations}", pending.Length, pending);
                    await db.Database.MigrateAsync(cancellationToken);
                    logger.LogInformation("Database migrations completed.");
                }
                else
                {
                    logger.LogInformation("Database schema is current.");
                }

                await SeedDemoDataAsync(db, logger, seedDemoData);
            }
            finally
            {
                await using var release = connection.CreateCommand();
                release.CommandText = "SELECT pg_advisory_unlock(hashtext('witforge:database-migrations'))";
                await release.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }

        private static async Task SeedDemoDataAsync(ApplicationDbContext db, ILogger logger, bool enabled)
        {
            if (enabled)
            {
                await DemoDataSeeder.SeedAsync(db, logger);
            }
        }
    }
}
