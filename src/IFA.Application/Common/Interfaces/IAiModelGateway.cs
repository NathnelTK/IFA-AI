using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    /// <summary>
    /// The three model responsibilities defined by the IFA pipeline.
    /// </summary>
    public enum AiModelRole
    {
        LearningAdvisor = 1,
        CourseArchitect = 2,
        CourseBuilder = 3
    }

    /// <summary>
    /// A single conversational turn passed to a model.
    /// </summary>
    public class AiChatMessage
    {
        public string Role { get; set; } = "user";

        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Structured chain-of-thought returned by a reasoning provider.
        /// Providers that emit thinking content (AgentRouter/Anthropic-style
        /// <c>content[].thinking</c>) require the previous assistant turn's
        /// thinking block to be echoed back verbatim on the next request,
        /// otherwise multi-turn calls are rejected or lose coherence.
        /// </summary>
        public string? ThinkingContent { get; set; }
    }

    public class AiCompletionRequest
    {
        public AiModelRole Role { get; set; }

        public string SystemPrompt { get; set; } = string.Empty;

        public List<AiChatMessage> Messages { get; set; } = new();

        /// <summary>Ask the provider for a strict JSON object response.</summary>
        public bool RequiresJson { get; set; }

        /// <summary>Optional JSON Schema used to constrain structured output.</summary>
        public string? JsonSchema { get; set; }

        public double Temperature { get; set; } = 0.4;

        public int MaxOutputTokens { get; set; } = 2048;
    }

    public class AiCompletionResult
    {
        public AiModelRole Role { get; set; }

        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Reasoning content returned by the provider. Persist this with the
        /// conversation turn and send it back through
        /// <see cref="AiChatMessage.ThinkingContent"/> on the next request.
        /// </summary>
        public string? ThinkingContent { get; set; }

        public string ProviderName { get; set; } = string.Empty;

        public bool UsedFallback { get; set; }

        public int? PromptTokens { get; set; }

        public int? CompletionTokens { get; set; }
    }

    /// <summary>
    /// Single application-facing entry point for Model 1, Model 2 and Model 3.
    /// Implementations are responsible for provider routing, free-tier rate
    /// limit handling and fallback behaviour.
    /// </summary>
    public interface IAiModelGateway
    {
        Task<AiCompletionResult> CompleteAsync(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default);

        Task<T?> CompleteAsJsonAsync<T>(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default);
    }
}
