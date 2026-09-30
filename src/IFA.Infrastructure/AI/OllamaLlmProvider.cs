// IFA.Infrastructure/AI/OllamaLlmProvider.cs
using System.Text;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class OllamaLlmProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private class OllamaGenerateResponse
        {
            public string Response { get; set; } = string.Empty;
        }

        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaLlmProvider> _logger;
        private readonly string _model;

        public OllamaLlmProvider(HttpClient httpClient, ILogger<OllamaLlmProvider> logger, string model = "llama3.2:3b")
        {
            _httpClient = httpClient;
            _logger = logger;
            _model = model;
        }

        public async Task<LlmCompletionResult> GenerateAsync(
            LlmCompletionRequest request,
            CancellationToken ct = default)
        {
            var payload = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["prompt"] = request.UserPrompt,
                ["system"] = request.SystemPrompt,
                ["stream"] = false,
                ["options"] = new
                {
                    temperature = request.Temperature,
                    num_predict = 18000
                }
            };

            // Only extraction calls set JsonSchemaHint. Ollama's "format": "json"
            // forces valid JSON output, which helps small models a lot.
            // Normal chat turns leave it unset and stay free-form text.
            if (request.JsonSchemaHint is not null)
                payload["format"] = "json";

            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/generate", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Ollama returned {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                    return new LlmCompletionResult { Success = false, ErrorMessage = $"Ollama error: {response.StatusCode}" };
                }

                var responseJson = await response.Content.ReadAsStringAsync(ct);

                // JsonOptions is passed here. This is the actual fix.
                var ollamaResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson, JsonOptions);
                var text = ollamaResponse?.Response ?? string.Empty;

                // An empty answer is a failure, not a success.
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Ollama returned an empty response. Raw body: {Raw}", responseJson);
                    return new LlmCompletionResult { Success = false, ErrorMessage = "Empty response from model." };
                }

                return new LlmCompletionResult { Success = true, RawText = text };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Ollama.");
                return new LlmCompletionResult { Success = false, ErrorMessage = $"Network error: {ex.Message}" };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Ollama request timed out.");
                return new LlmCompletionResult { Success = false, ErrorMessage = "Request timed out." };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Could not parse Ollama's response body.");
                return new LlmCompletionResult { Success = false, ErrorMessage = "Unreadable response from Ollama." };
            }
        }
    }
}