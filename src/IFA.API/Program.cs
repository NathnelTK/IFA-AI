
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
            await StampMigrationHistoryAsync(db, logger);
        }
        else
        {
            // Existing database from before the Educator Studio migration:
            // self-heal by applying pending migrations (safe here because the
            // migration only adds new tables and widens existing columns).
            var hasStudioTables = await HasTableAsync(dbConnection, "GenerationJobs");
            if (!hasStudioTables)
            {
                logger.LogInformation("Pre-studio schema detected; applying EF Core migrations...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied to legacy schema.");
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

        static async Task StampMigrationHistoryAsync(ApplicationDbContext db, Microsoft.Extensions.Logging.ILogger logger)
        {
            var migrations = db.Database.GetMigrations().ToList();
            if (migrations.Count == 0) return;

            await using var conn = (System.Data.Common.DbConnection)db.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" text NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" text NOT NULL);
                """;
            await cmd.ExecuteNonQueryAsync();

            foreach (var migration in migrations)
            {
                await using var insert = conn.CreateCommand();
                insert.CommandText = """
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    SELECT @id, @version
                    WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = @id);
                    """;
                var id = insert.CreateParameter(); id.ParameterName = "@id"; id.Value = migration; insert.Parameters.Add(id);
                var efVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "10.0";
                var ver = insert.CreateParameter(); ver.ParameterName = "@version"; ver.Value = efVersion; insert.Parameters.Add(ver);
                await insert.ExecuteNonQueryAsync();
            }
            logger.LogInformation("Migration history stamped ({Count} migration(s)).", migrations.Count);
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
