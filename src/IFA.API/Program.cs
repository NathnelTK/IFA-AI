using IFA.API.Endpoints;
using IFA.Infrastructure;
using Microsoft.OpenApi.Models;

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found in configuration.");

builder.Services.AddInfrastructure(connectionString);

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

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    service = "IFA.API",
    timestampUtc = DateTime.UtcNow
}))
    .WithName("GetHealth")
    .WithTags("Health");

app.Run();
