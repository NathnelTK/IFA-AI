using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Data
{
    internal static class LegacyWitforgeSchemaBridge
    {
        internal const string MigrationId = "20261029110932_AddWitforgeCourseWorkflow";

        private static readonly string[] RequiredTables =
        [
            "Assignments",
            "AssignmentSubmissions",
            "CourseDiscussions",
            "LiveSessions"
        ];

        private static readonly string[] RequiredCourseColumns =
        [
            "AccentColor",
            "EnrollmentClosesAt",
            "EnrollmentLimit",
            "EnrollmentMode",
            "EnrollmentOpensAt",
            "InstructorBio",
            "InstructorTitle",
            "IsPublished",
            "Language",
            "LearningObjectives",
            "Level",
            "MaxQuizAttempts",
            "PassingScorePercentage",
            "Prerequisites",
            "RequirePassingQuizzes",
            "ThemeMode",
            "Visibility"
        ];

        public static async Task<bool> TryReconcileAsync(
            ApplicationDbContext db,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
            if (!pending.Contains(MigrationId))
            {
                return false;
            }

            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var hasMarker = await HasTableAsync(connection, "LiveSessions", cancellationToken)
                || await HasColumnAsync(connection, "Courses", "AccentColor", cancellationToken);
            if (!hasMarker)
            {
                return false;
            }

            var missing = new List<string>();
            foreach (var table in RequiredTables)
            {
                if (!await HasTableAsync(connection, table, cancellationToken))
                {
                    missing.Add($"table:{table}");
                }
            }

            foreach (var column in RequiredCourseColumns)
            {
                if (!await HasColumnAsync(connection, "Courses", column, cancellationToken))
                {
                    missing.Add($"column:Courses.{column}");
                }
            }

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The legacy manually provisioned Witforge schema is incomplete ({string.Join(", ", missing)}). " +
                    "Migration history was not changed. Rehearse and repair this database from a backup before deployment.");
            }

            await ExecuteAsync(connection, """
                UPDATE "Courses" SET "IsPublished" = true, "Visibility" = 'Public' WHERE "IsPublic" = true;
                UPDATE "Courses" SET "ShareCode" = upper(md5("Id"::text || clock_timestamp()::text))
                WHERE length("ShareCode") < 20;
                """, cancellationToken);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId"));
                """, cancellationToken);

            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                SELECT @id, @version
                WHERE NOT EXISTS (
                    SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = @id);
                """;
            AddParameter(insert, "@id", MigrationId);
            AddParameter(
                insert,
                "@version",
                typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "10.0");
            await insert.ExecuteNonQueryAsync(cancellationToken);

            logger.LogWarning(
                "Validated the frozen legacy schema bridge and stamped only migration {MigrationId}. Future schema changes still run through EF migrations.",
                MigrationId);
            return true;
        }

        private static async Task<bool> HasTableAsync(
            DbConnection connection,
            string tableName,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass('public.' || quote_ident(@name)) IS NOT NULL";
            AddParameter(command, "@name", tableName);
            return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        private static async Task<bool> HasColumnAsync(
            DbConnection connection,
            string tableName,
            string columnName,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = @table AND column_name = @column);
                """;
            AddParameter(command, "@table", tableName);
            AddParameter(command, "@column", columnName);
            return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        private static async Task ExecuteAsync(
            DbConnection connection,
            string sql,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
    }
}
