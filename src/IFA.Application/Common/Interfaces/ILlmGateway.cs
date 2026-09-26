

namespace IFA.Application.Common.Interfaces
{
    public interface ILlmGateway
    {
        Task<LlmCompletionResult> CompleteAsync(
            LlmCompletionRequest request,
            CancellationToken ct = default
        );
    }
    public class LlmCompletionRequest
    {
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;

        public double Temperature { get; set; } = 0.3f;

        public string? JsonSchemaHint { get; set; }
    }
    public class LlmCompletionResult
    {
        public bool Success { get; set; }
        public string RawText { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }
}




