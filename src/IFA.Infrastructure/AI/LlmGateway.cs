// IFA.Infrastructure/AI/LlmGateway.cs
using IFA.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class LlmGateway : ILlmGateway
    {
        private readonly OllamaLlmProvider _ollama;
        private readonly ILogger<LlmGateway> _logger;
        private readonly bool _useOllama;

        public LlmGateway(OllamaLlmProvider ollama, ILogger<LlmGateway> logger, IConfiguration config)
        {
            _ollama = ollama;
            _logger = logger;
            _useOllama = config.GetValue<bool>("LlmGateway:UseOllama", defaultValue: true);
        }

        public async Task<LlmCompletionResult> CompleteAsync(
            LlmCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!_useOllama)
            {
                _logger.LogWarning("Ollama is disabled in config; no fallback provider configured yet.");
                return new LlmCompletionResult { Success = false, ErrorMessage = "No LLM provider configured." };
            }

            _logger.LogDebug("Calling Ollama with temperature {Temperature}.", request.Temperature);
            var result = await _ollama.GenerateAsync(request, cancellationToken);

            if (!result.Success)
                _logger.LogWarning("Ollama failed: {Error}. No fallback configured yet.", result.ErrorMessage);

            return result;
        }
    }
}