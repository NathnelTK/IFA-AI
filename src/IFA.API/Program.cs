
using IFA.API.Endpoints;
using IFA.Application.Common.Exceptions;
using IFA.Infrastructure;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.AspNetCore.Diagnostics;

// Load local secrets (see .env.example) before configuration is built so the
// PostgreSQL password never has to be committed. Missing file is a no-op.
EnvFile.Load();

var builder = WebApplication.CreateBuilder(args);

const string ClientCorsPolicy = "SvelteKitClient";

// -------------------------------------------------------------------------
// Service registration
// -------------------------------------------------------------------------
// IgnoreCycles keeps entity navigation back-references (Course <-> Module <->
// Lesson) from throwing "object cycle detected" when entities are returned
// directly from controllers. The back-reference is emitted as null instead.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Witforge API",
        Version = "v1",
        Description = "Backend API for Witforge learning, persisted course creation, delivery, and optional AI workflows."
    });
});

// JWT Authentication Configuration
var jwtKey = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtKey)
    || jwtKey.Length < 32
    || jwtKey.StartsWith("replace_", StringComparison.OrdinalIgnoreCase)
    || jwtKey.StartsWith("IFA_SUPER_SECRET", StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "Set JWT_SECRET to a unique random value of at least 32 characters before starting the API.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// Database connection resolution with PostgreSQL priority
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration["DATABASE_URL"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    var database = builder.Configuration.GetSection("Database");
    var dbHost = builder.Configuration["DB_HOST"] ?? database["Host"] ?? "localhost";
    
    connectionString = PostgresConnectionString.Create(
        host: dbHost,
        port: int.TryParse(builder.Configuration["DB_PORT"] ?? database["Port"], out var p) ? p : 5432,
        database: builder.Configuration["DB_NAME"] ?? database["Name"] ?? "ifa",
        username: builder.Configuration["DB_USER"] ?? database["User"] ?? "ifa",
        password: builder.Configuration["DB_PASSWORD"] ?? database["Password"] ?? string.Empty);
}

builder.Services.AddInfrastructure(connectionString, builder.Configuration);

// CORS Configuration with environment-aware origins
var corsOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]?.Split(',')
    ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:3000", "http://localhost:4173", "http://localhost:5000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// -------------------------------------------------------------------------
// Database initialization and development seeding
// -------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        
        // Ensure the schema exists. EnsureCreatedAsync() considers the whole
        // database, and on Supabase the auth/storage/realtime schemas always
        // contain tables — so it would silently skip creating our tables after
        // a "DROP SCHEMA public CASCADE" reset. Check the public schema only.
        logger.LogInformation("Ensuring database schema is created...");

        // Production deployments can opt into EF migrations instead of the
        // create-if-empty flow by setting DB_USE_MIGRATIONS=true. This applies
        // pending migrations (e.g. AddEducatorStudioAndPayments) to an existing
        // database without a manual schema reset.
        var migrationSetting = builder.Configuration["DB_USE_MIGRATIONS"] ?? Environment.GetEnvironmentVariable("DB_USE_MIGRATIONS");
        var useMigrations = migrationSetting == null
            ? !db.Database.IsSqlite()
            : string.Equals(migrationSetting, "true", StringComparison.OrdinalIgnoreCase);

        var dbConnection = db.Database.GetDbConnection();
        await dbConnection.OpenAsync();

        static async Task<bool> HasTableAsync(System.Data.Common.DbConnection connection, string tableName)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT to_regclass('public.' || quote_ident(@name)) IS NOT NULL";
            var parameter = cmd.CreateParameter(); parameter.ParameterName = "@name"; parameter.Value = tableName; cmd.Parameters.Add(parameter);
            return (bool)(await cmd.ExecuteScalarAsync())!;
        }

        static async Task<bool> HasColumnAsync(System.Data.Common.DbConnection connection, string tableName, string columnName)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT EXISTS (SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = @table AND column_name = @column)
                """;
            var table = cmd.CreateParameter(); table.ParameterName = "@table"; table.Value = tableName; cmd.Parameters.Add(table);
            var column = cmd.CreateParameter(); column.ParameterName = "@column"; column.Value = columnName; cmd.Parameters.Add(column);
            return (bool)(await cmd.ExecuteScalarAsync())!;
        }

        if (db.Database.IsSqlite())
        {
            await db.Database.EnsureCreatedAsync();
        }
        else if (useMigrations)
        {
            var migrations = db.Database.GetMigrations().ToArray();
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
            var latest = migrations.LastOrDefault();
            // An earlier startup path created the first Witforge tables manually
            // and stamped the old migration history. Reconcile that known state
            // instead of replaying CREATE TABLE/ADD COLUMN over user data.
            var manuallyProvisionedWitforge = pending.Length == 1 && pending[0] == latest &&
                (await HasTableAsync(dbConnection, "LiveSessions") || await HasColumnAsync(dbConnection, "Courses", "AccentColor"));
            if (manuallyProvisionedWitforge)
            {
                logger.LogInformation("Reconciling the pre-migration Witforge schema...");
                var hadPublicationColumn = await HasColumnAsync(dbConnection, "Courses", "IsPublished");
                await EnsureStudioSchemaAsync(dbConnection, logger);
                if (!hadPublicationColumn) await BackfillLegacyPublicationAsync(dbConnection);
                await RotateLegacyShareCodesAsync(dbConnection);
                await StampMigrationHistoryAsync(dbConnection, pending, logger);
            }
            else
            {
                logger.LogInformation("Applying EF Core migrations...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied.");
            }
        }
        else
        {
        long existingPublicTables;
        await using (var checkCmd = dbConnection.CreateCommand())
        {
            checkCmd.CommandText =
                "SELECT count(*) FROM information_schema.tables " +
                "WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";
            existingPublicTables = (long)(await checkCmd.ExecuteScalarAsync())!;
        }

        if (existingPublicTables == 0)
        {
            logger.LogInformation("Public schema is empty; creating application tables...");
            var createScript = db.Database.GenerateCreateScript();
            await using var createCmd = dbConnection.CreateCommand();
            createCmd.CommandText = createScript;
            await createCmd.ExecuteNonQueryAsync();
            logger.LogInformation("Application tables created.");

            // Stamp the migration history so a later MigrateAsync() (or a flip of
            // DB_USE_MIGRATIONS) treats this freshly created schema as current
            // instead of replaying CREATE TABLE statements over existing tables.
            await StampMigrationHistoryAsync(dbConnection, db.Database.GetMigrations(), logger);
        }
        else
        {
            // Legacy explicit non-migration mode keeps its idempotent schema path,
            // but does not claim unapplied migrations in EF history.
            logger.LogInformation("Ensuring additive studio and Witforge schema objects...");
            var hadPublicationColumn = await HasColumnAsync(dbConnection, "Courses", "IsPublished");
            await EnsureStudioSchemaAsync(dbConnection, logger);
            if (!hadPublicationColumn) await BackfillLegacyPublicationAsync(dbConnection);
            await RotateLegacyShareCodesAsync(dbConnection);
        }
        }

        static async Task ExecuteAsync(System.Data.Common.DbConnection connection, string sql)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Idempotent DDL for the Educator Studio schema. Safe to run on any
        /// database state: IF NOT EXISTS everywhere, so nothing collides with
        /// tables/columns that already exist.
        /// </summary>
        static async Task EnsureStudioSchemaAsync(
            System.Data.Common.DbConnection connection,
            Microsoft.Extensions.Logging.ILogger logger)
        {
            // New studio tables.
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "EducatorProjects" (
                    "Id" uuid NOT NULL,
                    "EducatorLearnerId" uuid NOT NULL,
                    "Title" character varying(200) NOT NULL,
                    "Description" character varying(2000) NOT NULL,
                    "Status" character varying(32) NOT NULL,
                    "CourseId" uuid NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_EducatorProjects" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_EducatorProjects_Learners_EducatorLearnerId" FOREIGN KEY ("EducatorLearnerId") REFERENCES "Learners" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_EducatorProjects_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE SET NULL);
                """);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "ProjectSources" (
                    "Id" uuid NOT NULL,
                    "ProjectId" uuid NOT NULL,
                    "Url" character varying(2048) NOT NULL,
                    "Type" character varying(32) NOT NULL,
                    "Title" character varying(300) NOT NULL,
                    "Description" character varying(2000) NULL,
                    "ExtractedContent" text NULL,
                    "Status" character varying(16) NOT NULL,
                    "FailureReason" character varying(1000) NULL,
                    "AddedBy" character varying(8) NOT NULL,
                    "Included" boolean NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_ProjectSources" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_ProjectSources_EducatorProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "EducatorProjects" ("Id") ON DELETE CASCADE);
                """);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "SourceConflicts" (
                    "Id" uuid NOT NULL,
                    "ProjectId" uuid NOT NULL,
                    "Kind" character varying(16) NOT NULL,
                    "SourceIds" character varying(2000) NOT NULL,
                    "Explanation" character varying(4000) NOT NULL,
                    "SuggestedResolutions" text NULL,
                    "Resolution" character varying(16) NULL,
                    "ManualNote" character varying(2000) NULL,
                    "ResolvedBy" character varying(8) NULL,
                    "ResolvedAt" timestamp with time zone NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_SourceConflicts" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_SourceConflicts_EducatorProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "EducatorProjects" ("Id") ON DELETE CASCADE);
                """);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "CourseBlueprints" (
                    "Id" uuid NOT NULL,
                    "ProjectId" uuid NOT NULL,
                    "Json" text NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "ApprovedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_CourseBlueprints" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_CourseBlueprints_EducatorProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "EducatorProjects" ("Id") ON DELETE CASCADE);
                """);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "GenerationJobs" (
                    "Id" uuid NOT NULL,
                    "ProjectId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "ModuleIndex" integer NOT NULL,
                    "ModuleTitle" character varying(300) NOT NULL,
                    "Status" character varying(16) NOT NULL,
                    "Error" character varying(2000) NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "StartedAt" timestamp with time zone NULL,
                    "CompletedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_GenerationJobs" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_GenerationJobs_EducatorProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "EducatorProjects" ("Id") ON DELETE CASCADE);
                """);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "Payments" (
                    "Id" uuid NOT NULL,
                    "LearnerId" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "Amount" numeric(12,2) NOT NULL,
                    "Currency" character varying(8) NOT NULL,
                    "Provider" character varying(32) NOT NULL,
                    "ProviderTxRef" character varying(128) NOT NULL,
                    "Status" character varying(16) NOT NULL,
                    "CheckoutUrl" character varying(2048) NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "CompletedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_Payments_Learners_LearnerId" FOREIGN KEY ("LearnerId") REFERENCES "Learners" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_Payments_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE);
                """);

            // Witforge tables: LiveSessions, Discussions, Assignments, AssignmentSubmissions
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "LiveSessions" (
                    "Id" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "Title" character varying(200) NOT NULL,
                    "Description" character varying(2000) NOT NULL,
                    "StartTime" timestamp with time zone NOT NULL,
                    "DurationMinutes" integer NOT NULL DEFAULT 60,
                    "TimeZone" character varying(64) NOT NULL DEFAULT 'UTC',
                    "MeetingUrl" character varying(2048) NOT NULL DEFAULT '',
                    "Status" character varying(32) NOT NULL DEFAULT 'Scheduled',
                    "CreatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_LiveSessions" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_LiveSessions_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE);
                """);

            await ExecuteAsync(connection, """
                DO $$
                BEGIN
                    IF to_regclass('public."CourseDiscussions"') IS NULL
                       AND to_regclass('public."Discussions"') IS NOT NULL THEN
                        ALTER TABLE "Discussions" RENAME TO "CourseDiscussions";
                    END IF;
                END $$;

                CREATE TABLE IF NOT EXISTS "CourseDiscussions" (
                    "Id" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "LessonId" uuid NULL,
                    "LearnerId" uuid NOT NULL,
                    "ParentId" uuid NULL,
                    "Title" character varying(200) NOT NULL,
                    "Content" text NOT NULL,
                    "IsInstructorReply" boolean NOT NULL DEFAULT false,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_CourseDiscussions" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_CourseDiscussions_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_CourseDiscussions_Lessons_LessonId" FOREIGN KEY ("LessonId") REFERENCES "Lessons" ("Id") ON DELETE SET NULL,
                    CONSTRAINT "FK_CourseDiscussions_Learners_LearnerId" FOREIGN KEY ("LearnerId") REFERENCES "Learners" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_CourseDiscussions_CourseDiscussions_ParentId" FOREIGN KEY ("ParentId") REFERENCES "CourseDiscussions" ("Id") ON DELETE CASCADE);
                """);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "Assignments" (
                    "Id" uuid NOT NULL,
                    "CourseId" uuid NOT NULL,
                    "ModuleId" uuid NULL,
                    "LessonId" uuid NULL,
                    "Title" character varying(200) NOT NULL,
                    "InstructionsMarkdown" text NOT NULL,
                    "DueDaysAfterEnrollment" integer NOT NULL DEFAULT 7,
                    "RubricCriteriaJson" text NOT NULL DEFAULT '[]',
                    "MaxScore" integer NOT NULL DEFAULT 100,
                    "AllowedFileTypes" character varying(256) NOT NULL DEFAULT '.pdf,.zip,.docx,.txt',
                    "MaxFileSizeMb" integer NOT NULL DEFAULT 25,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_Assignments" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_Assignments_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_Assignments_Modules_ModuleId" FOREIGN KEY ("ModuleId") REFERENCES "Modules" ("Id") ON DELETE SET NULL,
                    CONSTRAINT "FK_Assignments_Lessons_LessonId" FOREIGN KEY ("LessonId") REFERENCES "Lessons" ("Id") ON DELETE SET NULL);
                """);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "AssignmentSubmissions" (
                    "Id" uuid NOT NULL,
                    "AssignmentId" uuid NOT NULL,
                    "LearnerId" uuid NOT NULL,
                    "SubmissionText" text NOT NULL,
                    "FileUrl" character varying(2048) NULL,
                    "FileName" character varying(256) NULL,
                    "SubmittedAt" timestamp with time zone NOT NULL,
                    "Status" character varying(32) NOT NULL DEFAULT 'Submitted',
                    "Score" integer NULL,
                    "Feedback" text NULL,
                    "GradedAt" timestamp with time zone NULL,
                    "GradedByEducatorId" uuid NULL,
                    CONSTRAINT "PK_AssignmentSubmissions" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_AssignmentSubmissions_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_AssignmentSubmissions_Learners_LearnerId" FOREIGN KEY ("LearnerId") REFERENCES "Learners" ("Id") ON DELETE CASCADE);
                """);

            // Studio columns on pre-existing tables.
            await ExecuteAsync(connection, """
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "IsPaid" boolean NOT NULL DEFAULT false;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "PriceAmount" numeric(12,2) NOT NULL DEFAULT 0;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Currency" character varying(8) NOT NULL DEFAULT 'ETB';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "ChapaReference" character varying(128) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "CoverImageUrl" character varying(2048) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "CoverImageSource" character varying(16) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "ExternalMaterials" character varying(4000) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "AccentColor" character varying(16) NOT NULL DEFAULT '#1B3D2F';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "ThemeMode" character varying(32) NOT NULL DEFAULT 'pine';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "EnrollmentMode" character varying(32) NOT NULL DEFAULT 'Open';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "PassingScorePercentage" integer NOT NULL DEFAULT 70;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "MaxQuizAttempts" integer NOT NULL DEFAULT 3;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Prerequisites" character varying(2000) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "InstructorBio" text NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "InstructorTitle" character varying(200) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "IsPublished" boolean NOT NULL DEFAULT false;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Visibility" character varying(16) NOT NULL DEFAULT 'Public';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Level" character varying(64) NOT NULL DEFAULT 'Beginner';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Language" character varying(64) NOT NULL DEFAULT 'English';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "LearningObjectives" text[] NOT NULL DEFAULT ARRAY[]::text[];
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "RequirePassingQuizzes" boolean NOT NULL DEFAULT true;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "EnrollmentLimit" integer NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "EnrollmentOpensAt" timestamp with time zone NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "EnrollmentClosesAt" timestamp with time zone NULL;
                ALTER TABLE "Courses" ALTER COLUMN "ThumbnailUrl" TYPE character varying(4096);
                ALTER TABLE "Quizzes" ADD COLUMN IF NOT EXISTS "Kind" integer NOT NULL DEFAULT 0;
                ALTER TABLE "Quizzes" ADD COLUMN IF NOT EXISTS "OrderIndex" integer NOT NULL DEFAULT 0;
                ALTER TABLE "CourseEnrollments" ADD COLUMN IF NOT EXISTS "Status" character varying(16) NOT NULL DEFAULT 'Active';
                """);

            // Indexes (matching the EF configurations).
            await ExecuteAsync(connection, """
                CREATE INDEX IF NOT EXISTS "IX_EducatorProjects_EducatorLearnerId" ON "EducatorProjects" ("EducatorLearnerId");
                CREATE INDEX IF NOT EXISTS "IX_EducatorProjects_Status" ON "EducatorProjects" ("Status");
                CREATE INDEX IF NOT EXISTS "IX_ProjectSources_ProjectId" ON "ProjectSources" ("ProjectId");
                CREATE INDEX IF NOT EXISTS "IX_SourceConflicts_ProjectId" ON "SourceConflicts" ("ProjectId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_CourseBlueprints_ProjectId" ON "CourseBlueprints" ("ProjectId");
                CREATE INDEX IF NOT EXISTS "IX_GenerationJobs_ProjectId" ON "GenerationJobs" ("ProjectId");
                CREATE INDEX IF NOT EXISTS "IX_GenerationJobs_CourseId_ModuleIndex" ON "GenerationJobs" ("CourseId", "ModuleIndex");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Payments_ProviderTxRef" ON "Payments" ("ProviderTxRef");
                CREATE INDEX IF NOT EXISTS "IX_Payments_CourseId_LearnerId" ON "Payments" ("CourseId", "LearnerId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Payments_OneOpenCheckout" ON "Payments" ("CourseId", "LearnerId", "Status") WHERE "Status" IN ('Pending', 'Initializing');
                CREATE INDEX IF NOT EXISTS "IX_LiveSessions_CourseId" ON "LiveSessions" ("CourseId");
                CREATE INDEX IF NOT EXISTS "IX_CourseDiscussions_CourseId" ON "CourseDiscussions" ("CourseId");
                CREATE INDEX IF NOT EXISTS "IX_CourseDiscussions_LearnerId" ON "CourseDiscussions" ("LearnerId");
                CREATE INDEX IF NOT EXISTS "IX_CourseDiscussions_LessonId" ON "CourseDiscussions" ("LessonId");
                CREATE INDEX IF NOT EXISTS "IX_CourseDiscussions_ParentId" ON "CourseDiscussions" ("ParentId");
                CREATE INDEX IF NOT EXISTS "IX_Assignments_CourseId" ON "Assignments" ("CourseId");
                CREATE INDEX IF NOT EXISTS "IX_Assignments_LessonId" ON "Assignments" ("LessonId");
                CREATE INDEX IF NOT EXISTS "IX_Assignments_ModuleId" ON "Assignments" ("ModuleId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssignmentSubmissions_AssignmentId_LearnerId" ON "AssignmentSubmissions" ("AssignmentId", "LearnerId");
                CREATE INDEX IF NOT EXISTS "IX_AssignmentSubmissions_LearnerId" ON "AssignmentSubmissions" ("LearnerId");
                CREATE INDEX IF NOT EXISTS "IX_Courses_IsPublished_Visibility" ON "Courses" ("IsPublished", "Visibility");
                """);

            logger.LogInformation("Studio schema ensured.");
        }

        static Task BackfillLegacyPublicationAsync(System.Data.Common.DbConnection connection) => ExecuteAsync(connection, """
            UPDATE "Courses" SET "IsPublished" = true, "Visibility" = 'Public' WHERE "IsPublic" = true;
            """);

        static Task RotateLegacyShareCodesAsync(System.Data.Common.DbConnection connection) => ExecuteAsync(connection, """
            UPDATE "Courses" SET "ShareCode" = upper(md5("Id"::text || clock_timestamp()::text))
            WHERE length("ShareCode") < 20;
            """);

        static async Task StampMigrationHistoryAsync(
            System.Data.Common.DbConnection connection,
            IEnumerable<string> migrationIds,
            Microsoft.Extensions.Logging.ILogger logger)
        {
            var migrations = migrationIds.ToArray();

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" text NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" text NOT NULL);
                """);

            var efVersion = typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.GetName().Version?.ToString(3) ?? "10.0";
            foreach (var migration in migrations)
            {
                await using var insert = connection.CreateCommand();
                insert.CommandText = """
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    SELECT @id, @version
                    WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = @id);
                    """;
                var id = insert.CreateParameter(); id.ParameterName = "@id"; id.Value = migration; insert.Parameters.Add(id);
                var ver = insert.CreateParameter(); ver.ParameterName = "@version"; ver.Value = efVersion; insert.Parameters.Add(ver);
                await insert.ExecuteNonQueryAsync();
            }
            logger.LogInformation("Migration history stamped ({Count} migration(s)).", migrations.Length);
        }

        var demoDataEnabled = app.Environment.IsDevelopment()
            || builder.Configuration.GetValue<bool>("DemoData:Enabled")
            || string.Equals(builder.Configuration["DEMO_DATA_ENABLED"], "true", StringComparison.OrdinalIgnoreCase);
        if (demoDataEnabled)
        {
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("Seeding development data...");
                await DevSeeder.SeedAsync(db, logger);
            }

            await EntranceExamCourseSeeder.SeedAsync(
                db,
                logger,
                enrollDemoLearner: app.Environment.IsDevelopment());

            await DemoDataSeeder.SeedAsync(db, logger);
        }

        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogError(ex, "An error occurred during database initialization");
        
        // In production, you might want to throw here to prevent startup with a broken database
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// -------------------------------------------------------------------------
// HTTP request pipeline
// -------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Witforge API v1"));
}

app.UseCors(ClientCorsPolicy);

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is AiUnavailableException aiUnavailable)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { code = "ai_unavailable", message = aiUnavailable.Message });
        return;
    }
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new
    {
        code = "server_error",
        message = "The request could not be completed."
    });
}));

app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapLearnerEndpoints();
app.MapIntakeEndpoints();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Witforge.API",
    version = "v1.0",
    database = "PostgreSQL",
    pipelines = new[] { 
        "Model 1: Learning Advisor & Research Orchestrator", 
        "Model 2: Course Architect", 
        "Model 3: JIT Course Builder", 
        "AI Tutor: Socratic Learning Assistant" 
    },
    environment = app.Environment.EnvironmentName,
    timestampUtc = DateTime.UtcNow
}))
    .WithName("GetHealth")
    .WithTags("Health");

app.Run();
