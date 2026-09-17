using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IFA.Infrastructure.Data
{
    /// <summary>
    /// Enables `dotnet ef` to create the DbContext at design time without
    /// booting the API. The connection string can be overridden with the
    /// IFA_DB_CONNECTION environment variable.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        private const string DefaultConnectionString =
            "Host=localhost;Port=5432;Database=ifa;Username=ifa;Password=ifa_dev_password";

        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("IFA_DB_CONNECTION") ?? DefaultConnectionString;

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
