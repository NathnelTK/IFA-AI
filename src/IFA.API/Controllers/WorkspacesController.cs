using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/workspaces")]
    public sealed partial class WorkspacesController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly TimeProvider _timeProvider;
        private readonly IApplicationSecretProtector _protector;

        public WorkspacesController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            TimeProvider timeProvider,
            IApplicationSecretProtector protector)
        {
            _db = db;
            _authorization = authorization;
            _timeProvider = timeProvider;
            _protector = protector;
        }

        public sealed class CreateApplicationRequest
        {
            [Required, StringLength(160)]
            public string DisplayName { get; set; } = string.Empty;
            [StringLength(200)]
            public string? Title { get; set; }
            [StringLength(4000)]
            public string? Bio { get; set; }
            [StringLength(20_000)]
            public string EvidenceJson { get; set; } = "[]";
        }

        public sealed class ApplicationDecisionRequest
        {
            [Required, RegularExpression("^(Approved|Rejected)$")]
            public string Decision { get; set; } = string.Empty;
            [Required, StringLength(1000)]
            public string Reason { get; set; } = string.Empty;
        }

        public sealed class InviteMemberRequest
        {
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = string.Empty;
            [Required, RegularExpression("^(Admin|Instructor)$")]
            public string Role { get; set; } = "Instructor";
        }

        public sealed class AcceptInvitationRequest
        {
            [Required, StringLength(500)]
            public string Token { get; set; } = string.Empty;
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var workspaces = await _db.WorkspaceMemberships
                .Where(membership => membership.AccountId == accountId
                    && membership.State != WorkspaceMembershipState.Revoked)
                .Join(_db.EducatorWorkspaces,
                    membership => membership.WorkspaceId,
                    workspace => workspace.Id,
                    (membership, workspace) => new
                    {
                        workspace.Id,
                        workspace.Name,
                        workspace.Slug,
                        workspace.Type,
                        workspace.Status,
                        workspace.AuthorizationVersion,
                        membershipId = membership.Id,
                        membership.Role,
                        membership.State
                    })
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
            return Ok(workspaces);
        }

        [HttpPost("applications/individual")]
        public async Task<IActionResult> CreateIndividualApplication(
            [FromBody] CreateApplicationRequest request,
            CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var account = await _db.Learners.SingleAsync(candidate => candidate.Id == accountId, cancellationToken);
            var alreadyExists = await _db.WorkspaceMemberships
                .Where(membership => membership.AccountId == accountId
                    && membership.State != WorkspaceMembershipState.Revoked)
                .Join(_db.EducatorWorkspaces,
                    membership => membership.WorkspaceId,
                    workspace => workspace.Id,
                    (_, workspace) => workspace.Type)
                .AnyAsync(type => type == EducatorWorkspaceType.Individual, cancellationToken);
            if (alreadyExists)
            {
                return Conflict(new { code = "individual_workspace_exists", message = "This account already has an individual educator workspace." });
            }
            if (!ValidJsonArray(request.EvidenceJson))
            {
                return BadRequest(new { message = "Evidence must be a JSON array." });
            }

            var now = UtcNow();
            var workspace = new EducatorWorkspace
            {
                Type = EducatorWorkspaceType.Individual,
                Status = EducatorWorkspaceStatus.Draft,
                Name = request.DisplayName.Trim(),
                CreatedByAccountId = accountId,
                CreatedAt = now,
                UpdatedAt = now
            };
            workspace.Slug = Slug(request.DisplayName, workspace.Id);
            var membership = new WorkspaceMembership
            {
                WorkspaceId = workspace.Id,
                AccountId = accountId,
                Role = WorkspaceMembershipRole.Owner,
                State = WorkspaceMembershipState.Active,
                CreatedAt = now,
                AcceptedAt = now
            };
            var profile = new WorkspaceInstructorProfile
            {
                MembershipId = membership.Id,
                DisplayName = request.DisplayName.Trim(),
                Title = request.Title?.Trim(),
                Bio = request.Bio?.Trim(),
                UpdatedAt = now
            };
            var application = new EducatorApplication
            {
                WorkspaceId = workspace.Id,
                ApplicantAccountId = accountId,
                Status = "Draft",
                EvidenceJson = request.EvidenceJson,
                CreatedAt = now
            };
            _db.Add(workspace);
            _db.Add(membership);
            _db.Add(profile);
            _db.Add(application);
            account.RegistrationIntent = "Educator";
            account.AuthorizationVersion++;
            _db.Add(new AuditEvent
            {
                ActorLearnerId = accountId,
                Action = "educator_application.created",
                ResourceType = nameof(EducatorApplication),
                ResourceId = application.Id.ToString(),
                Outcome = "Draft",
                CorrelationId = HttpContext.TraceIdentifier,
                OccurredAt = now
            });
            await _db.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, new
            {
                application.Id,
                application.Status,
                workspaceId = workspace.Id,
                workspace.Slug
            });
        }

        [HttpGet("applications/{id:guid}")]
        public async Task<IActionResult> GetApplication(Guid id, CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var canReview = await CanReviewAsync(accountId, cancellationToken);
            var application = await _db.EducatorApplications.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (application == null)
            {
                return NotFound();
            }
            if (application.ApplicantAccountId != accountId && !canReview)
            {
                return Forbid();
            }
            return Ok(application);
        }

        [HttpGet("applications/mine")]
        public async Task<IActionResult> GetMyApplication(CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var application = await _db.EducatorApplications
                .Where(candidate => candidate.ApplicantAccountId == accountId)
                .OrderByDescending(candidate => candidate.CreatedAt)
                .Join(_db.EducatorWorkspaces,
                    candidate => candidate.WorkspaceId,
                    workspace => workspace.Id,
                    (candidate, workspace) => new
                    {
                        candidate.Id,
                        candidate.Status,
                        candidate.CreatedAt,
                        candidate.SubmittedAt,
                        candidate.DecidedAt,
                        candidate.DecisionReason,
                        workspaceId = workspace.Id,
                        workspace.Name,
                        workspace.Type
                    })
                .FirstOrDefaultAsync(cancellationToken);
            return application == null ? NoContent() : Ok(application);
        }

        [HttpPost("applications/organization")]
        public async Task<IActionResult> CreateOrganizationApplication(
            [FromBody] CreateApplicationRequest request,
            CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var account = await _db.Learners.SingleAsync(candidate => candidate.Id == accountId, cancellationToken);
            if (!ValidJsonArray(request.EvidenceJson))
            {
                return BadRequest(new { message = "Evidence must be a JSON array." });
            }
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return BadRequest(new { message = "Organization name is required." });
            }

            var now = UtcNow();
            var workspace = new EducatorWorkspace
            {
                Type = EducatorWorkspaceType.Organization,
                Status = EducatorWorkspaceStatus.Draft,
                Name = request.DisplayName.Trim(),
                CreatedByAccountId = accountId,
                CreatedAt = now,
                UpdatedAt = now
            };
            workspace.Slug = Slug(request.DisplayName, workspace.Id);
            var membership = new WorkspaceMembership
            {
                WorkspaceId = workspace.Id,
                AccountId = accountId,
                Role = WorkspaceMembershipRole.Owner,
                State = WorkspaceMembershipState.Active,
                CreatedAt = now,
                AcceptedAt = now
            };
            var application = new EducatorApplication
            {
                WorkspaceId = workspace.Id,
                ApplicantAccountId = accountId,
                Status = "Draft",
                EvidenceJson = request.EvidenceJson,
                CreatedAt = now
            };
            _db.Add(workspace);
            _db.Add(membership);
            _db.Add(new WorkspaceInstructorProfile
            {
                MembershipId = membership.Id,
                DisplayName = request.DisplayName.Trim(),
                Title = request.Title?.Trim(),
                Bio = request.Bio?.Trim(),
                UpdatedAt = now
            });
            _db.Add(application);
            account.RegistrationIntent = "Educator";
            account.AuthorizationVersion++;
            await _db.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, new
            {
                application.Id,
                application.Status,
                workspaceId = workspace.Id,
                workspace.Slug
            });
        }

        [HttpPost("applications/{id:guid}/submit")]
        public async Task<IActionResult> SubmitApplication(Guid id, CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            var application = await _db.EducatorApplications.SingleOrDefaultAsync(candidate =>
                candidate.Id == id && candidate.ApplicantAccountId == accountId,
                cancellationToken);
            if (application == null)
            {
                return NotFound();
            }
            if (application.Status != "Draft")
            {
                return Conflict(new { message = "Only a draft application can be submitted." });
            }

            var now = UtcNow();
            application.Status = "PendingReview";
            application.SubmittedAt = now;
            var workspace = await _db.EducatorWorkspaces.SingleAsync(candidate => candidate.Id == application.WorkspaceId, cancellationToken);
            workspace.Status = EducatorWorkspaceStatus.PendingReview;
            workspace.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { application.Id, application.Status, application.SubmittedAt });
        }

        [HttpGet("applications/review")]
        public async Task<IActionResult> ReviewQueue(CancellationToken cancellationToken)
        {
            var reviewerId = await GetCurrentLearnerIdAsync(_db);
            if (!await CanReviewAsync(reviewerId, cancellationToken))
            {
                return Forbid();
            }
            var queue = await _db.EducatorApplications
                .Where(application => application.Status == "PendingReview")
                .OrderBy(application => application.SubmittedAt)
                .ToListAsync(cancellationToken);
            return Ok(queue);
        }

        [HttpPost("applications/{id:guid}/decision")]
        public async Task<IActionResult> DecideApplication(
            Guid id,
            [FromBody] ApplicationDecisionRequest request,
            CancellationToken cancellationToken)
        {
            var reviewerId = await GetCurrentLearnerIdAsync(_db);
            if (!await CanReviewAsync(reviewerId, cancellationToken))
            {
                return Forbid();
            }

            await using var transaction = await CourseAccess.BeginMutationAsync(_db, cancellationToken);
            var application = await _db.EducatorApplications.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (application == null)
            {
                return NotFound();
            }
            if (application.Status != "PendingReview")
            {
                return Conflict(new { message = "This application has already been decided." });
            }

            var now = UtcNow();
            var approved = request.Decision == "Approved";
            application.Status = request.Decision;
            application.ReviewerAccountId = reviewerId;
            application.DecidedAt = now;
            application.DecisionReason = request.Reason.Trim();
            var workspace = await _db.EducatorWorkspaces.SingleAsync(candidate => candidate.Id == application.WorkspaceId, cancellationToken);
            workspace.Status = approved ? EducatorWorkspaceStatus.Verified : EducatorWorkspaceStatus.Draft;
            workspace.UpdatedAt = now;
            workspace.AuthorizationVersion++;

            if (approved)
            {
                var grant = await _db.AccountRoleGrants.SingleOrDefaultAsync(candidate =>
                    candidate.AccountId == application.ApplicantAccountId
                    && candidate.Role == AccountRoleKind.Educator,
                    cancellationToken);
                if (grant == null)
                {
                    _db.Add(new AccountRoleGrant
                    {
                        AccountId = application.ApplicantAccountId,
                        Role = AccountRoleKind.Educator,
                        GrantedByAccountId = reviewerId,
                        GrantedAt = now
                    });
                }
                else
                {
                    grant.IsActive = true;
                    grant.RevokedAt = null;
                    grant.RevokedByAccountId = null;
                    grant.GrantedByAccountId = reviewerId;
                    grant.GrantedAt = now;
                }
                var account = await _db.Learners.SingleAsync(candidate => candidate.Id == application.ApplicantAccountId, cancellationToken);
                account.Role = "Educator";
                account.RegistrationIntent = "Educator";
                account.AuthorizationVersion++;
            }

            _db.Add(new AuditEvent
            {
                ActorLearnerId = reviewerId,
                Action = approved ? "educator_application.approved" : "educator_application.rejected",
                ResourceType = nameof(EducatorApplication),
                ResourceId = application.Id.ToString(),
                Outcome = application.Status,
                CorrelationId = HttpContext.TraceIdentifier,
                OccurredAt = now,
                MetadataJson = JsonSerializer.Serialize(new { reason = request.Reason })
            });
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return Ok(new { application.Id, application.Status, workspaceStatus = workspace.Status });
        }

        [HttpPost("{workspaceId:guid}/invitations")]
        [EnableRateLimiting("notifications")]
        public async Task<IActionResult> InviteMember(
            Guid workspaceId,
            [FromBody] InviteMemberRequest request,
            CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.CanManageWorkspaceAsync(accountId, workspaceId, cancellationToken))
            {
                return Forbid();
            }
            var workspace = await _db.EducatorWorkspaces.SingleOrDefaultAsync(candidate => candidate.Id == workspaceId, cancellationToken);
            if (workspace == null)
            {
                return NotFound();
            }
            if (workspace.Type != EducatorWorkspaceType.Organization)
            {
                return Conflict(new { message = "Individual workspaces cannot add team members." });
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var existingAccountId = await _db.Learners
                .Where(account => account.Email == email)
                .Select(account => (Guid?)account.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (existingAccountId.HasValue
                && await _db.WorkspaceMemberships.AnyAsync(membership =>
                    membership.WorkspaceId == workspaceId
                    && membership.AccountId == existingAccountId.Value
                    && membership.State != WorkspaceMembershipState.Revoked,
                    cancellationToken))
            {
                return Conflict(new { message = "This account already belongs to the workspace." });
            }

            var rawToken = RandomNumberGenerator.GetBytes(32);
            var now = UtcNow();
            var invitation = new WorkspaceInvitation
            {
                WorkspaceId = workspaceId,
                Email = email,
                Role = Enum.Parse<WorkspaceMembershipRole>(request.Role),
                TokenHash = SHA256.HashData(rawToken),
                InvitedByAccountId = accountId,
                CreatedAt = now,
                ExpiresAt = now.AddDays(7)
            };
            var token = WebEncoders.Base64UrlEncode(rawToken);
            _db.Add(invitation);
            _db.Add(new OutboxMessage
            {
                Type = "workspace.invitation.email",
                AggregateType = nameof(EducatorWorkspace),
                AggregateId = workspaceId,
                DeduplicationKey = $"workspace-invite:{invitation.Id}",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    invitationId = invitation.Id,
                    email,
                    workspaceName = workspace.Name,
                    protectedToken = _protector.Protect(token),
                    invitation.ExpiresAt
                }),
                OccurredAt = now,
                AvailableAt = now,
                ActorLearnerId = accountId,
                CorrelationId = HttpContext.TraceIdentifier
            });
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { invitation.Id, invitation.Email, invitation.Role, invitation.ExpiresAt });
        }

        [HttpGet("{workspaceId:guid}/members")]
        public async Task<IActionResult> Members(Guid workspaceId, CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.CanManageWorkspaceAsync(accountId, workspaceId, cancellationToken)) return Forbid();
            var members = await _db.WorkspaceMemberships
                .Where(membership => membership.WorkspaceId == workspaceId
                    && membership.State != WorkspaceMembershipState.Revoked)
                .Join(_db.Learners,
                    membership => membership.AccountId,
                    account => account.Id,
                    (membership, account) => new
                    {
                        membership.Id,
                        membership.AccountId,
                        membership.Role,
                        membership.State,
                        membership.AcceptedAt,
                        account.Name,
                        account.Email,
                        account.AvatarUrl
                    })
                .OrderBy(member => member.Role)
                .ThenBy(member => member.Name)
                .ToListAsync(cancellationToken);
            var pendingInvitations = await _db.WorkspaceInvitations
                .Where(invitation => invitation.WorkspaceId == workspaceId
                    && invitation.AcceptedAt == null
                    && invitation.RevokedAt == null
                    && invitation.ExpiresAt > UtcNow())
                .Select(invitation => new
                {
                    invitation.Id,
                    invitation.Email,
                    invitation.Role,
                    invitation.CreatedAt,
                    invitation.ExpiresAt
                })
                .ToListAsync(cancellationToken);
            return Ok(new { members, pendingInvitations });
        }

        [HttpPost("invitations/accept")]
        public async Task<IActionResult> AcceptInvitation(
            [FromBody] AcceptInvitationRequest request,
            CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!TryHashToken(request.Token, out var tokenHash))
            {
                return BadRequest(new { message = "The invitation is invalid or expired." });
            }

            await using var transaction = await CourseAccess.BeginMutationAsync(_db, cancellationToken);
            var now = UtcNow();
            var invitation = await _db.WorkspaceInvitations.SingleOrDefaultAsync(candidate =>
                candidate.TokenHash == tokenHash
                && candidate.AcceptedAt == null
                && candidate.RevokedAt == null
                && candidate.ExpiresAt > now,
                cancellationToken);
            var account = await _db.Learners.SingleAsync(candidate => candidate.Id == accountId, cancellationToken);
            if (invitation == null || !account.Email.Equals(invitation.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "The invitation is invalid, expired, or addressed to another account." });
            }

            var membership = await _db.WorkspaceMemberships.SingleOrDefaultAsync(candidate =>
                candidate.WorkspaceId == invitation.WorkspaceId && candidate.AccountId == accountId,
                cancellationToken);
            if (membership == null)
            {
                membership = new WorkspaceMembership
                {
                    WorkspaceId = invitation.WorkspaceId,
                    AccountId = accountId,
                    Role = invitation.Role,
                    State = WorkspaceMembershipState.Active,
                    InvitedByAccountId = invitation.InvitedByAccountId,
                    CreatedAt = invitation.CreatedAt,
                    AcceptedAt = now
                };
                _db.Add(membership);
                _db.Add(new WorkspaceInstructorProfile
                {
                    MembershipId = membership.Id,
                    DisplayName = account.Name,
                    UpdatedAt = now
                });
            }
            else
            {
                membership.Role = invitation.Role;
                membership.State = WorkspaceMembershipState.Active;
                membership.AcceptedAt = now;
                membership.RevokedAt = null;
            }
            invitation.AcceptedAt = now;
            invitation.AcceptedByAccountId = accountId;
            var educatorGrant = await _db.AccountRoleGrants.SingleOrDefaultAsync(candidate =>
                candidate.AccountId == accountId && candidate.Role == AccountRoleKind.Educator,
                cancellationToken);
            if (educatorGrant == null)
            {
                _db.Add(new AccountRoleGrant
                {
                    AccountId = accountId,
                    Role = AccountRoleKind.Educator,
                    GrantedByAccountId = invitation.InvitedByAccountId,
                    GrantedAt = now,
                    Reason = "Accepted an educator workspace invitation"
                });
            }
            else
            {
                educatorGrant.IsActive = true;
                educatorGrant.RevokedAt = null;
                educatorGrant.RevokedByAccountId = null;
                educatorGrant.GrantedAt = now;
                educatorGrant.GrantedByAccountId = invitation.InvitedByAccountId;
                educatorGrant.Reason = "Accepted an educator workspace invitation";
            }
            account.Role = "Educator";
            account.RegistrationIntent = "Educator";
            account.AuthorizationVersion++;
            var workspace = await _db.EducatorWorkspaces.SingleAsync(candidate => candidate.Id == invitation.WorkspaceId, cancellationToken);
            workspace.AuthorizationVersion++;
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return Ok(new { membership.Id, membership.WorkspaceId, membership.Role, membership.State });
        }

        [HttpDelete("{workspaceId:guid}/members/{membershipId:guid}")]
        public async Task<IActionResult> RevokeMember(
            Guid workspaceId,
            Guid membershipId,
            CancellationToken cancellationToken)
        {
            var actorId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.CanManageWorkspaceAsync(actorId, workspaceId, cancellationToken))
            {
                return Forbid();
            }
            var membership = await _db.WorkspaceMemberships.SingleOrDefaultAsync(candidate =>
                candidate.Id == membershipId && candidate.WorkspaceId == workspaceId,
                cancellationToken);
            if (membership == null)
            {
                return NotFound();
            }
            if (membership.Role == WorkspaceMembershipRole.Owner)
            {
                return Conflict(new { message = "Transfer workspace ownership before removing its owner." });
            }

            var now = UtcNow();
            membership.State = WorkspaceMembershipState.Revoked;
            membership.RevokedAt = now;
            var assignments = await _db.CourseInstructorAssignments
                .Where(assignment => assignment.MembershipId == membership.Id && assignment.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
            }
            var account = await _db.Learners.SingleAsync(candidate => candidate.Id == membership.AccountId, cancellationToken);
            account.AuthorizationVersion++;
            var workspace = await _db.EducatorWorkspaces.SingleAsync(candidate => candidate.Id == workspaceId, cancellationToken);
            workspace.AuthorizationVersion++;
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPut("{workspaceId:guid}/courses/{courseId:guid}/instructors/{membershipId:guid}")]
        public async Task<IActionResult> AssignInstructor(
            Guid workspaceId,
            Guid courseId,
            Guid membershipId,
            CancellationToken cancellationToken)
        {
            var actorId = await GetCurrentLearnerIdAsync(_db);
            if (!await _authorization.CanManageWorkspaceAsync(actorId, workspaceId, cancellationToken))
            {
                return Forbid();
            }
            var courseExists = await _db.Courses.AnyAsync(course =>
                course.Id == courseId && course.WorkspaceId == workspaceId,
                cancellationToken);
            var membership = await _db.WorkspaceMemberships.SingleOrDefaultAsync(candidate =>
                candidate.Id == membershipId
                && candidate.WorkspaceId == workspaceId
                && candidate.State == WorkspaceMembershipState.Active,
                cancellationToken);
            if (!courseExists || membership == null)
            {
                return NotFound();
            }

            var assignment = await _db.CourseInstructorAssignments.SingleOrDefaultAsync(candidate =>
                candidate.CourseId == courseId && candidate.MembershipId == membershipId,
                cancellationToken);
            if (assignment == null)
            {
                assignment = new CourseInstructorAssignment
                {
                    CourseId = courseId,
                    MembershipId = membershipId,
                    AssignedByAccountId = actorId,
                    AssignedAt = UtcNow()
                };
                _db.Add(assignment);
            }
            assignment.IsActive = true;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(assignment);
        }

        private async Task<bool> CanReviewAsync(Guid accountId, CancellationToken cancellationToken) =>
            await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.PlatformReviewer, cancellationToken)
            || await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.PlatformAdmin, cancellationToken);

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static bool ValidJsonArray(string value)
        {
            try
            {
                using var json = JsonDocument.Parse(value);
                return json.RootElement.ValueKind == JsonValueKind.Array;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryHashToken(string token, out byte[] hash)
        {
            try
            {
                var raw = WebEncoders.Base64UrlDecode(token);
                if (raw.Length != 32)
                {
                    hash = [];
                    return false;
                }
                hash = SHA256.HashData(raw);
                return true;
            }
            catch (FormatException)
            {
                hash = [];
                return false;
            }
        }

        private static string Slug(string value, Guid id)
        {
            var normalized = SlugCharacters().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "educator";
            }
            normalized = normalized[..Math.Min(normalized.Length, 80)];
            return $"{normalized}-{id:N}"[..Math.Min(normalized.Length + 9, 100)];
        }

        [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
        private static partial Regex SlugCharacters();
    }
}
