using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly FallbackLlmProvider _fallback;
        private readonly IConfiguration _config;
        private readonly ILogger<LlmGateway> _logger;

        public LlmGateway(
            GeminiLlmProvider gemini,
            GroqLlmProvider groq,
            OllamaLlmProvider ollama,
            FallbackLlmProvider fallback,
            IConfiguration config,
            ILogger<LlmGateway> logger)
        {
            _gemini = gemini;
            _groq = groq;
            _ollama = ollama;
            _fallback = fallback;
            _config = config;
            _logger = logger;
        }

        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, LlmRole role = LlmRole.General, CancellationToken ct = default)
        {
            var preferredProvider = GetProviderForRole(role);
            var providersToTry = GetProviderOrder(preferredProvider);

            foreach (var providerName in providersToTry)
            {
                try
                {
                    if (providerName.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && _gemini.IsConfigured)
                    {
                        _logger.LogInformation("Invoking Gemini LLM for role {Role}", role);
                        var model = _config[$"Ai:Pipelines:{role}:Model"] ?? _config["Ai:Gemini:Model"];
                        return await _gemini.GenerateAsync(systemPrompt, userPrompt, model, ct);
                    }
                    if (providerName.Equals("Groq", StringComparison.OrdinalIgnoreCase) && _groq.IsConfigured)
                    {
                        _logger.LogInformation("Invoking Groq LLM for role {Role}", role);
                        var model = _config[$"Ai:Pipelines:{role}:Model"] ?? _config["Ai:Groq:Model"];
                        return await _groq.GenerateAsync(systemPrompt, userPrompt, model, ct);
                    }
                    if (providerName.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Invoking Ollama LLM for role {Role}", role);
                        var model = _config[$"Ai:Pipelines:{role}:Model"] ?? _config["Ai:Ollama:Model"];
                        return await _ollama.GenerateAsync(systemPrompt, userPrompt, model, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {Provider} failed for role {Role}. Falling back to next provider.", providerName, role);
                }
            }

            // Fallback provider
            _logger.LogInformation("Using deterministic Fallback LLM provider for role {Role}", role);
            return await _fallback.CompleteAsync(systemPrompt, userPrompt, role, ct);
        }

        public async Task<T?> CompleteJsonAsync<T>(string systemPrompt, string userPrompt, LlmRole role = LlmRole.General, CancellationToken ct = default)
        {
            var response = await CompleteAsync(systemPrompt, userPrompt, role, ct);
            var json = ExtractJson(response);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("No JSON found in LLM response for role {Role}. Attempting fallback.", role);
                var fallbackRaw = await _fallback.CompleteAsync(systemPrompt, userPrompt, role, ct);
                json = ExtractJson(fallbackRaw);
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize LLM JSON response: {Json}", json);
                return default;
            }
        }

        public async Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
        {
            try
            {
                var response = await CompleteAsync(request.SystemPrompt, request.UserPrompt, LlmRole.General, ct);
                return new LlmCompletionResult
                {
                    Success = true,
                    RawText = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing LLM request");
                return new LlmCompletionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string GetProviderForRole(LlmRole role)
        {
            var roleKey = role switch
            {
                LlmRole.Model1_Intake => "Model1",
                LlmRole.Model2_Architect => "Model2",
                LlmRole.Model3_Builder => "Model3",
                LlmRole.Tutor => "Tutor",
                _ => "General"
            };

            return _config[$"Ai:Pipelines:{roleKey}:Provider"] 
                ?? _config["AI_DEFAULT_PROVIDER"] 
                ?? _config["Ai:DefaultProvider"] 
                ?? "Gemini";
        }

        private string[] GetProviderOrder(string preferred)
        {
            if (preferred.Equals("Groq", StringComparison.OrdinalIgnoreCase))
                return new[] { "Groq", "Gemini", "Ollama" };
            if (preferred.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
                return new[] { "Ollama", "Gemini", "Groq" };
            
            return new[] { "Gemini", "Groq", "Ollama" };
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
