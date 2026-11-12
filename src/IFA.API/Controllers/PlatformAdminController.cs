using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/platform-admin")]
    public sealed class PlatformAdminController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly TimeProvider _timeProvider;

        public PlatformAdminController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            TimeProvider timeProvider)
        {
            _db = db;
            _authorization = authorization;
            _timeProvider = timeProvider;
        }

        public sealed class RoleChangeRequest
        {
            [Required, RegularExpression("^(PlatformReviewer|PlatformModerator|PlatformAdmin)$")]
            public string Role { get; set; } = string.Empty;
            [Required, StringLength(500)]
            public string Reason { get; set; } = string.Empty;
        }

        public sealed class SuspensionRequest
        {
            [Required, StringLength(500)]
            public string Reason { get; set; } = string.Empty;
        }

        [HttpPut("accounts/{accountId:guid}/roles")]
        public async Task<IActionResult> GrantRole(
            Guid accountId,
            [FromBody] RoleChangeRequest request,
            CancellationToken cancellationToken)
        {
            var actorId = await RequireStepUpAdminAsync(cancellationToken);
            if (!actorId.HasValue)
            {
                return Forbid();
            }
            if (!Enum.TryParse<AccountRoleKind>(request.Role, out var role))
            {
                return BadRequest();
            }
            var account = await _db.Learners.SingleOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken);
            if (account == null)
            {
                return NotFound();
            }

            var now = UtcNow();
            var grant = await _db.AccountRoleGrants.SingleOrDefaultAsync(candidate =>
                candidate.AccountId == accountId && candidate.Role == role,
                cancellationToken);
            if (grant == null)
            {
                grant = new AccountRoleGrant { AccountId = accountId, Role = role };
                _db.Add(grant);
            }
            grant.IsActive = true;
            grant.GrantedAt = now;
            grant.GrantedByAccountId = actorId;
            grant.RevokedAt = null;
            grant.RevokedByAccountId = null;
            grant.Reason = request.Reason.Trim();
            account.AuthorizationVersion++;
            AddAudit(actorId.Value, "account_role.granted", accountId, role, request.Reason, now);
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpDelete("accounts/{accountId:guid}/roles")]
        public async Task<IActionResult> RevokeRole(
            Guid accountId,
            [FromBody] RoleChangeRequest request,
            CancellationToken cancellationToken)
        {
            var actorId = await RequireStepUpAdminAsync(cancellationToken);
            if (!actorId.HasValue)
            {
                return Forbid();
            }
            if (!Enum.TryParse<AccountRoleKind>(request.Role, out var role))
            {
                return BadRequest();
            }
            if (role == AccountRoleKind.PlatformAdmin
                && accountId == actorId.Value
                && await _db.AccountRoleGrants.CountAsync(grant =>
                    grant.Role == AccountRoleKind.PlatformAdmin && grant.IsActive && grant.RevokedAt == null,
                    cancellationToken) == 1)
            {
                return Conflict(new { message = "Grant another PlatformAdmin before revoking the last active administrator." });
            }

            var grant = await _db.AccountRoleGrants.SingleOrDefaultAsync(candidate =>
                candidate.AccountId == accountId && candidate.Role == role,
                cancellationToken);
            if (grant == null || !grant.IsActive)
            {
                return NoContent();
            }
            var account = await _db.Learners.SingleAsync(candidate => candidate.Id == accountId, cancellationToken);
            var now = UtcNow();
            grant.IsActive = false;
            grant.RevokedAt = now;
            grant.RevokedByAccountId = actorId;
            grant.Reason = request.Reason.Trim();
            account.AuthorizationVersion++;
            AddAudit(actorId.Value, "account_role.revoked", accountId, role, request.Reason, now);
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("workspaces/{workspaceId:guid}/suspend")]
        public async Task<IActionResult> SuspendWorkspace(
            Guid workspaceId,
            [FromBody] SuspensionRequest request,
            CancellationToken cancellationToken)
        {
            var actorId = await RequireStepUpAdminAsync(cancellationToken);
            if (!actorId.HasValue)
            {
                return Forbid();
            }
            var workspace = await _db.EducatorWorkspaces.SingleOrDefaultAsync(candidate => candidate.Id == workspaceId, cancellationToken);
            if (workspace == null)
            {
                return NotFound();
            }
            var now = UtcNow();
            workspace.Status = EducatorWorkspaceStatus.Suspended;
            workspace.SuspendedAt = now;
            workspace.SuspensionReason = request.Reason.Trim();
            workspace.AuthorizationVersion++;
            var memberIds = await _db.WorkspaceMemberships
                .Where(membership => membership.WorkspaceId == workspaceId)
                .Select(membership => membership.AccountId)
                .ToListAsync(cancellationToken);
            var accounts = await _db.Learners.Where(account => memberIds.Contains(account.Id)).ToListAsync(cancellationToken);
            foreach (var account in accounts)
            {
                account.AuthorizationVersion++;
            }
            AddAudit(actorId.Value, "workspace.suspended", workspaceId, null, request.Reason, now);
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private async Task<Guid?> RequireStepUpAdminAsync(CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            return await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.PlatformAdmin, cancellationToken)
                && int.TryParse(User.FindFirst("mfa_level")?.Value, out var mfaLevel)
                && mfaLevel >= 1
                ? accountId
                : null;
        }

        private void AddAudit(
            Guid actorId,
            string action,
            Guid resourceId,
            AccountRoleKind? role,
            string reason,
            DateTime now)
        {
            _db.Add(new AuditEvent
            {
                ActorLearnerId = actorId,
                Action = action,
                ResourceType = role.HasValue ? nameof(AccountRoleGrant) : nameof(EducatorWorkspace),
                ResourceId = resourceId.ToString(),
                Outcome = "Succeeded",
                CorrelationId = HttpContext.TraceIdentifier,
                MetadataJson = JsonSerializer.Serialize(new { role, reason }),
                OccurredAt = now
            });
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
