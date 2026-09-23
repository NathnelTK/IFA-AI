using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.AI
{
    /// <summary>
    /// Application-facing gateway that routes each request to the first
    /// provider that supports the requested model role, and transparently
    /// retries the next provider when one reports a free-tier rate limit or
    /// transport failure.
    /// </summary>
    public class FallbackAiService : IAiModelGateway
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        private readonly IReadOnlyDictionary<string, ILlmProvider> _providers;
        private readonly AiGatewayOptions _options;

        public FallbackAiService(IEnumerable<ILlmProvider> providers, AiGatewayOptions options)
        {
            _providers = providers.ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
            _options = options;
        }

        public async Task<AiCompletionResult> CompleteAsync(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            var candidates = _options.ProviderFallbackOrder
                .Where(name => _providers.ContainsKey(name))
                .Select(name => _providers[name])
                .Where(provider => provider.SupportsRole(request.Role))
                .ToList();

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No LLM provider is enabled for model role '{request.Role}'.");
            }

            var failures = new List<string>();

            for (var index = 0; index < candidates.Count; index++)
            {
                var provider = candidates[index];
                try
                {
                    var result = await provider.CompleteAsync(request, cancellationToken).ConfigureAwait(false);
                    result.Role = request.Role;
                    result.ProviderName = provider.Name;
                    result.UsedFallback = index > 0;
                    return result;
                }
                catch (LlmProviderException exception) when (exception.IsRateLimited || index < candidates.Count - 1)
                {
                    failures.Add($"{provider.Name}: {exception.Message}");
                }
            }

            throw new LlmProviderException(
                providerName: "Gateway",
                message: $"All LLM providers failed for role '{request.Role}'. {string.Join(" | ", failures)}");
        }

        public async Task<T?> CompleteAsJsonAsync<T>(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            request.RequiresJson = true;
            var result = await CompleteAsync(request, cancellationToken).ConfigureAwait(false);
            var json = ExtractJsonObject(result.Content);
            return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        private static string? ExtractJsonObject(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var trimmed = Regex
                .Replace(content.Trim(), "^```(?:json)?|```$", string.Empty, RegexOptions.Multiline)
                .Trim();

            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');

            return start >= 0 && end > start
                ? trimmed.Substring(start, end - start + 1)
                : null;
        }
    }
}
