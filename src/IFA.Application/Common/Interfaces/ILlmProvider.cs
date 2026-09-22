using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    /// <summary>
    /// A single upstream LLM provider (Gemini, Groq, a tuned endpoint, ...).
    /// The unified gateway selects providers through this abstraction.
    /// </summary>
    public interface ILlmProvider
    {
        /// <summary>Stable provider name used in routing configuration.</summary>
        string Name { get; }

        bool SupportsRole(AiModelRole role);

        Task<AiCompletionResult> CompleteAsync(
            AiCompletionRequest request,
            CancellationToken cancellationToken = default);
    }
}
