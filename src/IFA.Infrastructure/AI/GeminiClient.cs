using System;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.AI
{
    /// <summary>
    /// Google Gemini free-tier provider (Model roles: Learning Advisor,
    /// Course Architect, Course Builder fallback).
    ///
    /// LEFT FOR THE BACKEND TEAM
    /// -------------------------
    /// 1. POST to {BaseUrl}/models/{Model}:generateContent with the API key
    ///    from <see cref="ProviderOptions.ApiKey"/> (never hard-code it).
    /// 2. Map system prompt + <see cref="AiCompletionRequest.Messages"/> into
    ///    Gemini "contents" parts; when RequiresJson is true send
    ///    generationConfig.responseMimeType = "application/json" and pass
    ///    JsonSchema through responseSchema.
    /// 3. Map the candidate text into AiCompletionResult.Content and, when the
    ///    response contains reasoning/thinking parts, copy them into
    ///    AiCompletionResult.ThinkingContent.
    /// 4. On HTTP 429 throw LlmProviderException(isRateLimited: true) so the
    ///    gateway falls back to Groq.
    /// </summary>
    public class GeminiClient : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly AiGatewayOptions _options;

        public GeminiClient(HttpClient httpClient, AiGatewayOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public string Name => "Gemini";

        public bool SupportsRole(AiModelRole role) => _options.Gemini.Enabled;

        public Task<AiCompletionResult> CompleteAsync(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(
                "TODO (backend team): implement the Gemini generateContent call, structured JSON output and rate-limit mapping. See the class comment for the exact contract.");
        }
    }
}
