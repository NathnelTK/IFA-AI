using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
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
    [Route("api/sso")]
    public sealed class SsoController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IOpaqueSessionService _sessions;
        private readonly IHostPolicyService _hostPolicy;
        private readonly AuthOptions _options;
        private readonly TimeProvider _timeProvider;

        public SsoController(
            IApplicationDbContext db,
            IOpaqueSessionService sessions,
            IHostPolicyService hostPolicy,
            IOptions<AuthOptions> options,
            TimeProvider timeProvider)
        {
            _db = db;
            _sessions = sessions;
            _hostPolicy = hostPolicy;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public sealed class AuthorizeRequest
        {
            [Required, StringLength(100)]
            public string ClientId { get; set; } = string.Empty;
            [Required, StringLength(1000)]
            public string RedirectUri { get; set; } = string.Empty;
            [Required, StringLength(128, MinimumLength = 43)]
            public string CodeChallenge { get; set; } = string.Empty;
            [Required, StringLength(500)]
            public string State { get; set; } = string.Empty;
        }

        public sealed class TokenRequest
        {
            [Required, StringLength(100)]
            public string ClientId { get; set; } = string.Empty;
            [Required, StringLength(1000)]
            public string RedirectUri { get; set; } = string.Empty;
            [Required, StringLength(500)]
            public string Code { get; set; } = string.Empty;
            [Required, StringLength(128, MinimumLength = 43)]
            public string CodeVerifier { get; set; } = string.Empty;
        }

        [HttpPost("authorize")]
        [Authorize]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> AuthorizeClient(
            [FromBody] AuthorizeRequest request,
            CancellationToken cancellationToken)
        {
            var client = await FindClientAsync(request.ClientId, request.RedirectUri, cancellationToken);
            if (client == null
                || !Enum.TryParse<AuthSiteScope>(client.TargetSite, true, out var site)
                || !await _hostPolicy.IsAllowedAsync(client.TargetHost, site, cancellationToken)
                || !ValidPkceValue(request.CodeChallenge))
            {
                return BadRequest(new { code = "invalid_sso_request", message = "The SSO client, redirect, or PKCE challenge is invalid." });
            }
            var accountId = await GetCurrentLearnerIdAsync(_db);
            if (!Guid.TryParse(User.FindFirstValue("session_id"), out var parentSessionId))
            {
                return Unauthorized();
            }

            var rawCode = RandomNumberGenerator.GetBytes(32);
            var now = UtcNow();
            _db.Add(new OAuthAuthorizationCode
            {
                AccountId = accountId,
                SessionId = parentSessionId,
                CodeHash = SHA256.HashData(rawCode),
                ClientId = client.ClientId,
                RedirectUri = request.RedirectUri,
                CodeChallenge = request.CodeChallenge,
                StateHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.State))).ToLowerInvariant(),
                TargetHost = _hostPolicy.Normalize(client.TargetHost),
                TargetSiteScope = site,
                MfaLevel = int.TryParse(User.FindFirstValue("mfa_level"), out var mfaLevel) ? mfaLevel : 0,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(5)
            });
            await _db.SaveChangesAsync(cancellationToken);

            var redirect = new UriBuilder(request.RedirectUri);
            var separator = string.IsNullOrWhiteSpace(redirect.Query) ? string.Empty : $"{redirect.Query.TrimStart('&', '?')}&";
            redirect.Query = $"{separator}code={Uri.EscapeDataString(WebEncoders.Base64UrlEncode(rawCode))}&state={Uri.EscapeDataString(request.State)}";
            return Ok(new { redirectUrl = redirect.Uri.ToString() });
        }

        [HttpPost("token")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ExchangeCode(
            [FromBody] TokenRequest request,
            CancellationToken cancellationToken)
        {
            var client = await FindClientAsync(request.ClientId, request.RedirectUri, cancellationToken);
            if (client == null || !TryHashCode(request.Code, out var codeHash) || !ValidPkceValue(request.CodeVerifier))
            {
                return BadRequest(new { code = "invalid_authorization_code" });
            }

            await using var transaction = await CourseAccess.BeginMutationAsync(_db, cancellationToken);
            var now = UtcNow();
            var code = await _db.OAuthAuthorizationCodes.SingleOrDefaultAsync(candidate =>
                candidate.CodeHash == codeHash
                && candidate.ClientId == request.ClientId
                && candidate.RedirectUri == request.RedirectUri
                && candidate.ConsumedAt == null
                && candidate.ExpiresAt > now,
                cancellationToken);
            if (code == null || !PkceMatches(request.CodeVerifier, code.CodeChallenge))
            {
                return BadRequest(new { code = "invalid_authorization_code" });
            }

            code.ConsumedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            var issued = await _sessions.IssueAsync(
                code.AccountId,
                code.TargetHost,
                code.TargetSiteScope,
                mfaLevel: code.MfaLevel,
                Request.Headers.UserAgent.FirstOrDefault(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                code.SessionId,
                cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            var account = await _db.Learners.SingleAsync(candidate => candidate.Id == code.AccountId, cancellationToken);
            return Ok(new
            {
                sessionToken = issued.SessionToken,
                csrfToken = issued.CsrfToken,
                issued.ExpiresAt,
                learnerId = account.Id,
                account.Name,
                account.Email,
                role = account.Role
            });
        }

        private async Task<SsoClientOptions?> FindClientAsync(
            string clientId,
            string redirectUri,
            CancellationToken cancellationToken)
        {
            var configured = _options.Clients.SingleOrDefault(client =>
                client.ClientId.Equals(clientId, StringComparison.Ordinal)
                && client.RedirectUris.Contains(redirectUri, StringComparer.Ordinal));
            if (configured != null) return configured;
            if (!clientId.Equals("witforge-public-site", StringComparison.Ordinal)
                || !Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || uri.AbsolutePath != "/auth/callback"
                || !string.IsNullOrEmpty(uri.UserInfo)
                || !await _hostPolicy.IsAllowedAsync(uri.Host, AuthSiteScope.Public, cancellationToken))
            {
                return null;
            }
            return new SsoClientOptions
            {
                ClientId = clientId,
                TargetHost = uri.Host,
                TargetSite = AuthSiteScope.Public.ToString(),
                RedirectUris = [redirectUri]
            };
        }

        private static bool PkceMatches(string verifier, string challenge)
        {
            var actual = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            var actualBytes = Encoding.ASCII.GetBytes(actual);
            var challengeBytes = Encoding.ASCII.GetBytes(challenge);
            return actualBytes.Length == challengeBytes.Length
                && CryptographicOperations.FixedTimeEquals(actualBytes, challengeBytes);
        }

        private static bool ValidPkceValue(string value) =>
            value.Length is >= 43 and <= 128
            && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '.' or '_' or '~');

        private static bool TryHashCode(string value, out byte[] hash)
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
