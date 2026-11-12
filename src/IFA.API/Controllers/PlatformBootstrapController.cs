using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/bootstrap")]
    public sealed class PlatformBootstrapController : ControllerBase
    {
        private readonly IApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;

        public PlatformBootstrapController(
            IApplicationDbContext db,
            IConfiguration configuration,
            TimeProvider timeProvider)
        {
            _db = db;
            _configuration = configuration;
            _timeProvider = timeProvider;
        }

        public sealed class BootstrapRequest
        {
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = string.Empty;
            [Required, RegularExpression("^[0-9]{6}$")]
            public string TotpCode { get; set; } = string.Empty;
        }

        [HttpPost("platform-admin")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> BootstrapAdmin(
            [FromBody] BootstrapRequest request,
            CancellationToken cancellationToken)
        {
            if (await _db.AccountRoleGrants.AnyAsync(grant =>
                grant.Role == AccountRoleKind.PlatformAdmin && grant.IsActive && grant.RevokedAt == null,
                cancellationToken))
            {
                return NotFound();
            }

            var configuredEmail = _configuration["ADMIN_BOOTSTRAP_EMAIL"]?.Trim().ToLowerInvariant();
            var configuredTotp = _configuration["ADMIN_BOOTSTRAP_TOTP_SECRET"];
            if (string.IsNullOrWhiteSpace(configuredEmail)
                || string.IsNullOrWhiteSpace(configuredTotp)
                || !request.Email.Trim().Equals(configuredEmail, StringComparison.OrdinalIgnoreCase)
                || !TotpVerifier.IsValid(configuredTotp, request.TotpCode, _timeProvider.GetUtcNow()))
            {
                return Unauthorized(new { message = "Bootstrap credentials are invalid." });
            }

            var account = await _db.Learners.SingleOrDefaultAsync(candidate =>
                candidate.Email == configuredEmail
                && candidate.EmailVerifiedAt != null
                && candidate.DisabledAt == null,
                cancellationToken);
            if (account == null)
            {
                return Conflict(new { message = "Create and verify the configured bootstrap account first." });
            }

            await using var transaction = await CourseAccess.BeginMutationAsync(_db, cancellationToken);
            if (await _db.AccountRoleGrants.AnyAsync(grant =>
                grant.Role == AccountRoleKind.PlatformAdmin && grant.IsActive && grant.RevokedAt == null,
                cancellationToken))
            {
                return Conflict(new { message = "Platform administration is already bootstrapped." });
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            _db.Add(new AccountRoleGrant
            {
                AccountId = account.Id,
                Role = AccountRoleKind.PlatformAdmin,
                GrantedAt = now,
                Reason = "One-time email and TOTP bootstrap"
            });
            if (!await _db.AccountRoleGrants.AnyAsync(grant =>
                grant.AccountId == account.Id && grant.Role == AccountRoleKind.PlatformReviewer,
                cancellationToken))
            {
                _db.Add(new AccountRoleGrant
                {
                    AccountId = account.Id,
                    Role = AccountRoleKind.PlatformReviewer,
                    GrantedAt = now,
                    Reason = "Granted with initial platform administration"
                });
            }
            account.AuthorizationVersion++;
            _db.Add(new AuditEvent
            {
                ActorLearnerId = account.Id,
                Action = "platform.admin_bootstrapped",
                ResourceType = nameof(Learner),
                ResourceId = account.Id.ToString(),
                Outcome = "Succeeded",
                CorrelationId = HttpContext.TraceIdentifier,
                MetadataJson = JsonSerializer.Serialize(new { method = "email_totp" }),
                OccurredAt = now
            });
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return NoContent();
        }
    }
}
