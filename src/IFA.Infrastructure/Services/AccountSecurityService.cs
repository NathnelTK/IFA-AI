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
    public sealed class AccountTokenService : IAccountTokenService
    {
        private readonly ApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;

        public AccountTokenService(ApplicationDbContext db, TimeProvider timeProvider)
        {
            _db = db;
            _timeProvider = timeProvider;
        }

        public async Task<IssuedAccountToken> IssueAsync(
            Guid accountId,
            AccountSecurityTokenPurpose purpose,
            TimeSpan lifetime,
            CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var previous = await _db.AccountSecurityTokens
                .Where(token => token.AccountId == accountId
                    && token.Purpose == purpose
                    && token.ConsumedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var token in previous)
            {
                token.ConsumedAt = now;
            }

            var raw = RandomNumberGenerator.GetBytes(32);
            var expiresAt = now.Add(lifetime);
            _db.AccountSecurityTokens.Add(new AccountSecurityToken
            {
                AccountId = accountId,
                Purpose = purpose,
                TokenHash = SHA256.HashData(raw),
                CreatedAt = now,
                ExpiresAt = expiresAt
            });
            await _db.SaveChangesAsync(cancellationToken);
            return new IssuedAccountToken(Base64UrlEncoder.Encode(raw), expiresAt);
        }

        public async Task<Guid?> ConsumeAsync(
            string rawToken,
            AccountSecurityTokenPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            byte[] raw;
            try
            {
                raw = Base64UrlEncoder.DecodeBytes(rawToken);
            }
            catch (FormatException)
            {
                return null;
            }
            if (raw.Length != 32)
            {
                return null;
            }

            var hash = SHA256.HashData(raw);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var token = await _db.AccountSecurityTokens.SingleOrDefaultAsync(candidate =>
                candidate.TokenHash == hash
                && candidate.Purpose == purpose
                && candidate.ConsumedAt == null
                && candidate.ExpiresAt > now,
                cancellationToken);
            if (token == null)
            {
                return null;
            }
            token.ConsumedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return token.AccountId;
        }
    }

    public sealed class ApplicationSecretProtector : IApplicationSecretProtector
    {
        private readonly byte[] _key;

        public ApplicationSecretProtector(IOptions<AuthOptions> options)
        {
            try
            {
                _key = Convert.FromBase64String(options.Value.DataProtectionKey);
            }
            catch (FormatException)
            {
                _key = [];
            }
        }

        public string Protect(string plaintext)
        {
            EnsureConfigured();
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(_key, tag.Length);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            var envelope = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
            envelope[0] = 1;
            nonce.CopyTo(envelope, 1);
            tag.CopyTo(envelope, 13);
            ciphertext.CopyTo(envelope, 29);
            return Base64UrlEncoder.Encode(envelope);
        }

        public string Unprotect(string protectedValue)
        {
            EnsureConfigured();
            var envelope = Base64UrlEncoder.DecodeBytes(protectedValue);
            if (envelope.Length < 30 || envelope[0] != 1)
            {
                throw new CryptographicException("The protected value is invalid.");
            }
            var plaintext = new byte[envelope.Length - 29];
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(envelope.AsSpan(1, 12), envelope.AsSpan(13, 16), envelope.AsSpan(29), plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }

        private void EnsureConfigured()
        {
            if (_key.Length != 32)
            {
                throw new InvalidOperationException("Auth:DataProtectionKey must be a base64-encoded 256-bit key.");
            }
        }
    }
}
