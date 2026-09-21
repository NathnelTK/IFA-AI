using System;
using Npgsql;

namespace IFA.Infrastructure.Data
{
    /// <summary>
    /// Builds a PostgreSQL connection string from discrete settings so the
    /// password never has to live in source-controlled configuration files.
    /// </summary>
    public static class PostgresConnectionString
    {
        public static string Create(
            string host,
            int port,
            string database,
            string username,
            string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "The PostgreSQL password is not configured. Set the DB_PASSWORD environment " +
                    "variable locally via .env (see .env.example) or provide it through the " +
                    "deployment environment.");
            }

            return new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password
            }.ConnectionString;
        }
    }
}
