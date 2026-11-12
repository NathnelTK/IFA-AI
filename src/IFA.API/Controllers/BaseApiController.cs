using System;
using System.Security.Claims;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IFA.API.Controllers
{
    public abstract class BaseApiController : ControllerBase, IAsyncActionFilter
    {
        protected bool IsCurrentUserInRole(string role)
        {
            if (User.Identity?.IsAuthenticated != true) return false;
            return User.IsInRole(role);
        }

        protected async Task<bool> HasCurrentUserRoleAsync(
            IApplicationDbContext context,
            AccountRoleKind role,
            CancellationToken cancellationToken = default)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (User.Identity?.IsAuthenticated != true || !Guid.TryParse(claim?.Value, out var accountId))
            {
                return false;
            }

            var authorization = HttpContext.RequestServices.GetRequiredService<IPlatformAuthorizationService>();
            return await authorization.HasAccountRoleAsync(accountId, role, cancellationToken);
        }

        protected async Task<Guid> GetCurrentLearnerIdAsync(IApplicationDbContext context)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (User.Identity?.IsAuthenticated == true && Guid.TryParse(claim?.Value, out var id) &&
                id != Guid.Empty && await context.Learners.AnyAsync(l => l.Id == id, HttpContext.RequestAborted))
            {
                return id;
            }

            throw new CourseAccessException(401, "Sign in with a valid learner account.");
        }

        protected async Task<Guid?> GetOptionalLearnerIdAsync(IApplicationDbContext context) =>
            User.Identity?.IsAuthenticated == true ? await GetCurrentLearnerIdAsync(context) : null;

        async Task IAsyncActionFilter.OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();
            if (executed.Exception is CourseAccessException access)
            {
                executed.Result = StatusCode(access.StatusCode, new { message = access.Message });
                executed.ExceptionHandled = true;
            }
            else if (executed.Exception is DbUpdateConcurrencyException ||
                     executed.Exception is PostgresException { SqlState: "40001" or "40P01" } ||
                     executed.Exception is DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "40001" or "40P01" } })
            {
                executed.Result = Conflict(new { message = "Another request changed this record. Refresh and retry the action." });
                executed.ExceptionHandled = true;
            }
        }
    }
}
