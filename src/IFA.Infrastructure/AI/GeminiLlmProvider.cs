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
                return LlmGateway.IsConfiguredKey(key);
            }
        }

        public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, string? modelOverride = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var apiKey = (_configuration["GEMINI_API_KEY"] ?? _configuration["Ai:Gemini:ApiKey"])?.Trim();
            if (!LlmGateway.IsConfiguredKey(apiKey))
            {
                throw new InvalidOperationException("GEMINI_API_KEY is not configured with a real provider key.");
            }

            var model = modelOverride 
                ?? _configuration["Ai:Gemini:Model"] 
                ?? "gemini-flash-latest";

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

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

            var serializedBody = JsonSerializer.Serialize(requestBody);

            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("x-goog-api-key", apiKey);
                request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");
                using var response = await _httpClient.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(responseJson);
                    var candidates = doc.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() == 0) return string.Empty;
                    var parts = candidates[0].GetProperty("content").GetProperty("parts");
                    return parts[0].GetProperty("text").GetString() ?? string.Empty;
                }

                var transient = response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                    || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    || (int)response.StatusCode >= 500;

                if (!transient || attempt == maxAttempts)
                {
                    throw new HttpRequestException($"Gemini API returned HTTP {(int)response.StatusCode}.", null, response.StatusCode);
                }

                _logger.LogWarning("Gemini API transient error {StatusCode} (attempt {Attempt}/{Max}); retrying.",
                    response.StatusCode, attempt, maxAttempts);
                response.Dispose();
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
            }

            throw new InvalidOperationException("Gemini did not return a response.");
        }
    }
}
