
using IFA.API.Endpoints;
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
        Title = "IFA API",
        Version = "v1",
        Description = "Backend API for IFA — the AI-powered adaptive learning companion with 3-model pipeline architecture."
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
        var useMigrations = string.Equals(
            builder.Configuration["DB_USE_MIGRATIONS"] ?? Environment.GetEnvironmentVariable("DB_USE_MIGRATIONS"),
            "true", StringComparison.OrdinalIgnoreCase);

        var dbConnection = db.Database.GetDbConnection();
        await dbConnection.OpenAsync();

        if (useMigrations)
        {
            logger.LogInformation("Applying EF Core migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrations applied.");
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
            await StampMigrationHistoryAsync(dbConnection, logger);
        }
        else
        {
            // Existing database that predates the Educator Studio. The DB was
            // created by GenerateCreateScript (no migration history), so
            // replaying old migrations would collide with existing tables.
            // Instead, apply idempotent DDL for exactly the studio objects,
            // then stamp the migration history so MigrateAsync stays a no-op.
            var hasStudioTables = await HasTableAsync(dbConnection, "GenerationJobs");
            if (!hasStudioTables)
            {
                logger.LogInformation("Pre-studio schema detected; adding studio tables and columns...");
                await EnsureStudioSchemaAsync(dbConnection, logger);
            }
        }
        }

        static async Task<bool> HasTableAsync(System.Data.Common.DbConnection connection, string tableName)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT count(*) FROM information_schema.tables " +
                "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' AND table_name = @name";
            var p = cmd.CreateParameter();
            p.ParameterName = "@name";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            return (long)(await cmd.ExecuteScalarAsync())! > 0;
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
        static async Task EnsureStudioSchemaAsync(System.Data.Common.DbConnection connection, Microsoft.Extensions.Logging.ILogger logger)
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

            // Studio columns on pre-existing tables.
            await ExecuteAsync(connection, """
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "IsPaid" boolean NOT NULL DEFAULT false;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "PriceAmount" numeric(12,2) NOT NULL DEFAULT 0;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Currency" character varying(8) NOT NULL DEFAULT 'ETB';
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "ChapaReference" character varying(128) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "CoverImageUrl" character varying(2048) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "CoverImageSource" character varying(16) NULL;
                ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "ExternalMaterials" character varying(4000) NULL;
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
                """);

            logger.LogInformation("Studio schema ensured.");

            // Stamp the full migration history: the schema now matches the
            // current model, so any later MigrateAsync() (or a flip of
            // DB_USE_MIGRATIONS) becomes a no-op instead of replaying DDL.
            await StampMigrationHistoryAsync(connection, logger);
        }

        static async Task StampMigrationHistoryAsync(System.Data.Common.DbConnection connection, Microsoft.Extensions.Logging.ILogger logger)
        {
            var migrations = new[]
            {
                "20260917132857_InitialCreate",
                "20260919144707_FixCompletionTrackingAndAddProfileContracts",
                "20260921065838_ResolvePostMergeModelChanges",
                "20260925081248_ExternalIdToAcademicEvidence",
                "20260926090335_AddChatSessionAndLlmFoundation",
                "20261002191535_RefactorPerLearnerStateAndProfileContracts",
                "20261018121500_AddEducatorStudioAndPayments"
            };

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

        // Seed development data if in development environment
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Seeding development data...");
            await DevSeeder.SeedAsync(db, logger);
        }

        await EntranceExamCourseSeeder.SeedAsync(
            db,
            logger,
            enrollDemoLearner: app.Environment.IsDevelopment());

        // Demo accounts + public course catalog. Idempotent, so safe to run on
        // every startup.
        await DemoDataSeeder.SeedAsync(db, logger);

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IFA API v1"));
}

app.UseCors(ClientCorsPolicy);

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
    service = "IFA.API",
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
