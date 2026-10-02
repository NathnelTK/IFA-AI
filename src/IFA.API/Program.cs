
using IFA.API.Endpoints;
using IFA.Infrastructure;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
        Description = "Backend API for IFA — the AI-powered adaptive learning companion with 3-model pipeline architecture."
    });
});

// JWT Authentication Configuration
var jwtKey = builder.Configuration["JWT_SECRET"] 
    ?? "IFA_SUPER_SECRET_SECURITY_KEY_FOR_HACKATHON_DEMO_2026_LONG_ENOUGH";

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
    ?? new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost:4173", "http://localhost:5000" };

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
        
        // Ensure database is created and migrations are applied
        logger.LogInformation("Ensuring database is created...");
        await db.Database.EnsureCreatedAsync();
        
        // Seed development data if in development environment
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Seeding development data...");
            await DevSeeder.SeedAsync(db, logger);
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

