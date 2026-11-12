using IFA.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace IFA.UnitTests;

public sealed class DatabaseConnectionResolverTests
{
    [Fact]
    public void NormalizesPostgresUriWithoutLosingEncodedCredentials()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "postgresql://witforge:p%40ssword@db.example.test:6543/witforge?sslmode=Require"
        });

        var result = new NpgsqlConnectionStringBuilder(DatabaseConnectionResolver.Resolve(configuration));

        Assert.Equal("db.example.test", result.Host);
        Assert.Equal(6543, result.Port);
        Assert.Equal("witforge", result.Database);
        Assert.Equal("witforge", result.Username);
        Assert.Equal("p@ssword", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void MigrationConnectionDoesNotLeakIntoRuntimeResolution()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=runtime;Database=app;Username=app;Password=runtime-secret",
            ["MIGRATION_DATABASE_URL"] = "Host=migrator;Database=app;Username=ddl;Password=migration-secret"
        });

        var runtime = new NpgsqlConnectionStringBuilder(DatabaseConnectionResolver.Resolve(configuration));
        var migration = new NpgsqlConnectionStringBuilder(DatabaseConnectionResolver.Resolve(configuration, forMigrations: true));

        Assert.Equal("runtime", runtime.Host);
        Assert.Equal("migrator", migration.Host);
        Assert.Equal("ddl", migration.Username);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
