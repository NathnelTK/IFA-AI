

using System.Text;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

public class UnderstandingAgentService : IUnderstandingAgentService
{

    private readonly ILlmGateway _llmGateway;
    private readonly ILogger<UnderstandingAgentService> _logger;


    private const string ConversationSystemPrompt = """
           You are IFA's learning advisor. Have a natural, brief conversation
            to understand what the learner wants to learn, their current level,
            constraints (time available, preferred language/style), and any
            preferred resources. Ask ONE focused question at a time. Do not
            ask about things already answered. Do not output JSON here - just
            talk naturally. Keep replies short (2-4 sentences).
""";

    private const string ExtractionSystemPrompt = """
            Read the full conversation transcript below. Extract a structured
            learner profile as JSON matching this exact shape (omit fields
            never discussed - do not invent information):
            {
              "goal": "string, required",
              "subjectTopic": "string or null",
              "currentLevel": "string or null",
              "targetOutcome": "string or null",
              "availableStudyHoursPerWeek": "number or null",
              "preferredLanguage": "string or null",
              "preferredLearningStyle": "string or null",
              "constraints": ["string", ...],
              "preferredYouTubeChannels": ["string", ...],
              "knownStrengths": ["string", ...],
              "knownWeaknesses": ["string", ...]
            }
            Respond with ONLY the JSON object, no prose, no markdown fences.
            """;

    public UnderstandingAgentService(ILlmGateway llmGateway,
    ILogger<UnderstandingAgentService> logger)
    {
        _llmGateway = llmGateway;
        _logger = logger;
    }

    public async Task<string> ContinueConverstaionAsync(ChatSession session, string learnerMessage, CancellationToken ct)
    {
        var transcript = BuildTranscript(session, includeLatestUserMessage: learnerMessage);

        var result = await _llmGateway.ComplateAsync(new LlmCompletionRequest
        {
            ExtractionSystemPrompt = ConversationSystemPrompt,
            UserPrompt = transcript,
            Temprature = 0.6f
        }, ct);

        return result.Success
               ? result.RawText.Trim()
               : "Sorry, I'm having trouble responding right now - could you try again?";
    }

    public async Task<LearnerProfile?> ExtractProfileAsync(ChatSession session, CancellationToken ct = default)
    {
        var transcript = BuildTranscript(session, includeLatestUserMessage: null);

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var result = await _llmGateway.ComplateAsync(new LlmCompletionRequest
            {
                SystemPrompt = ExtractionSystemPrompt,
                UserPrompt = transcript,
                Temperature = 0.1f, // low - this needs to be precise, not creative
                JsonSchemaHint = "see system prompt"
            }, ct);
            if (!result.Success) continue;

            try
            {
                var dto = JsonSerializer.Deserialize<ExtractedProfileDto>(
                       result.RawText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto is null || string.IsNullOrWhiteSpace(dto.Goal))
                {
                    _logger.LogWarning("Extraction attempt {Attempt}: missing required Goal field.", attempt);
                    continue;
                }

                return MapToProfile(dto, session.LearnerId);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Extraction attempt {Attempt}: invalid JSON from LLM.", attempt);
            }
            _logger.LogError("Profile extraction failed after retry for session {SessionId}.", session.Id);
            return null;
        }
    }
    private static string BuildTranscript(ChatSession session, string? includeLatestUserMessage)
    {
        var sb = new StringBuilder();
        foreach (var msg in session.Messages.OrderBy(m => m.SequenceNumber))
            sb.AppendLine($"{msg.Role}: {msg.Content}");

        if (includeLatestUserMessage is not null)
            sb.AppendLine($"User: {includeLatestUserMessage}");

        return sb.ToString();
    }

    private static LearnerProfile MapToProfile(ExtractedProfileDto dto, Guid learnerId) => new()
    {
        LearnerId = learnerId,
        Goal = dto.Goal!,
        SubjectTopic = dto.SubjectTopic,
        CurrentLevel = dto.CurrentLevel,
        TargetOutcome = dto.TargetOutcome,
        AvailableStudyHoursPerWeek = dto.AvailableStudyHoursPerWeek,
        PreferredLanguage = dto.PreferredLanguage,
        PreferredLearningStyle = dto.PreferredLearningStyle,
        Constraints = dto.Constraints ?? new(),
        PreferredYouTubeChannels = dto.PreferredYouTubeChannels ?? new(),
        KnownStrengths = dto.KnownStrengths ?? new(),
        KnownWeaknesses = dto.KnownWeaknesses ?? new()
    };

    public Task<string> ContinueConversationAsync(ChatSession session, string learnerMessage, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private class ExtractedProfileDto
    {
        public string? Goal { get; set; }
        public string? SubjectTopic { get; set; }
        public string? CurrentLevel { get; set; }
        public string? TargetOutcome { get; set; }
        public int? AvailableStudyHoursPerWeek { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? PreferredLearningStyle { get; set; }
        public List<string>? Constraints { get; set; }
        public List<string>? PreferredYouTubeChannels { get; set; }
        public List<string>? KnownStrengths { get; set; }
        public List<string>? KnownWeaknesses { get; set; }
    }
}
