using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Exceptions;
using IFA.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class LlmGateway : ILlmGateway
    {
        private readonly GeminiLlmProvider _gemini;
        private readonly GroqLlmProvider _groq;
        private readonly OllamaLlmProvider _ollama;
        private readonly IConfiguration _config;
        private readonly ILogger<LlmGateway> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            MaxDepth = 32
        };

        public LlmGateway(
            GeminiLlmProvider gemini,
            GroqLlmProvider groq,
            OllamaLlmProvider ollama,
            IConfiguration config,
            ILogger<LlmGateway> logger)
        {
            _gemini = gemini;
            _groq = groq;
            _ollama = ollama;
            _config = config;
            _logger = logger;
        }

        public Task<string> CompleteAsync(string systemPrompt, string userPrompt, LlmRole role = LlmRole.General, CancellationToken ct = default) =>
            CompleteWithProvidersAsync(systemPrompt, userPrompt, role, response => response, ct);

        private async Task<T> CompleteWithProvidersAsync<T>(string systemPrompt, string userPrompt, LlmRole role, Func<string, T> parse, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(90));
            var preferredProvider = GetProviderForRole(role);
            var providersToTry = GetProviderOrder(preferredProvider);

            foreach (var providerName in providersToTry)
            {
                ct.ThrowIfCancellationRequested();
                if (timeout.IsCancellationRequested) break;

                try
                {
                    // A role's model belongs only to its selected provider, not its failover providers.
                    var model = providerName.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase)
                        ? _config[$"Ai:Pipelines:{GetRoleKey(role)}:Model"]
                        : null;
                    string response;
                    if (providerName.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && _gemini.IsConfigured)
                    {
                        _logger.LogInformation("Invoking Gemini LLM for role {Role}", role);
                        response = await _gemini.GenerateAsync(systemPrompt, userPrompt, model, timeout.Token);
                    }
                    else if (providerName.Equals("Groq", StringComparison.OrdinalIgnoreCase) && _groq.IsConfigured)
                    {
                        _logger.LogInformation("Invoking Groq LLM for role {Role}", role);
                        response = await _groq.GenerateAsync(systemPrompt, userPrompt, model, timeout.Token);
                    }
                    else if (providerName.Equals("Ollama", StringComparison.OrdinalIgnoreCase) && _ollama.IsConfigured)
                    {
                        _logger.LogInformation("Invoking Ollama LLM for role {Role}", role);
                        response = await _ollama.GenerateAsync(systemPrompt, userPrompt, model, timeout.Token);
                    }
                    else continue;

                    ct.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(response) || response.Length > 100_000)
                        throw new InvalidOperationException("The AI provider returned an empty or oversized response.");

                    // JSON validation is part of each attempt, so malformed output can fail over too.
                    return parse(response);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Provider {Provider} failed for role {Role} ({ErrorType}); trying the next configured provider.",
                        providerName, role, ex.GetType().Name);
                }
            }

            ct.ThrowIfCancellationRequested();
            throw new AiUnavailableException("AI is unavailable or returned an unusable response. Check provider configuration or try again later. Manual course editing remains available.");
        }

        public async Task<T?> CompleteJsonAsync<T>(string systemPrompt, string userPrompt, LlmRole role = LlmRole.General, CancellationToken ct = default)
        {
            return await CompleteWithProvidersAsync<T?>(systemPrompt, userPrompt, role, response =>
            {
                var value = JsonSerializer.Deserialize<T>(ExtractJson(response), JsonOptions);
                if (value is null) throw new JsonException("The AI provider returned null JSON.");
                return value;
            }, ct);
        }

        public async Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
        {
            var response = await CompleteAsync(request.SystemPrompt, request.UserPrompt, LlmRole.General, ct);
            return new LlmCompletionResult
            {
                Success = true,
                RawText = response
            };
        }

        internal static bool IsConfiguredKey(string? key)
        {
            key = key?.Trim();
            return !string.IsNullOrWhiteSpace(key)
                && !key.Equals("change_me", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("changeme", StringComparison.OrdinalIgnoreCase)
                && !key.StartsWith("your_", StringComparison.OrdinalIgnoreCase)
                && !key.StartsWith("your-", StringComparison.OrdinalIgnoreCase)
                && !key.StartsWith("replace", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
                && !key.StartsWith('<');
        }

        private string GetProviderForRole(LlmRole role)
        {
            var roleKey = GetRoleKey(role);

            return _config["AI_DEFAULT_PROVIDER"]
                ?? _config["Ai:DefaultProvider"]
                ?? _config[$"Ai:Pipelines:{roleKey}:Provider"]
                ?? "Gemini";
        }

        private static string GetRoleKey(LlmRole role) => role switch
        {
            LlmRole.Model1_Intake => "Model1",
            LlmRole.Model2_Architect => "Model2",
            LlmRole.Model3_Builder => "Model3",
            LlmRole.Tutor => "Tutor",
            _ => "General"
        };

        private string[] GetProviderOrder(string preferred)
        {
            var providers = new List<string> { preferred };
            var allowCrossProviderFailover = bool.TryParse(
                _config["AI_ALLOW_CROSS_PROVIDER_FAILOVER"] ?? _config["Ai:AllowCrossProviderFailover"], out var enabled) && enabled;
            if (allowCrossProviderFailover) providers.AddRange(new[] { "Gemini", "Groq" });
            // Local Ollama is a safe last resort for hosted preferred providers;
            // switching between hosted vendors requires explicit deployment opt-in.
            if (!preferred.Equals("Ollama", StringComparison.OrdinalIgnoreCase)) providers.Add("Ollama");
            return providers.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static string ExtractJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Check ```json ... ```
            var match = Regex.Match(text, @"```(?:json)?\s*(\{[\s\S]*?\}|\[[\s\S]*?\])\s*```", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Find first '{' and last '}'
            var firstBrace = text.IndexOf('{');
            var lastBrace = text.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return text.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            // Find first '[' and last ']'
            var firstBracket = text.IndexOf('[');
            var lastBracket = text.LastIndexOf(']');
            if (firstBracket >= 0 && lastBracket > firstBracket)
            {
                return text.Substring(firstBracket, lastBracket - firstBracket + 1);
            }

            return text;
        }
    }
}
