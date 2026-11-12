using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Middleware
{
    public sealed class DatabaseSchemaGuardMiddleware
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(15);
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private DateTimeOffset _nextRefreshAt = DateTimeOffset.MinValue;
        private string[] _pending = [];

        public DatabaseSchemaGuardMiddleware(
            RequestDelegate next,
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider)
        {
            _next = next;
            _scopeFactory = scopeFactory;
            _timeProvider = timeProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.StartsWithSegments("/api/health/live"))
            {
                await _next(context);
                return;
            }

            await RefreshAsync(context.RequestAborted);
            if (_pending.Length == 0)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://witforge.com/problems/database-migrations-pending",
                title = "Database migration required",
                status = StatusCodes.Status503ServiceUnavailable,
                code = "database_migrations_pending",
                detail = "Run the one-shot migration owner before using the API.",
                command = "dotnet run --project src/IFA.Worker -- migrate",
                pendingMigrations = _pending,
                correlationId = context.TraceIdentifier
            }, context.RequestAborted);
        }

        private async Task RefreshAsync(CancellationToken cancellationToken)
        {
            var now = _timeProvider.GetUtcNow();
            if (now < _nextRefreshAt) return;
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                now = _timeProvider.GetUtcNow();
                if (now < _nextRefreshAt) return;
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                _pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
                _nextRefreshAt = now.Add(RefreshInterval);
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }
}
