// IFA.Infrastructure/AI/OllamaLlmProvider.cs
using System.Text;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class OllamaLlmProvider
    {
        private class OllamaGenerateResponse
        {
            public string Response { get; set; } = string.Empty;
        }

        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaLlmProvider> _logger;
        private readonly string _model;

        public OllamaLlmProvider(HttpClient httpClient, ILogger<OllamaLlmProvider> logger, string model = "llama3.1:8b")
        {
            _httpClient = httpClient;
            _logger = logger;
            _model = model;
        }

        public async Task<LlmCompletionResult> GenerateAsync(
            LlmCompletionRequest request,
            CancellationToken ct = default)
        {
            var payload = new
            {
                model = _model,
                prompt = request.UserPrompt,
                system = request.SystemPrompt,
                stream = false,
                options = new { temperature = request.Temperature }
            };

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
                var ollamaResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson);

                return new LlmCompletionResult
                {
                    Success = true,
                    RawText = ollamaResponse?.Response ?? string.Empty
                };
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
        }
    }
}