using System;
using IFA.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

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

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(DatabaseConnectionResolver.Resolve(configuration, forMigrations: true))
                .Options;

            return new ApplicationDbContext(options);
        }

    }
}
