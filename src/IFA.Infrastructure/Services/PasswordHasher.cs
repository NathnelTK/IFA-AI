using System;
using System.Security.Cryptography;
using System.Text;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Single source of truth for IFA password hashing so the auth controller and
    /// the demo seeders always produce comparable hashes.
    /// </summary>
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("IFA_SALT_" + password));
            return Convert.ToBase64String(bytes);
        }

        public static bool Verify(string? storedHash, string password)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(storedHash),
                Encoding.UTF8.GetBytes(Hash(password)));
        }
    }
}
