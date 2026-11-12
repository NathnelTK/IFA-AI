using System.Text.RegularExpressions;

namespace IFA.API.Middleware
{
    public sealed partial class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string ItemName = "CorrelationId";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
            var correlationId = IsValid(supplied) ? supplied! : Guid.NewGuid().ToString("N");
            context.TraceIdentifier = correlationId;
            context.Items[ItemName] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object> { [ItemName] = correlationId }))
            {
                await _next(context);
            }
        }

        private static bool IsValid(string? value) =>
            !string.IsNullOrWhiteSpace(value)
            && value.Length <= 100
            && CorrelationPattern().IsMatch(value);

        [GeneratedRegex("^[A-Za-z0-9._:/-]+$", RegexOptions.CultureInvariant)]
        private static partial Regex CorrelationPattern();
    }
}
