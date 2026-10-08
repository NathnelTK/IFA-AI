using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class OllamaLlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OllamaLlmProvider> _logger;

        public OllamaLlmProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaLlmProvider> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public string BaseUrl => _configuration["OLLAMA_BASE_URL"] 
            ?? _configuration["Ai:Ollama:BaseUrl"] 
            ?? "http://localhost:11434";

        public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, string? modelOverride = null, CancellationToken ct = default)
        {
            var model = modelOverride 
                ?? _configuration["Ai:Ollama:Model"] 
                ?? "llama3.1:8b";

            var endpoint = $"{BaseUrl.TrimEnd('/')}/api/generate";

            var requestBody = new
            {
                model = model,
                system = systemPrompt,
                prompt = userPrompt,
                stream = false,
                format = "json"
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, jsonContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Ollama API call returned status {StatusCode}: {Error}", response.StatusCode, err);
                throw new HttpRequestException($"Ollama API error: {response.StatusCode} - {err}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("response", out var resp))
            {
                return resp.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
