
namespace IFA.Infrastructure.AI
{
    internal static class LlmOutput
    {
        private static readonly HashSet<string> EmptyMarkers = new(StringComparer.OrdinalIgnoreCase)
        { "none", "null", "n/a", "na", "unknown", "not specified", "not mentioned", "unspecified", "-" };

        public static string? Clean(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var t = value.Trim();
            return EmptyMarkers.Contains(t) ? null : t;
        }

        public static List<string> CleanList(List<string>? values) =>
            (values ?? new()).Select(Clean).Where(v => v is not null).Select(v => v!).ToList();

        // Cuts out the {...} part, ignoring code fences or chatter around it.
        public static string ExtractJsonObject(string raw)
        {
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            return start >= 0 && end > start ? raw[start..(end + 1)] : raw;
        }
    }
}