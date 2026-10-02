using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    public enum LlmRole
    {
        General,
        Model1_Intake,      // Learning Advisor & Research Orchestrator
        Model2_Architect,   // Course Architect
        Model3_Builder,     // Fine-Tuned Course Builder
        Tutor               // Grounded Socratic AI Tutor
    }

    public interface ILlmGateway
    {
        Task<string> CompleteAsync(string systemPrompt, string userPrompt, LlmRole role = LlmRole.General, CancellationToken ct = default);
        Task<T?> CompleteJsonAsync<T>(string systemPrompt, string userPrompt, LlmRole role = LlmRole.General, CancellationToken ct = default);
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
