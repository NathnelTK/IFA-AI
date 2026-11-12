using System.Security.Cryptography;

namespace IFA.Infrastructure.Services
{
    public static class TotpVerifier
    {
        public static string GenerateSecret() => EncodeBase32(RandomNumberGenerator.GetBytes(20));

        public static bool IsValid(string base32Secret, string code, DateTimeOffset now)
        {
            if (code.Length != 6 || !code.All(char.IsDigit))
            {
                return false;
            }
            byte[] secret;
            try
            {
                secret = DecodeBase32(base32Secret);
            }
            catch (FormatException)
            {
                return false;
            }
            if (secret.Length < 16)
            {
                return false;
            }

            var supplied = int.Parse(code, System.Globalization.CultureInfo.InvariantCulture);
            var step = now.ToUnixTimeSeconds() / 30;
            for (var offset = -1; offset <= 1; offset++)
            {
                if (Generate(secret, step + offset) == supplied)
                {
                    return true;
                }
            }
            return false;
        }

        private static int Generate(byte[] secret, long counter)
        {
            Span<byte> counterBytes = stackalloc byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
            using var hmac = new HMACSHA1(secret);
            var hash = hmac.ComputeHash(counterBytes.ToArray());
            var offset = hash[^1] & 0x0f;
            var binary = ((hash[offset] & 0x7f) << 24)
                | (hash[offset + 1] << 16)
                | (hash[offset + 2] << 8)
                | hash[offset + 3];
            return binary % 1_000_000;
        }

        private static byte[] DecodeBase32(string value)
        {
            var normalized = value.Trim().Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
            var output = new List<byte>();
            var buffer = 0;
            var bits = 0;
            foreach (var character in normalized)
            {
                var index = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".IndexOf(character);
                if (index < 0)
                {
                    throw new FormatException("Invalid base32 value.");
                }
                buffer = (buffer << 5) | index;
                bits += 5;
                if (bits >= 8)
                {
                    output.Add((byte)(buffer >> (bits - 8)));
                    bits -= 8;
                    buffer &= (1 << bits) - 1;
                }
            }
            return output.ToArray();
        }

        private static string EncodeBase32(byte[] value)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var output = new System.Text.StringBuilder((value.Length * 8 + 4) / 5);
            var buffer = 0;
            var bits = 0;
            foreach (var item in value)
            {
                buffer = (buffer << 8) | item;
                bits += 8;
                while (bits >= 5)
                {
                    output.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                    buffer &= (1 << bits) - 1;
                }
            }
            if (bits > 0)
            {
                output.Append(alphabet[(buffer << (5 - bits)) & 31]);
            }
            return output.ToString();
        }
    }
}
