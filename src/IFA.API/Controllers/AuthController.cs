using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IFA.API.Authentication;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IOpaqueSessionService _sessions;
        private readonly IAccountTokenService _accountTokens;
        private readonly IApplicationSecretProtector _protector;
        private readonly IHostPolicyService _hostPolicy;
        private readonly FeatureOptions _features;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;

        public AuthController(
            IApplicationDbContext context,
            ITokenService tokenService,
            IOpaqueSessionService sessions,
            IAccountTokenService accountTokens,
            IApplicationSecretProtector protector,
            IHostPolicyService hostPolicy,
            IOptions<FeatureOptions> features,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            TimeProvider timeProvider)
        {
            _context = context;
            _tokenService = tokenService;
            _sessions = sessions;
            _accountTokens = accountTokens;
            _protector = protector;
            _hostPolicy = hostPolicy;
            _features = features.Value;
            _environment = environment;
            _configuration = configuration;
            _timeProvider = timeProvider;
        }

        private bool DemoAccessEnabled => _environment.IsDevelopment()
            || _configuration.GetValue<bool>("DemoData:Enabled")
            || string.Equals(_configuration["DEMO_DATA_ENABLED"], "true", StringComparison.OrdinalIgnoreCase);

        public sealed class RegisterRequest
        {
            [Required, StringLength(120)]
            public string Name { get; set; } = string.Empty;
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = string.Empty;
            [Required, StringLength(128, MinimumLength = 8)]
            public string Password { get; set; } = string.Empty;
        }

        public sealed class LoginRequest
        {
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = string.Empty;
            [Required, StringLength(128)]
            public string Password { get; set; } = string.Empty;
            [StringLength(253)]
            public string? Host { get; set; }
            [StringLength(16)]
            public string? Site { get; set; }
        }

        public sealed class LegacyExchangeRequest
        {
            [Required, StringLength(253)]
            public string Host { get; set; } = string.Empty;
            [Required, StringLength(16)]
            public string Site { get; set; } = string.Empty;
        }

        public class TokenRequest
        {
            [Required, StringLength(500)]
            public string Token { get; set; } = string.Empty;
        }

        public sealed class EmailRequest
        {
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = string.Empty;
        }

        public sealed class ResetPasswordRequest : TokenRequest
        {
            [Required, StringLength(128, MinimumLength = 8)]
            public string Password { get; set; } = string.Empty;
        }

        public sealed class MfaLoginRequest
        {
            [Required, StringLength(500)]
            public string ChallengeToken { get; set; } = string.Empty;
            [Required, StringLength(32)]
            public string Code { get; set; } = string.Empty;
        }

        public sealed class MfaCodeRequest
        {
            [Required, StringLength(32)]
            public string Code { get; set; } = string.Empty;
        }

        public sealed class UpdateMeRequest
        {
            [StringLength(120)]
            public string? Name { get; set; }
            [StringLength(2048)]
            public string? AvatarUrl { get; set; }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            if (await _context.Learners.AnyAsync(account => account.Email == cleanEmail, cancellationToken))
            {
                return Conflict(new { message = "An account with this email already exists." });
            }

            var now = UtcNow();
            var account = new Learner
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? cleanEmail.Split('@')[0] : request.Name.Trim(),
                Email = cleanEmail,
                PasswordHash = PasswordHasher.Hash(request.Password),
                Role = "Learner",
                EmailVerifiedAt = _features.OpaqueSessions ? null : now,
                PasswordChangedAt = now,
                AvatarUrl = string.Empty,
                CreatedAt = now
            };
            IssuedAccountToken? verification = null;
            try
            {
                await _context.ExecuteInTransactionAsync(async () =>
                {
                    _context.Add(account);
                    _context.Add(new AccountRoleGrant
                    {
                        AccountId = account.Id,
                        Role = AccountRoleKind.Learner,
                        GrantedAt = now
                    });
                    _context.Add(new LearnerProfile
                    {
                        LearnerId = account.Id,
                        CurrentLevel = "Not set",
                        UpdatedAt = now
                    });
                    _context.Add(new LearnerState
                    {
                        LearnerId = account.Id,
                        UpdatedAt = now
                    });
                    await _context.SaveChangesAsync(cancellationToken);

                    if (_features.OpaqueSessions)
                    {
                        verification = await _accountTokens.IssueAsync(
                            account.Id,
                            AccountSecurityTokenPurpose.EmailVerification,
                            TimeSpan.FromHours(24),
                            cancellationToken);
                        await QueueSecurityEmailAsync(account, "verify-email", verification, cancellationToken);
                    }
                }, cancellationToken);
            }
            catch (DbUpdateException)
            {
                if (await _context.Learners.AnyAsync(candidate => candidate.Email == cleanEmail, cancellationToken))
                {
                    return Conflict(new { message = "An account with this email already exists." });
                }
                throw;
            }

            if (!_features.OpaqueSessions)
            {
                return Ok(_tokenService.GenerateToken(account));
            }

            return Accepted(new
            {
                verificationRequired = true,
                message = "Check your email to verify the account before signing in.",
                developmentVerificationToken = _environment.IsDevelopment() ? verification!.RawToken : null
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var account = await _context.Learners.FirstOrDefaultAsync(candidate => candidate.Email == cleanEmail, cancellationToken);
            if (account == null
                || account.DisabledAt.HasValue
                || !PasswordHasher.Verify(account.PasswordHash, request.Password))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (PasswordHasher.NeedsRehash(account.PasswordHash))
            {
                account.PasswordHash = PasswordHasher.Hash(request.Password);
                account.PasswordChangedAt = UtcNow();
                account.SecurityStamp = Guid.NewGuid();
                account.AuthorizationVersion++;
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (!_features.OpaqueSessions)
            {
                return Ok(_tokenService.GenerateToken(account));
            }
            if (!account.EmailVerifiedAt.HasValue)
            {
                return StatusCode(403, new
                {
                    code = "email_verification_required",
                    message = "Verify your email before signing in."
                });
            }

            var context = await ResolveSessionContextAsync(request.Host, request.Site, cancellationToken);
            if (context == null)
            {
                return BadRequest(new { code = "invalid_session_host", message = "This host cannot receive a session." });
            }
            if (await _context.AccountMfaCredentials.AnyAsync(
                credential => credential.AccountId == account.Id
                    && credential.VerifiedAt != null
                    && credential.DisabledAt == null,
                cancellationToken))
            {
                var challengeBytes = RandomNumberGenerator.GetBytes(32);
                var challengeExpiresAt = UtcNow().AddMinutes(5);
                _context.Add(new MfaLoginChallenge
                {
                    AccountId = account.Id,
                    TokenHash = SHA256.HashData(challengeBytes),
                    HostScope = context.Value.Host,
                    SiteScope = context.Value.Site,
                    CreatedAt = UtcNow(),
                    ExpiresAt = challengeExpiresAt
                });
                await _context.SaveChangesAsync(cancellationToken);
                return Ok(new
                {
                    mfaRequired = true,
                    challengeToken = WebEncoders.Base64UrlEncode(challengeBytes),
                    expiresAt = challengeExpiresAt
                });
            }

            var session = await _sessions.IssueAsync(
                account.Id,
                context.Value.Host,
                context.Value.Site,
                mfaLevel: 0,
                Request.Headers.UserAgent.FirstOrDefault(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken: cancellationToken);
            return Ok(SessionResponse(account, session));
        }

        [HttpPost("login/mfa")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> CompleteMfaLogin(
            [FromBody] MfaLoginRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions) return NotFound();
            if (!TryHashToken(request.ChallengeToken, out var challengeHash))
            {
                return Unauthorized(new { code = "invalid_mfa_challenge", message = "The MFA challenge is invalid or expired." });
            }
            var now = UtcNow();
            var challenge = await _context.MfaLoginChallenges.SingleOrDefaultAsync(candidate =>
                candidate.TokenHash == challengeHash
                && candidate.ConsumedAt == null
                && candidate.ExpiresAt > now,
                cancellationToken);
            if (challenge == null
                || !await ValidateMfaCodeAsync(challenge.AccountId, request.Code, cancellationToken))
            {
                return Unauthorized(new { code = "invalid_mfa_code", message = "The MFA code is invalid or expired." });
            }

            challenge.ConsumedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            var account = await _context.Learners.SingleAsync(candidate => candidate.Id == challenge.AccountId, cancellationToken);
            var session = await _sessions.IssueAsync(
                account.Id,
                challenge.HostScope,
                challenge.SiteScope,
                mfaLevel: 1,
                Request.Headers.UserAgent.FirstOrDefault(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken: cancellationToken);
            return Ok(SessionResponse(account, session));
        }

        [HttpPost("mfa/enroll")]
        [Authorize]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> BeginMfaEnrollment(CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_context);
            var credential = await _context.AccountMfaCredentials.SingleOrDefaultAsync(
                candidate => candidate.AccountId == accountId,
                cancellationToken);
            if (credential?.VerifiedAt != null && credential.DisabledAt == null)
            {
                return Conflict(new { message = "MFA is already enabled for this account." });
            }

            var secret = TotpVerifier.GenerateSecret();
            var now = UtcNow();
            if (credential == null)
            {
                credential = new AccountMfaCredential
                {
                    AccountId = accountId,
                    CreatedAt = now
                };
                _context.Add(credential);
            }
            credential.ProtectedSecret = _protector.Protect(secret);
            credential.ProtectionKeyVersion = 1;
            credential.CreatedAt = now;
            credential.VerifiedAt = null;
            credential.DisabledAt = null;
            await _context.SaveChangesAsync(cancellationToken);

            var account = await _context.Learners.SingleAsync(candidate => candidate.Id == accountId, cancellationToken);
            var label = Uri.EscapeDataString($"Witforge:{account.Email}");
            var issuer = Uri.EscapeDataString("Witforge");
            return Ok(new
            {
                secret,
                otpauthUri = $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30"
            });
        }

        [HttpPost("mfa/confirm")]
        [Authorize]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ConfirmMfaEnrollment(
            [FromBody] MfaCodeRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_context);
            var credential = await _context.AccountMfaCredentials.SingleOrDefaultAsync(candidate =>
                candidate.AccountId == accountId && candidate.VerifiedAt == null && candidate.DisabledAt == null,
                cancellationToken);
            if (credential == null
                || !TotpVerifier.IsValid(_protector.Unprotect(credential.ProtectedSecret), request.Code, _timeProvider.GetUtcNow()))
            {
                return BadRequest(new { code = "invalid_mfa_code", message = "The authenticator code is invalid." });
            }

            var now = UtcNow();
            credential.VerifiedAt = now;
            var existingCodes = await _context.AccountRecoveryCodes
                .Where(code => code.AccountId == accountId)
                .ToListAsync(cancellationToken);
            foreach (var existing in existingCodes)
            {
                _context.Remove(existing);
            }
            var recoveryCodes = Enumerable.Range(0, 10)
                .Select(_ => RecoveryCode())
                .ToArray();
            foreach (var code in recoveryCodes)
            {
                _context.Add(new AccountRecoveryCode
                {
                    AccountId = accountId,
                    CodeHash = RecoveryCodeHash(code),
                    CreatedAt = now
                });
            }
            var account = await _context.Learners.SingleAsync(candidate => candidate.Id == accountId, cancellationToken);
            account.AuthorizationVersion++;
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new { recoveryCodes });
        }

        [HttpPost("mfa/disable")]
        [Authorize]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> DisableMfa(
            [FromBody] MfaCodeRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions) return NotFound();
            var accountId = await GetCurrentLearnerIdAsync(_context);
            if (!await ValidateMfaCodeAsync(accountId, request.Code, cancellationToken))
            {
                return BadRequest(new { code = "invalid_mfa_code", message = "The authenticator or recovery code is invalid." });
            }
            var credential = await _context.AccountMfaCredentials.SingleOrDefaultAsync(candidate =>
                candidate.AccountId == accountId && candidate.DisabledAt == null,
                cancellationToken);
            if (credential == null)
            {
                return NoContent();
            }
            credential.DisabledAt = UtcNow();
            var account = await _context.Learners.SingleAsync(candidate => candidate.Id == accountId, cancellationToken);
            account.AuthorizationVersion++;
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("legacy-exchange")]
        [Authorize(AuthenticationSchemes = AuthSchemes.LegacyJwt)]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ExchangeLegacyToken(
            [FromBody] LegacyExchangeRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions)
            {
                return NotFound();
            }
            var accountId = await GetCurrentLearnerIdAsync(_context);
            if (!Enum.TryParse<AuthSiteScope>(request.Site, true, out var site)
                || !await _hostPolicy.IsAllowedAsync(request.Host, site, cancellationToken))
            {
                return BadRequest(new { code = "invalid_session_host", message = "This host cannot receive a session." });
            }
            var authorization = Request.Headers.Authorization.FirstOrDefault();
            var rawToken = authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                ? authorization[7..].Trim()
                : null;
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return Unauthorized();
            }

            var session = await _sessions.ExchangeLegacyTokenAsync(
                rawToken,
                accountId,
                _hostPolicy.Normalize(request.Host),
                site,
                Request.Headers.UserAgent.FirstOrDefault(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
            if (session == null)
            {
                return Conflict(new { code = "legacy_token_already_exchanged", message = "This legacy token cannot be exchanged again." });
            }
            var account = await _context.Learners.SingleAsync(candidate => candidate.Id == accountId, cancellationToken);
            return Ok(SessionResponse(account, session));
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> VerifyEmail([FromBody] TokenRequest request, CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions) return NotFound();
            var accountId = await _accountTokens.ConsumeAsync(
                request.Token,
                AccountSecurityTokenPurpose.EmailVerification,
                cancellationToken);
            if (!accountId.HasValue)
            {
                return BadRequest(new { code = "invalid_verification_token", message = "The verification link is invalid or expired." });
            }
            var account = await _context.Learners.SingleAsync(candidate => candidate.Id == accountId.Value, cancellationToken);
            account.EmailVerifiedAt ??= UtcNow();
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("resend-verification")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-email")]
        public async Task<IActionResult> ResendVerification([FromBody] EmailRequest request, CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions) return NotFound();
            var account = await _context.Learners.FirstOrDefaultAsync(
                candidate => candidate.Email == request.Email.Trim().ToLowerInvariant(),
                cancellationToken);
            if (account != null && !account.EmailVerifiedAt.HasValue && !account.DisabledAt.HasValue)
            {
                var token = await _accountTokens.IssueAsync(
                    account.Id,
                    AccountSecurityTokenPurpose.EmailVerification,
                    TimeSpan.FromHours(24),
                    cancellationToken);
                await QueueSecurityEmailAsync(account, "verify-email", token, cancellationToken);
            }
            return Accepted(new { message = "If the account is eligible, a verification message will be sent." });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-email")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailRequest request, CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions) return NotFound();
            var account = await _context.Learners.FirstOrDefaultAsync(
                candidate => candidate.Email == request.Email.Trim().ToLowerInvariant(),
                cancellationToken);
            string? developmentToken = null;
            if (account != null && !account.DisabledAt.HasValue)
            {
                var token = await _accountTokens.IssueAsync(
                    account.Id,
                    AccountSecurityTokenPurpose.PasswordReset,
                    TimeSpan.FromMinutes(30),
                    cancellationToken);
                await QueueSecurityEmailAsync(account, "reset-password", token, cancellationToken);
                developmentToken = _environment.IsDevelopment() ? token.RawToken : null;
            }
            return Accepted(new
            {
                message = "If the account exists, a password reset message will be sent.",
                developmentToken
            });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            if (!_features.OpaqueSessions) return NotFound();
            var accountId = await _accountTokens.ConsumeAsync(
                request.Token,
                AccountSecurityTokenPurpose.PasswordReset,
                cancellationToken);
            if (!accountId.HasValue)
            {
                return BadRequest(new { code = "invalid_reset_token", message = "The reset link is invalid or expired." });
            }
            var account = await _context.Learners.SingleAsync(candidate => candidate.Id == accountId.Value, cancellationToken);
            var now = UtcNow();
            account.PasswordHash = PasswordHasher.Hash(request.Password);
            account.PasswordChangedAt = now;
            account.SecurityStamp = Guid.NewGuid();
            account.AuthorizationVersion++;
            var sessions = await _context.AuthSessions
                .Where(session => session.AccountId == account.Id && session.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                session.RevokedAt = now;
                session.RevocationReason = "password_reset";
            }
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            if (Guid.TryParse(User.FindFirstValue("session_id"), out var sessionId))
            {
                await _sessions.RevokeAsync(sessionId, "signed_out", cancellationToken);
            }
            return NoContent();
        }

        [HttpGet("demo-accounts")]
        [AllowAnonymous]
        public IActionResult GetDemoAccounts()
        {
            if (!DemoAccessEnabled)
            {
                return NotFound();
            }
            return Ok(DemoAccounts.All.Select(account => new
            {
                account.Name,
                account.Email,
                account.Role,
                account.AvatarUrl
            }));
        }

        [HttpPost("demo-login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> DemoLogin(CancellationToken cancellationToken)
        {
            if (!DemoAccessEnabled)
            {
                return NotFound();
            }
            var account = await _context.Learners.FirstOrDefaultAsync(
                candidate => candidate.Email == DemoAccounts.All[0].Email,
                cancellationToken);
            if (account == null)
            {
                return StatusCode(503, new { message = "The demo account is not available in this environment." });
            }
            if (_features.OpaqueSessions)
            {
                var context = await ResolveSessionContextAsync(null, null, cancellationToken);
                if (context == null)
                {
                    return BadRequest(new { message = "This host cannot receive a session." });
                }
                var session = await _sessions.IssueAsync(
                    account.Id,
                    context.Value.Host,
                    context.Value.Site,
                    mfaLevel: 0,
                    Request.Headers.UserAgent.FirstOrDefault(),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    cancellationToken: cancellationToken);
                return Ok(SessionResponse(account, session));
            }
            return Ok(_tokenService.GenerateToken(account));
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_context);
            var account = await _context.Learners
                .Include(candidate => candidate.Profile)
                .Include(candidate => candidate.State)
                .Include(candidate => candidate.Settings)
                .FirstOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken);
            if (account == null)
            {
                return NotFound();
            }

            var roles = _features.Workspaces
                ? await _context.AccountRoleGrants
                    .Where(role => role.AccountId == accountId && role.IsActive && role.RevokedAt == null)
                    .Select(role => role.Role.ToString())
                    .ToListAsync(cancellationToken)
                : [account.Role];
            var workspaces = _features.Workspaces
                ? await _context.WorkspaceMemberships
                    .Where(membership => membership.AccountId == accountId
                        && membership.State == WorkspaceMembershipState.Active)
                    .Join(_context.EducatorWorkspaces,
                        membership => membership.WorkspaceId,
                        workspace => workspace.Id,
                        (membership, workspace) => new
                        {
                            workspace.Id,
                            workspace.Name,
                            workspace.Slug,
                            workspace.Type,
                            membership.Role
                        })
                    .ToListAsync(cancellationToken)
                : [];

            return Ok(new
            {
                account.Id,
                account.Name,
                account.Email,
                role = roles.Contains(AccountRoleKind.Educator.ToString()) ? "Educator" : "Learner",
                roles,
                workspaces,
                account.EmailVerifiedAt,
                account.AvatarUrl,
                account.OverallProgress,
                account.Profile,
                account.State,
                account.Settings
            });
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request, CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_context);
            var account = await _context.Learners.FirstOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken);
            if (account == null)
            {
                return NotFound();
            }
            if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Name cannot be blank." });
            }
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl)
                && (!Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out var avatarUri)
                    || avatarUri.Scheme is not ("http" or "https")
                    || !string.IsNullOrEmpty(avatarUri.UserInfo)))
            {
                return BadRequest(new { message = "Avatar must be a valid HTTP(S) URL." });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                account.Name = request.Name.Trim();
            }
            if (request.AvatarUrl is not null)
            {
                account.AvatarUrl = request.AvatarUrl.Trim();
            }
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new
            {
                account.Id,
                account.Name,
                account.Email,
                account.AvatarUrl,
                account.OverallProgress
            });
        }

        private async Task QueueSecurityEmailAsync(
            Learner account,
            string template,
            IssuedAccountToken token,
            CancellationToken cancellationToken)
        {
            var now = UtcNow();
            _context.Add(new OutboxMessage
            {
                Type = "auth.email",
                AggregateType = nameof(Learner),
                AggregateId = account.Id,
                DeduplicationKey = $"auth-email:{template}:{account.Id}:{token.ExpiresAt.Ticks}",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    accountId = account.Id,
                    account.Email,
                    account.Name,
                    template,
                    protectedToken = _protector.Protect(token.RawToken),
                    token.ExpiresAt
                }, JsonOptions),
                OccurredAt = now,
                AvailableAt = now,
                CorrelationId = HttpContext.TraceIdentifier,
                ActorLearnerId = account.Id
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<(string Host, AuthSiteScope Site)?> ResolveSessionContextAsync(
            string? requestedHost,
            string? requestedSite,
            CancellationToken cancellationToken)
        {
            var host = requestedHost
                ?? Request.Headers[OpaqueBffAuthenticationHandler.HostHeader].FirstOrDefault()
                ?? Request.Host.Host;
            var siteValue = requestedSite
                ?? Request.Headers[OpaqueBffAuthenticationHandler.SiteHeader].FirstOrDefault()
                ?? AuthSiteScope.App.ToString();
            if (!Enum.TryParse<AuthSiteScope>(siteValue, true, out var site)
                || !await _hostPolicy.IsAllowedAsync(host, site, cancellationToken))
            {
                return null;
            }
            return (_hostPolicy.Normalize(host), site);
        }

        private static object SessionResponse(Learner account, IssuedOpaqueSession session) => new
        {
            sessionToken = session.SessionToken,
            csrfToken = session.CsrfToken,
            session.ExpiresAt,
            learnerId = account.Id,
            account.Name,
            account.Email,
            role = account.Role
        };

        private async Task<bool> ValidateMfaCodeAsync(
            Guid accountId,
            string code,
            CancellationToken cancellationToken)
        {
            var normalized = code.Trim().ToUpperInvariant();
            var credential = await _context.AccountMfaCredentials.SingleOrDefaultAsync(candidate =>
                candidate.AccountId == accountId
                && candidate.VerifiedAt != null
                && candidate.DisabledAt == null,
                cancellationToken);
            if (credential == null)
            {
                return false;
            }
            if (TotpVerifier.IsValid(_protector.Unprotect(credential.ProtectedSecret), normalized, _timeProvider.GetUtcNow()))
            {
                return true;
            }

            var hash = RecoveryCodeHash(normalized);
            var recovery = await _context.AccountRecoveryCodes.SingleOrDefaultAsync(candidate =>
                candidate.AccountId == accountId
                && candidate.CodeHash == hash
                && candidate.UsedAt == null,
                cancellationToken);
            if (recovery == null)
            {
                return false;
            }
            recovery.UsedAt = UtcNow();
            return true;
        }

        private static string RecoveryCode()
        {
            var value = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
            return $"{value[..4]}-{value[4..8]}-{value[8..12]}-{value[12..]}";
        }

        private static byte[] RecoveryCodeHash(string value) =>
            SHA256.HashData(Encoding.UTF8.GetBytes(value.Replace("-", string.Empty).Trim().ToUpperInvariant()));

        private static bool TryHashToken(string value, out byte[] hash)
        {
            try
            {
                var raw = WebEncoders.Base64UrlDecode(value);
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

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
