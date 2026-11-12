using System.Reflection;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using IFA.API.Endpoints;
using IFA.API.Errors;
using IFA.API.Middleware;
using IFA.API.Authentication;
using IFA.Infrastructure;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

EnvFile.Load();

var builder = WebApplication.CreateBuilder(args);
const string ClientCorsPolicy = "SvelteKitClient";

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
        Description = "Backend API for Witforge learning, course authoring, delivery, and optional AI workflows."
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
            ? value
            : TimeSpan.FromMinutes(1);
        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/429",
            title = "Too many requests",
            status = StatusCodes.Status429TooManyRequests,
            code = "rate_limited",
            detail = $"Try again in {retryAfterSeconds} seconds.",
            retryAfterSeconds,
            traceId = context.HttpContext.TraceIdentifier
        }, cancellationToken);
    };
    options.AddPolicy("auth-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("auth-email", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("generation", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            RateLimitKey(context),
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 10,
                TokensPerPeriod = 5,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("uploads", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            RateLimitKey(context),
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 80,
                TokensPerPeriod = 40,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("payments", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("payment-webhooks", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("notifications", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
var telemetry = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "Witforge.API",
        serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString()))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = context => !context.Request.Path.StartsWithSegments("/api/health/live");
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation(options =>
        {
            // Signed URLs and provider query strings must never enter telemetry.
            options.FilterHttpRequestMessage = request => string.IsNullOrEmpty(request.RequestUri?.Query);
            options.RecordException = true;
        }));
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    telemetry.WithTracing(tracing => tracing.AddOtlpExporter());
}

var jwtKey = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtKey)
    || jwtKey.Length < 32
    || jwtKey.StartsWith("replace_", StringComparison.OrdinalIgnoreCase)
    || jwtKey.StartsWith("IFA_SUPER_SECRET", StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "Set JWT_SECRET to a unique random value of at least 32 characters before starting the API.");
}

var opaqueSessionsEnabled = builder.Configuration.GetValue<bool>("Features:OpaqueSessions");
var defaultAuthScheme = opaqueSessionsEnabled ? AuthSchemes.OpaqueBff : AuthSchemes.LegacyJwt;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = defaultAuthScheme;
    options.DefaultChallengeScheme = defaultAuthScheme;
})
.AddJwtBearer(AuthSchemes.LegacyJwt, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuers =
        [
            builder.Configuration["Auth:Issuer"] ?? builder.Configuration["Jwt:Issuer"] ?? "Witforge.Auth",
            "IFA.API"
        ],
        ValidateAudience = true,
        ValidAudiences =
        [
            builder.Configuration["Auth:LegacyAudience"] ?? builder.Configuration["Jwt:Audience"] ?? "Witforge.LegacyClient",
            "IFA.Client"
        ],
        ClockSkew = TimeSpan.Zero
    };
})
.AddScheme<AuthenticationSchemeOptions, OpaqueBffAuthenticationHandler>(
    AuthSchemes.OpaqueBff,
    _ => { });

var legacyMigrationSetting = builder.Configuration["DB_USE_MIGRATIONS"];
var initializationMode = builder.Configuration["DB_INITIALIZATION_MODE"] ?? "none";
if (!initializationMode.Equals("none", StringComparison.OrdinalIgnoreCase)
    || !string.IsNullOrWhiteSpace(legacyMigrationSetting))
{
    throw new InvalidOperationException(
        "The API no longer mutates database schema. Remove DB_USE_MIGRATIONS, set DB_INITIALIZATION_MODE=none, " +
        "and run the one-shot 'IFA.Worker migrate' release command before starting API or worker processes.");
}

builder.Services.AddInfrastructure(
    DatabaseConnectionResolver.Resolve(builder.Configuration),
    builder.Configuration);

var corsOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"]?.Split(',')
    ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ??
    [
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:3000",
        "http://localhost:4173",
        "http://localhost:5000"
    ])
    .Select(origin => origin.Trim())
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        if (opaqueSessionsEnabled)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();
app.Logger.LogInformation(
    "Starting Witforge API. Environment={Environment} BuildSha={BuildSha} DatabaseProvider={DatabaseProvider} OpaqueSessions={OpaqueSessions} Workspaces={Workspaces} UnifiedAuthoring={UnifiedAuthoring} AssetUploads={AssetUploads} ImmutableReleases={ImmutableReleases}",
    app.Environment.EnvironmentName,
    builder.Configuration["BUILD_SHA"] ?? builder.Configuration["RENDER_GIT_COMMIT"] ?? "local",
    builder.Configuration["DB_PROVIDER"] ?? "postgres",
    opaqueSessionsEnabled,
    builder.Configuration.GetValue<bool>("Features:Workspaces"),
    builder.Configuration.GetValue<bool>("Features:UnifiedAuthoring"),
    builder.Configuration.GetValue<bool>("Features:AssetUploads"),
    builder.Configuration.GetValue<bool>("Features:ImmutableReleases"));

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseMiddleware<DatabaseSchemaGuardMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Witforge API v1"));
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(ClientCorsPolicy);
app.UseAuthentication();
if (!opaqueSessionsEnabled)
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Headers.ContainsKey(OpaqueBffAuthenticationHandler.SignatureHeader))
        {
            var envelope = await context.AuthenticateAsync(AuthSchemes.OpaqueBff);
            if (envelope.Failure != null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "invalid_bff_envelope",
                    message = "The internal request signature is invalid."
                }, context.RequestAborted);
                return;
            }
        }
        await next();
    });
}
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapLearnerEndpoints();
app.MapIntakeEndpoints();

app.MapGet("/api/health/live", (TimeProvider timeProvider) => Results.Ok(new
{
    status = "healthy",
    service = "Witforge.API",
    timestampUtc = timeProvider.GetUtcNow()
}))
    .WithName("GetLiveness")
    .WithTags("Health");

app.MapGet("/api/health", async (
    ApplicationDbContext db,
    TimeProvider timeProvider,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return Results.Json(new { status = "unavailable", code = "database_unavailable" }, statusCode: 503);
        }

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pending.Length > 0)
        {
            return Results.Json(new
            {
                status = "unavailable",
                code = "database_migrations_pending",
                pendingMigrations = pending
            }, statusCode: 503);
        }

        return Results.Ok(new
        {
            status = "healthy",
            service = "Witforge.API",
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            buildSha = configuration["BUILD_SHA"] ?? configuration["RENDER_GIT_COMMIT"],
            database = db.Database.ProviderName,
            environment = app.Environment.EnvironmentName,
            timestampUtc = timeProvider.GetUtcNow()
        });
    }
    catch (Exception)
    {
        return Results.Json(new { status = "unavailable", code = "database_readiness_failed" }, statusCode: 503);
    }
})
    .WithName("GetReadiness")
    .WithTags("Health");

app.Run();

static string RateLimitKey(HttpContext context)
{
    if (context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value is { Length: > 0 } accountId)
    {
        return $"account:{accountId}";
    }
    if (context.Items[OpaqueBffAuthenticationHandler.ClientKeyItem]?.ToString() is { Length: > 0 } clientKey)
    {
        return $"client:{clientKey}";
    }
    return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

public partial class Program;
