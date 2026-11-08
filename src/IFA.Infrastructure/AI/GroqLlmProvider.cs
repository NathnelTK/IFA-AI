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
                return LlmGateway.IsConfiguredKey(key);
            }
        }

        public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, string? modelOverride = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var apiKey = (_configuration["GROQ_API_KEY"] ?? _configuration["Ai:Groq:ApiKey"])?.Trim();
            if (!LlmGateway.IsConfiguredKey(apiKey))
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

            var serializedBody = JsonSerializer.Serialize(requestBody);
            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // HttpRequestMessage cannot be sent twice, including on a transient retry.
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");
                using var response = await _httpClient.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(responseJson);
                    var choices = doc.RootElement.GetProperty("choices");
                    return choices.GetArrayLength() == 0
                        ? string.Empty
                        : choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
                }

                var transient = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    || (int)response.StatusCode >= 500;

                if (!transient || attempt == maxAttempts)
                {
                    throw new HttpRequestException($"Groq API returned HTTP {(int)response.StatusCode}.", null, response.StatusCode);
                }

                _logger.LogWarning("Groq API transient error {StatusCode} (attempt {Attempt}/{Max}); retrying.",
                    response.StatusCode, attempt, maxAttempts);
                response.Dispose();
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
            }

            throw new InvalidOperationException("Groq did not return a response.");
        }
    }
}
