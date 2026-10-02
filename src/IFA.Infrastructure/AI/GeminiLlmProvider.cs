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
    public class GeminiLlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiLlmProvider> _logger;

        public GeminiLlmProvider(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiLlmProvider> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured
        {
            get
            {
                var key = _configuration["GEMINI_API_KEY"] ?? _configuration["Ai:Gemini:ApiKey"];
                return !string.IsNullOrWhiteSpace(key) && key != "change_me";
            }
        }

        public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, string? modelOverride = null, CancellationToken ct = default)
        {
            var apiKey = _configuration["GEMINI_API_KEY"] ?? _configuration["Ai:Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("GEMINI_API_KEY is not configured.");
            }

            var model = modelOverride 
                ?? _configuration["Ai:Gemini:Model"] 
                ?? "gemini-2.0-flash";

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    maxOutputTokens = 4096
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, jsonContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API call failed: {StatusCode} - {Error}", response.StatusCode, err);
                throw new HttpRequestException($"Gemini API error: {response.StatusCode} - {err}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);

            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            var text = parts[0].GetProperty("text").GetString();

            return text ?? string.Empty;
        }
    }
}
