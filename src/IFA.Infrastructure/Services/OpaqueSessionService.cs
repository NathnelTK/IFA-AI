using System.Security.Cryptography;
using System.Text;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IFA.Infrastructure.Services
{
    public sealed class OpaqueSessionService : IOpaqueSessionService
    {
        private readonly ApplicationDbContext _db;
        private readonly AuthOptions _options;
        private readonly TimeProvider _timeProvider;

        public OpaqueSessionService(
            ApplicationDbContext db,
            IOptions<AuthOptions> options,
            TimeProvider timeProvider)
        {
            _db = db;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public async Task<IssuedOpaqueSession> IssueAsync(
            Guid accountId,
            string hostScope,
            AuthSiteScope siteScope,
            int mfaLevel,
            string? userAgent,
            string? ipAddress,
            Guid? parentSessionId = null,
            CancellationToken cancellationToken = default)
        {
            var account = await _db.Learners.SingleOrDefaultAsync(candidate =>
                candidate.Id == accountId && candidate.DisabledAt == null,
                cancellationToken) ?? throw new InvalidOperationException("The account cannot start a session.");
            var normalizedHost = NormalizeHost(hostScope);
            var token = RandomNumberGenerator.GetBytes(32);
            var csrf = RandomNumberGenerator.GetBytes(32);
            var now = UtcNow();
            var expiresAt = now.AddHours(_options.SessionHours);
            var session = new AuthSession
            {
                AccountId = account.Id,
                TokenHash = SHA256.HashData(token),
                CsrfTokenHash = SHA256.HashData(csrf),
                HostScope = normalizedHost,
                SiteScope = siteScope,
                MfaLevel = Math.Clamp(mfaLevel, 0, 2),
                AuthorizationVersionSnapshot = account.AuthorizationVersion,
                ParentSessionId = parentSessionId,
                UserAgentHash = ContextHash(userAgent),
                IpAddressHash = ContextHash(ipAddress),
                CreatedAt = now,
                LastSeenAt = now,
                ExpiresAt = expiresAt
            };
            _db.AuthSessions.Add(session);
            await _db.SaveChangesAsync(cancellationToken);

            return new IssuedOpaqueSession(
                session.Id,
                Base64UrlEncoder.Encode(token),
                Base64UrlEncoder.Encode(csrf),
                expiresAt);
        }

        public async Task<ValidatedOpaqueSession?> ValidateAsync(
            string rawToken,
            string hostScope,
            AuthSiteScope siteScope,
            string? csrfToken,
            bool requireCsrf,
            CancellationToken cancellationToken = default)
        {
            if (!TryDecode(rawToken, out var token) || token.Length != 32)
            {
                return null;
            }

            var tokenHash = SHA256.HashData(token);
            var session = await _db.AuthSessions
                .Include(candidate => candidate.Account)
                .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);
            var now = UtcNow();
            if (session?.Account == null
                || session.RevokedAt.HasValue
                || session.ExpiresAt <= now
                || session.Account.DisabledAt.HasValue
                || session.SiteScope != siteScope
                || !session.HostScope.Equals(NormalizeHost(hostScope), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (session.AuthorizationVersionSnapshot != session.Account.AuthorizationVersion)
            {
                session.RevokedAt = now;
                session.RevocationReason = "authorization_changed";
                await _db.SaveChangesAsync(cancellationToken);
                return null;
            }
            if (requireCsrf && !ValidCsrf(csrfToken, session.CsrfTokenHash))
            {
                return null;
            }

            var roles = await _db.AccountRoleGrants
                .Where(grant => grant.AccountId == session.AccountId && grant.IsActive && grant.RevokedAt == null)
                .Select(grant => grant.Role)
                .ToListAsync(cancellationToken);
            var workspaceIds = await _db.WorkspaceMemberships
                .Where(membership => membership.AccountId == session.AccountId
                    && membership.State == WorkspaceMembershipState.Active)
                .Select(membership => membership.WorkspaceId)
                .ToListAsync(cancellationToken);

            if (session.LastSeenAt < now.AddMinutes(-5))
            {
                session.LastSeenAt = now;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new ValidatedOpaqueSession(
                session.Id,
                session.AccountId,
                session.Account.Name,
                session.Account.Email,
                session.Account.RegistrationIntent,
                session.MfaLevel,
                roles,
                workspaceIds);
        }

        public async Task<IssuedOpaqueSession?> ExchangeLegacyTokenAsync(
            string rawLegacyToken,
            Guid accountId,
            string hostScope,
            AuthSiteScope siteScope,
            string? userAgent,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawLegacyToken) || rawLegacyToken.Length > 16_384)
            {
                return null;
            }

            var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(rawLegacyToken));
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            if (await _db.LegacyTokenExchanges.AnyAsync(exchange => exchange.TokenHash == tokenHash, cancellationToken))
            {
                return null;
            }

            var issued = await IssueAsync(
                accountId,
                hostScope,
                siteScope,
                mfaLevel: 0,
                userAgent,
                ipAddress,
                parentSessionId: null,
                cancellationToken);
            _db.LegacyTokenExchanges.Add(new LegacyTokenExchange
            {
                TokenHash = tokenHash,
                AccountId = accountId,
                SessionId = issued.SessionId,
                ConsumedAt = UtcNow(),
                ExpiresAt = UtcNow().AddDays(8)
            });
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return issued;
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
        }

        public async Task RevokeAsync(
            Guid sessionId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var session = await _db.AuthSessions.SingleOrDefaultAsync(candidate => candidate.Id == sessionId, cancellationToken);
            if (session == null || session.RevokedAt.HasValue)
            {
                return;
            }
            session.RevokedAt = UtcNow();
            session.RevocationReason = string.IsNullOrWhiteSpace(reason) ? "signed_out" : reason[..Math.Min(reason.Length, 200)];
            await _db.SaveChangesAsync(cancellationToken);
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private string? ContextHash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{_options.ContextPepper}:{value}"))).ToLowerInvariant();
        }

        private static string NormalizeHost(string value)
        {
            var host = value.Trim().TrimEnd('.').ToLowerInvariant();
            if (host.Length is 0 or > 253 || host.Contains('/') || host.Contains('\\'))
            {
                throw new InvalidOperationException("The session host scope is invalid.");
            }
            return host;
        }

        private static bool ValidCsrf(string? rawToken, byte[] expectedHash)
        {
            if (!TryDecode(rawToken, out var token) || token.Length != 32)
            {
                return false;
            }
            return CryptographicOperations.FixedTimeEquals(SHA256.HashData(token), expectedHash);
        }

        private static bool TryDecode(string? value, out byte[] bytes)
        {
            try
            {
                bytes = string.IsNullOrWhiteSpace(value) ? [] : Base64UrlEncoder.DecodeBytes(value);
                return bytes.Length > 0;
            }
            catch (FormatException)
            {
                bytes = [];
                return false;
            }
        }
    }
}
