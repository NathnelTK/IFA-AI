using IFA.API.Middleware;
using IFA.Application.Common.Exceptions;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Errors
{
    public sealed class ApiExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetails;
        private readonly ILogger<ApiExceptionHandler> _logger;

        public ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
        {
            _problemDetails = problemDetails;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (status, code, title, detail) = exception switch
            {
                AiUnavailableException ai => (
                    StatusCodes.Status503ServiceUnavailable,
                    "ai_unavailable",
                    "AI service unavailable",
                    ai.Message),
                CourseAccessException access => (
                    access.StatusCode,
                    "course_access_denied",
                    "Course access denied",
                    access.Message),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "server_error",
                    "The request could not be completed",
                    "An unexpected error occurred.")
            };

            if (status >= 500)
            {
                _logger.LogError(exception, "Unhandled API exception with error code {ErrorCode}.", code);
            }
            else
            {
                _logger.LogWarning(exception, "API request failed with error code {ErrorCode}.", code);
            }

            httpContext.Response.StatusCode = status;
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };
            problem.Extensions["code"] = code;
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;
            problem.Extensions["correlationId"] =
                httpContext.Items[CorrelationIdMiddleware.ItemName]?.ToString() ?? httpContext.TraceIdentifier;

            return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }
    }
}
