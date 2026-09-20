using System;
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
    /// LEFT FOR THE BACKEND TEAM
    /// -------------------------
    /// 1. POST to {BaseUrl}/chat/completions with "Authorization: Bearer
    ///    {ApiKey}" and the configured Model.
    /// 2. Map SystemPrompt + Messages; when RequiresJson is true set
    ///    response_format = { "type": "json_object" }.
    /// 3. Map choices[0].message.content into AiCompletionResult.Content.
    ///    If the response exposes reasoning_content, copy it into
    ///    ThinkingContent and echo it back on the next turn through
    ///    AiChatMessage.ThinkingContent.
    /// 4. On HTTP 429 throw LlmProviderException(isRateLimited: true).
    /// </summary>
    public class GroqClient : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly AiGatewayOptions _options;

        public GroqClient(HttpClient httpClient, AiGatewayOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public string Name => "Groq";

        public bool SupportsRole(AiModelRole role) => _options.Groq.Enabled;

        public Task<AiCompletionResult> CompleteAsync(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException(
                "TODO (backend team): implement the Groq chat-completions call, JSON mode and reasoning_content passthrough. See the class comment for the exact contract.");
        }
    }
}
