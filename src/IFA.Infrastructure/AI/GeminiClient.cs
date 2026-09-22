using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.AI
{
    /// <summary>
    /// Google Gemini free-tier provider (Model roles: Learning Advisor,
    /// Course Architect, Course Builder fallback).
    ///
    /// Talks to the Generative Language REST API:
    ///   POST {BaseUrl}/models/{Model}:generateContent
    /// authenticating with the <c>x-goog-api-key</c> header. The system prompt
    /// maps to <c>systemInstruction</c> and the conversation to <c>contents</c>.
    /// When <see cref="AiCompletionRequest.RequiresJson"/> is set we ask for
    /// <c>responseMimeType = application/json</c> (plus <c>responseSchema</c>
    /// when a schema is supplied). HTTP 429/503 are surfaced as rate-limit
    /// failures so <see cref="FallbackAiService"/> can fall through to Groq.
    /// </summary>
    public class GeminiClient : ILlmProvider
    {
        private const string DefaultBaseUrl = "https://generativelanguage.googleapis.com/v1beta";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly AiGatewayOptions _options;

        public GeminiClient(HttpClient httpClient, AiGatewayOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public string Name => "Gemini";

        public bool SupportsRole(AiModelRole role) => _options.Gemini.Enabled;

        public async Task<AiCompletionResult> CompleteAsync(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            var settings = _options.Gemini;

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new LlmProviderException(Name, "Gemini API key is not configured (set GEMINI_API_KEY).");
            }

            if (string.IsNullOrWhiteSpace(settings.Model))
            {
                throw new LlmProviderException(Name, "Gemini model is not configured (set GEMINI_MODEL).");
            }

            var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl) ? DefaultBaseUrl : settings.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/models/{settings.Model}:generateContent";
            var json = JsonSerializer.Serialize(BuildRequestBody(request), SerializerOptions);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("x-goog-api-key", settings.ApiKey);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new LlmProviderException(Name, $"Gemini request timed out after {settings.TimeoutSeconds}s.");
            }
            catch (HttpRequestException exception)
            {
                throw new LlmProviderException(Name, $"Gemini transport failure: {exception.Message}", innerException: exception);
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var rateLimited = response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
                throw new LlmProviderException(
                    Name,
                    $"Gemini returned HTTP {(int)response.StatusCode}: {Truncate(payload)}",
                    isRateLimited: rateLimited);
            }

            return ParseResponse(payload);
        }

        private static Dictionary<string, object?> BuildRequestBody(AiCompletionRequest request)
        {
            var contents = new List<object>();
            foreach (var message in request.Messages)
            {
                var role = message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                        || message.Role.Equals("model", StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user";

                contents.Add(new Dictionary<string, object?>
                {
                    ["role"] = role,
                    ["parts"] = new[] { new Dictionary<string, object?> { ["text"] = message.Content } }
                });
            }

            var generationConfig = new Dictionary<string, object?>
            {
                ["temperature"] = request.Temperature,
                ["maxOutputTokens"] = request.MaxOutputTokens
            };

            if (request.RequiresJson)
            {
                generationConfig["responseMimeType"] = "application/json";

                if (!string.IsNullOrWhiteSpace(request.JsonSchema) && TryParseJson(request.JsonSchema!, out var schema))
                {
                    generationConfig["responseSchema"] = schema;
                }
            }

            var body = new Dictionary<string, object?>
            {
                ["contents"] = contents,
                ["generationConfig"] = generationConfig
            };

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                body["systemInstruction"] = new Dictionary<string, object?>
                {
                    ["parts"] = new[] { new Dictionary<string, object?> { ["text"] = request.SystemPrompt } }
                };
            }

            return body;
        }

        private AiCompletionResult ParseResponse(string payload)
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates)
                || candidates.ValueKind != JsonValueKind.Array
                || candidates.GetArrayLength() == 0)
            {
                var reason = root.TryGetProperty("promptFeedback", out var feedback)
                             && feedback.TryGetProperty("blockReason", out var block)
                    ? block.GetString()
                    : "no candidates returned";
                throw new LlmProviderException(Name, $"Gemini produced no content ({reason}).");
            }

            var content = new StringBuilder();
            var thinking = new StringBuilder();

            if (candidates[0].TryGetProperty("content", out var contentElement)
                && contentElement.TryGetProperty("parts", out var parts)
                && parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (!part.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    // Thinking models flag reasoning summaries with "thought": true.
                    if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True)
                    {
                        thinking.Append(text.GetString());
                    }
                    else
                    {
                        content.Append(text.GetString());
                    }
                }
            }

            var result = new AiCompletionResult
            {
                Content = content.ToString(),
                ThinkingContent = thinking.Length > 0 ? thinking.ToString() : null
            };

            if (root.TryGetProperty("usageMetadata", out var usage))
            {
                if (usage.TryGetProperty("promptTokenCount", out var promptTokens) && promptTokens.TryGetInt32(out var pt))
                {
                    result.PromptTokens = pt;
                }

                if (usage.TryGetProperty("candidatesTokenCount", out var completionTokens) && completionTokens.TryGetInt32(out var ct))
                {
                    result.CompletionTokens = ct;
                }
            }

            if (string.IsNullOrEmpty(result.Content) && result.ThinkingContent is null)
            {
                throw new LlmProviderException(Name, "Gemini returned an empty completion.");
            }

            return result;
        }

        private static bool TryParseJson(string raw, out JsonElement element)
        {
            try
            {
                using var document = JsonDocument.Parse(raw);
                element = document.RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                element = default;
                return false;
            }
        }

        private static string Truncate(string value, int max = 500)
            => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max] + "…";
    }
}
