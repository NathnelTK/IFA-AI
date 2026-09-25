
using IFA.API.Endpoints;
using IFA.Infrastructure;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.OpenApi;

// Load local secrets (see .env.example) before configuration is built so the
// PostgreSQL password never has to be committed. Missing file is a no-op.
EnvFile.Load();

var builder = WebApplication.CreateBuilder(args);

const string ClientCorsPolicy = "SvelteKitClient";

// -------------------------------------------------------------------------
// Service registration
// -------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IFA API",
        Version = "v1",
        Description = "Backend API for IFA — the AI-powered adaptive learning companion."
    });
});

// A fully-formed connection string can still be injected through the
// ConnectionStrings__DefaultConnection environment variable. Otherwise the
// string is composed from the Database section plus the DB_PASSWORD secret.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    var database = builder.Configuration.GetSection("Database");

    connectionString = PostgresConnectionString.Create(
        host: database["Host"] ?? "localhost",
        port: database.GetValue("Port", 5432),
        database: database["Name"] ?? "ifa",
        username: database["User"] ?? "ifa",
        password: builder.Configuration["DB_PASSWORD"]
            ?? database["Password"]
            ?? string.Empty);
}

builder.Services.AddInfrastructure(connectionString, builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// -------------------------------------------------------------------------
// Development seed data
// -------------------------------------------------------------------------
// Creates the fixed demo learner used by Swagger/local development.
// This does NOT modify the database schema; it only inserts the demo row
// if it does not already exist.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DevSeeder");

    await DevSeeder.SeedAsync(db, logger);
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapLearnerEndpoints();
app.MapLearnerEndpoints();
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    service = "IFA.API",
    timestampUtc = DateTime.UtcNow
}))
    .WithName("GetHealth")
    .WithTags("Health");

app.Run();

