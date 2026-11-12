using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace IFA.API.Authentication
{
    public sealed class OpaqueBffAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SessionHeader = "X-Witforge-Session";
        public const string HostHeader = "X-Witforge-Host";
        public const string SiteHeader = "X-Witforge-Site";
        public const string WorkspaceHeader = "X-Witforge-Workspace";
        public const string TimestampHeader = "X-Witforge-Timestamp";
        public const string NonceHeader = "X-Witforge-Nonce";
        public const string SignatureHeader = "X-Witforge-Signature";
        public const string ClientKeyHeader = "X-Witforge-Client-Key";
        public const string CsrfHeader = "X-CSRF-Token";
        public const string VerifiedBffItem = "VerifiedBffRequest";
        public const string ClientKeyItem = "BffClientKey";

        private readonly IOpaqueSessionService _sessions;
        private readonly ApplicationDbContext _db;
        private readonly AuthOptions _authOptions;
        private readonly TimeProvider _timeProvider;

        public OpaqueBffAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder,
            IOpaqueSessionService sessions,
            ApplicationDbContext db,
            IOptions<AuthOptions> authOptions,
            TimeProvider timeProvider)
            : base(options, logger, encoder)
        {
            _sessions = sessions;
            _db = db;
            _authOptions = authOptions.Value;
            _timeProvider = timeProvider;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var sessionToken = Request.Headers[SessionHeader].FirstOrDefault() ?? string.Empty;
            var signature = Request.Headers[SignatureHeader].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(sessionToken) && string.IsNullOrWhiteSpace(signature))
            {
                return AuthenticateResult.NoResult();
            }

            var host = Request.Headers[HostHeader].FirstOrDefault();
            var siteValue = Request.Headers[SiteHeader].FirstOrDefault();
            var workspaceValue = Request.Headers[WorkspaceHeader].FirstOrDefault() ?? string.Empty;
            var timestampValue = Request.Headers[TimestampHeader].FirstOrDefault();
            var nonce = Request.Headers[NonceHeader].FirstOrDefault();
            var clientKey = Request.Headers[ClientKeyHeader].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(host)
                || !Enum.TryParse<AuthSiteScope>(siteValue, true, out var siteScope)
                || !long.TryParse(timestampValue, NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp)
                || string.IsNullOrWhiteSpace(nonce)
                || nonce.Length is < 16 or > 128
                || string.IsNullOrWhiteSpace(clientKey)
                || clientKey.Length > 128
                || string.IsNullOrWhiteSpace(signature))
            {
                return AuthenticateResult.Fail("The internal authentication envelope is incomplete.");
            }

            var now = _timeProvider.GetUtcNow();
            var sentAt = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            if ((now - sentAt).Duration() > TimeSpan.FromSeconds(_authOptions.InternalRequestMaxSkewSeconds))
            {
                return AuthenticateResult.Fail("The internal authentication envelope expired.");
            }

            var canonical = CanonicalRequest(
                Request.Method,
                $"{Request.PathBase}{Request.Path}{Request.QueryString}",
                timestampValue!,
                nonce,
                host,
                siteValue!,
                workspaceValue,
                clientKey,
                sessionToken);
            if (!ValidSignature(canonical, signature))
            {
                return AuthenticateResult.Fail("The internal authentication signature is invalid.");
            }

            var nonceHash = SHA256.HashData(Encoding.UTF8.GetBytes($"{host}\n{timestampValue}\n{nonce}\n{clientKey}"));
            _db.InternalRequestReplays.Add(new InternalRequestReplay
            {
                NonceHash = nonceHash,
                ReceivedAt = now.UtcDateTime,
                ExpiresAt = now.AddSeconds(_authOptions.InternalRequestMaxSkewSeconds * 2).UtcDateTime
            });
            try
            {
                await _db.SaveChangesAsync(Context.RequestAborted);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
            {
                return AuthenticateResult.Fail("The internal authentication envelope was replayed.");
            }

            Context.Items[VerifiedBffItem] = true;
            Context.Items[ClientKeyItem] = clientKey;
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return AuthenticateResult.NoResult();
            }

            var requireCsrf = HttpMethods.IsPost(Request.Method)
                || HttpMethods.IsPut(Request.Method)
                || HttpMethods.IsPatch(Request.Method)
                || HttpMethods.IsDelete(Request.Method);
            var validated = await _sessions.ValidateAsync(
                sessionToken,
                host,
                siteScope,
                Request.Headers[CsrfHeader].FirstOrDefault(),
                requireCsrf,
                Context.RequestAborted);
            if (validated == null)
            {
                return AuthenticateResult.Fail("The opaque session is invalid or expired.");
            }

            Guid? activeWorkspaceId = null;
            if (!string.IsNullOrWhiteSpace(workspaceValue))
            {
                if (!Guid.TryParse(workspaceValue, out var parsed)
                    || !validated.WorkspaceIds.Contains(parsed))
                {
                    return AuthenticateResult.Fail("The requested workspace context is not an active membership.");
                }
                activeWorkspaceId = parsed;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, validated.AccountId.ToString()),
                new(ClaimTypes.Name, validated.Name),
                new(ClaimTypes.Email, validated.Email),
                new("session_id", validated.SessionId.ToString()),
                new("mfa_level", validated.MfaLevel.ToString(CultureInfo.InvariantCulture)),
                new("site_scope", siteScope.ToString())
            };
            claims.AddRange(validated.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));
            claims.AddRange(validated.WorkspaceIds.Select(id => new Claim("workspace", id.ToString())));
            if (activeWorkspaceId.HasValue)
            {
                claims.Add(new Claim("active_workspace", activeWorkspaceId.Value.ToString()));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        private bool ValidSignature(string canonical, string supplied)
        {
            if (_authOptions.BffSigningSecret.Length < 32)
            {
                return false;
            }
            byte[] suppliedBytes;
            try
            {
                suppliedBytes = WebEncoders.Base64UrlDecode(supplied);
            }
            catch (FormatException)
            {
                return false;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_authOptions.BffSigningSecret));
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            return suppliedBytes.Length == expected.Length
                && CryptographicOperations.FixedTimeEquals(suppliedBytes, expected);
        }

        public static string CanonicalRequest(
            string method,
            string pathAndQuery,
            string timestamp,
            string nonce,
            string host,
            string site,
            string workspace,
            string clientKey,
            string sessionToken) =>
            string.Join('\n',
                method.ToUpperInvariant(),
                pathAndQuery,
                timestamp,
                nonce,
                host.ToLowerInvariant(),
                site,
                workspace,
                clientKey,
                sessionToken);
    }
}
