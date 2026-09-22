using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.AI
{
    /// <summary>
    /// Groq fallback provider (OpenAI-compatible chat completions API,
    /// e.g. Llama 3.3). Used automatically when the primary provider is
    /// rate limited or unavailable.
    ///
    /// Talks to POST {BaseUrl}/chat/completions with a bearer token. When
    /// <see cref="AiCompletionRequest.RequiresJson"/> is set we send
    /// <c>response_format = { "type": "json_object" }</c>; Groq rejects the
    /// default <c>raw</c> reasoning format in JSON mode, so we also request
    /// <c>reasoning_format = parsed</c> which surfaces reasoning models' chain
    /// of thought in a dedicated <c>message.reasoning</c> field. That reasoning
    /// (or <c>&lt;think&gt;</c> tags in raw mode) is copied into
    /// <see cref="AiCompletionResult.ThinkingContent"/>. HTTP 429/503 are
    /// surfaced as rate-limit failures so the gateway can fall through.
    /// </summary>
    public class GroqClient : ILlmProvider
    {
        private const string DefaultBaseUrl = "https://api.groq.com/openai/v1";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly Regex ThinkTagPattern = new(
            "<think>(.*?)</think>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly HttpClient _httpClient;
        private readonly AiGatewayOptions _options;

        public GroqClient(HttpClient httpClient, AiGatewayOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public string Name => "Groq";

        public bool SupportsRole(AiModelRole role) => _options.Groq.Enabled;

        public async Task<AiCompletionResult> CompleteAsync(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            var settings = _options.Groq;

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new LlmProviderException(Name, "Groq API key is not configured (set GROQ_API_KEY).");
            }

            if (string.IsNullOrWhiteSpace(settings.Model))
            {
                throw new LlmProviderException(Name, "Groq model is not configured (set GROQ_MODEL).");
            }

            var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/chat/completions";
            var json = JsonSerializer.Serialize(BuildRequestBody(request, settings.Model), SerializerOptions);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new LlmProviderException(Name, $"Groq request timed out after {settings.TimeoutSeconds}s.");
            }
            catch (HttpRequestException exception)
            {
                throw new LlmProviderException(Name, $"Groq transport failure: {exception.Message}", innerException: exception);
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var rateLimited = response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
                throw new LlmProviderException(
                    Name,
                    $"Groq returned HTTP {(int)response.StatusCode}: {Truncate(payload)}",
                    isRateLimited: rateLimited);
            }

            return ParseResponse(payload);
        }

        private static Dictionary<string, object?> BuildRequestBody(AiCompletionRequest request, string model)
        {
            var messages = new List<object>();

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "system",
                    ["content"] = request.SystemPrompt
                });
            }

            foreach (var message in request.Messages)
            {
                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = NormalizeRole(message.Role),
                    ["content"] = message.Content
                });
            }

            var body = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["messages"] = messages,
                ["temperature"] = request.Temperature,
                ["max_tokens"] = request.MaxOutputTokens
            };

            if (request.RequiresJson)
            {
                body["response_format"] = new Dictionary<string, object?> { ["type"] = "json_object" };
                // Reasoning models reject the default "raw" format alongside JSON
                // mode with HTTP 400; "parsed" keeps them working.
                body["reasoning_format"] = "parsed";
            }

            return body;
        }

        private static string NormalizeRole(string role)
        {
            if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                || role.Equals("model", StringComparison.OrdinalIgnoreCase))
            {
                return "assistant";
            }

            return role.Equals("system", StringComparison.OrdinalIgnoreCase) ? "system" : "user";
        }

        private AiCompletionResult ParseResponse(string payload)
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0
                || !choices[0].TryGetProperty("message", out var message))
            {
                throw new LlmProviderException(Name, "Groq returned no choices.");
            }

            var content = message.TryGetProperty("content", out var contentElement)
                          && contentElement.ValueKind == JsonValueKind.String
                ? contentElement.GetString() ?? string.Empty
                : string.Empty;

            string? thinking = null;

            if (message.TryGetProperty("reasoning", out var reasoning) && reasoning.ValueKind == JsonValueKind.String)
            {
                thinking = reasoning.GetString();
            }
            else if (message.TryGetProperty("reasoning_content", out var reasoningContent) && reasoningContent.ValueKind == JsonValueKind.String)
            {
                thinking = reasoningContent.GetString();
            }
            else if (content.Contains("<think>", StringComparison.OrdinalIgnoreCase))
            {
                // "raw" reasoning format embeds the chain of thought inline.
                var match = ThinkTagPattern.Match(content);
                if (match.Success)
                {
                    thinking = match.Groups[1].Value.Trim();
                    content = ThinkTagPattern.Replace(content, string.Empty).Trim();
                }
            }

            var result = new AiCompletionResult
            {
                Content = content,
                ThinkingContent = string.IsNullOrWhiteSpace(thinking) ? null : thinking
            };

            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var promptTokens) && promptTokens.TryGetInt32(out var pt))
                {
                    result.PromptTokens = pt;
                }

                if (usage.TryGetProperty("completion_tokens", out var completionTokens) && completionTokens.TryGetInt32(out var ct))
                {
                    result.CompletionTokens = ct;
                }
            }

            if (string.IsNullOrEmpty(result.Content))
            {
                throw new LlmProviderException(Name, "Groq returned an empty completion.");
            }

            return result;
        }

        private static string Truncate(string value, int max = 500)
            => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max] + "…";
    }
}
