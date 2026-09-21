using System;
using IFA.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IFA.Infrastructure.Data
{
    /// <summary>
    /// Enables `dotnet ef` to create the DbContext at design time without
    /// booting the API. Configuration is resolved from the local .env file
    /// and process environment variables; a full connection string can be
    /// supplied through ConnectionStrings__DefaultConnection or
    /// IFA_DB_CONNECTION.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            EnvFile.Load();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ResolveConnectionString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static string ResolveConnectionString()
        {
            var explicitConnection =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("IFA_DB_CONNECTION");

            if (!string.IsNullOrWhiteSpace(explicitConnection))
            {
                return explicitConnection;
            }

            return PostgresConnectionString.Create(
                host: Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost",
                port: ParsePort(Environment.GetEnvironmentVariable("DB_PORT")),
                database: Environment.GetEnvironmentVariable("DB_NAME") ?? "ifa",
                username: Environment.GetEnvironmentVariable("DB_USER") ?? "ifa",
                password: Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty);
        }

        private static int ParsePort(string? value) =>
            int.TryParse(value, out var port) ? port : 5432;
    }
}
