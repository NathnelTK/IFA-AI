using System;
using System.Security.Cryptography;
using System.Text;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Versioned, per-password hashing with transparent verification support for
    /// legacy SHA-256 rows. Successful legacy sign-ins are upgraded by the caller.
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iterations = 600_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const string Prefix = "pbkdf2-sha256";

        public static string Hash(string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string? storedHash, string password)
        {
            if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrEmpty(password)) return false;
            if (!storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
                return VerifyLegacy(storedHash, password);

            var parts = storedHash.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations is < 100_000 or > 2_000_000)
                return false;
            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                if (salt.Length < SaltSize || expected.Length != HashSize) return false;
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool NeedsRehash(string? storedHash) =>
            string.IsNullOrWhiteSpace(storedHash) || !storedHash.StartsWith($"{Prefix}${Iterations}$", StringComparison.Ordinal);

        private static bool VerifyLegacy(string storedHash, string password)
        {
            var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("IFA_SALT_" + password)));
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(storedHash), Encoding.UTF8.GetBytes(expected));
        }
    }
}
