using Microsoft.Extensions.Configuration;
using Npgsql;

namespace IFA.Infrastructure.Data
{
    public static class DatabaseConnectionResolver
    {
        public static string Resolve(IConfiguration configuration, bool forMigrations = false)
        {
            var explicitConnection = forMigrations
                ? configuration["MIGRATION_DATABASE_URL"]
                : null;

            explicitConnection ??= configuration.GetConnectionString("DefaultConnection")
                ?? configuration["DATABASE_URL"]
                ?? configuration["IFA_DB_CONNECTION"];

            if (!string.IsNullOrWhiteSpace(explicitConnection))
            {
                return Normalize(explicitConnection);
            }

            var database = configuration.GetSection("Database");
            return PostgresConnectionString.Create(
                host: configuration["DB_HOST"] ?? database["Host"] ?? "localhost",
                port: int.TryParse(configuration["DB_PORT"] ?? database["Port"], out var port) ? port : 5432,
                database: configuration["DB_NAME"] ?? database["Name"] ?? "ifa",
                username: configuration["DB_USER"] ?? database["User"] ?? "ifa",
                password: configuration["DB_PASSWORD"] ?? database["Password"] ?? string.Empty);
        }

        private static string Normalize(string value)
        {
            if (!value.Contains("://", StringComparison.Ordinal))
            {
                return value;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
            {
                throw new InvalidOperationException("DATABASE_URL must be a PostgreSQL connection string or postgres:// URI.");
            }

            var userInfo = uri.UserInfo.Split(':', 2);
            if (userInfo.Length != 2)
            {
                throw new InvalidOperationException("DATABASE_URL must include a username and password.");
            }

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort ? 5432 : uri.Port,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = Uri.UnescapeDataString(userInfo[1])
            };

            var sslModeValue = uri.Query
                .TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .FirstOrDefault(part => part.Length == 2 && part[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))?
                .ElementAtOrDefault(1);
            if (Enum.TryParse<Npgsql.SslMode>(Uri.UnescapeDataString(sslModeValue ?? string.Empty), true, out var sslMode))
            {
                builder.SslMode = sslMode;
            }

            return builder.ConnectionString;
        }
    }
}
