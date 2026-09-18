using System;
using System.Collections.Generic;
using System.IO;

namespace IFA.Infrastructure.Configuration
{
    /// <summary>
    /// Minimal .env loader so local secrets (such as the PostgreSQL password)
    /// can live in an untracked file while staying available to both the API
    /// host and the `dotnet ef` design-time factory.
    /// Existing process environment variables always take precedence.
    /// </summary>
    public static class EnvFile
    {
        public static void Load(string? startDirectory = null)
        {
            var path = Find(startDirectory);
            if (path is null)
            {
                return;
            }

            foreach (var pair in Parse(File.ReadAllLines(path)))
            {
                if (Environment.GetEnvironmentVariable(pair.Key) is null)
                {
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                }
            }
        }

        public static string? Find(string? startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory ?? Directory.GetCurrentDirectory());

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, ".env");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<string, string>> Parse(IEnumerable<string> lines)
        {
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                {
                    line = line["export ".Length..].Trim();
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim().Trim('"', '\'');

                if (key.Length > 0)
                {
                    yield return new KeyValuePair<string, string>(key, value);
                }
            }
        }
    }
}
