using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class GroqLlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GroqLlmProvider> _logger;

        public GroqLlmProvider(HttpClient httpClient, IConfiguration configuration, ILogger<GroqLlmProvider> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured
        {
            get
            {
                var key = _configuration["GROQ_API_KEY"] ?? _configuration["Ai:Groq:ApiKey"];
                return !string.IsNullOrWhiteSpace(key)
                    && key != "change_me"
                    && !key.StartsWith("your_", StringComparison.OrdinalIgnoreCase);
            }
        }

        public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, string? modelOverride = null, CancellationToken ct = default)
        {
            var apiKey = _configuration["GROQ_API_KEY"] ?? _configuration["Ai:Groq:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)
                || apiKey == "change_me"
                || apiKey.StartsWith("your_", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("GROQ_API_KEY is not configured with a real provider key.");
            }

            var model = modelOverride 
                ?? _configuration["Ai:Groq:Model"] 
                ?? "llama-3.3-70b-versatile";

            const string endpoint = "https://api.groq.com/openai/v1/chat/completions";

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.5,
                max_tokens = 4096
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Groq API call failed: {StatusCode} - {Error}", response.StatusCode, err);
                throw new HttpRequestException($"Groq API error: {response.StatusCode} - {err}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);

            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var content = choices[0].GetProperty("message").GetProperty("content").GetString();
            return content ?? string.Empty;
        }
    }
}
