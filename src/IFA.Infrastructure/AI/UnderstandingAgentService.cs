using System.Text;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.AI
{
    public class UnderstandingAgentService : IUnderstandingAgentService
    {
        private readonly ILlmGateway _llmGateway;
        private readonly ILogger<UnderstandingAgentService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // private const string ConversationSystemPrompt = """
        //     You are IFA's learning advisor. Have a natural, brief conversation
        //     to understand what the learner wants to learn, their current level,
        //     constraints (time available, preferred language/style), and any
        //     preferred resources. Ask ONE focused question at a time. Do not
        //     ask about things already answered. Do not output JSON here - just
        //     talk naturally. Keep replies short (2-4 sentences).
        //     """;


        private const string ConversationSystemPrompt = """
    You are IFA's learning advisor talking with a learner.
    Your job is to learn enough to build a personalized course.
    Cover these topics one at a time, in this order, skipping any the learner already answered:
    1. What they want to learn and what they want to build or achieve
    2. Their current experience level
    3. Study time available per week
    4. Preferred language and learning style (videos, reading, projects)
    5. Preferred channels or resources, and any constraints

    Rules:
    - Ask exactly ONE short question per reply (1-3 sentences).
    - Base your reply only on what the Learner actually wrote. Never claim they said something they did not.
    - Never comment on repetition and never guess how the learner feels.
    - When all five topics are covered, say you have what you need and they can press "Generate my course".
    - Plain text only. No JSON, no lists.
    """;
        private const string ExtractionSystemPrompt = """ You are IFA's learner profile extraction agent. Your job is to read the complete conversation between IFA's learning advisor and the learner and extract a structured learner profile. IMPORTANT RULES: 1. Read the ENTIRE conversation. 2. Pay attention especially to the LEARNER's answers. 3. The learner may answer questions using natural language. 4. Normalize natural language into useful structured values. 5. Never invent information. 6. Never use the string "none" for missing values. 7. Use null for a missing scalar value. 8. Use [] for a missing list value. 9. The goal is required. If the learner clearly explains what they want to learn, build, or achieve, extract it. 10. subjectTopic should identify the main subject or technology. 11. If the learner says they know the basics of a technology, currentLevel should normally be "beginner". 12. If the learner says they have more than a certain number of study hours, use that number as the minimum numeric value. 13. If the learner mentions a preferred programming language, extract it. 14. If the learner mentions a YouTube channel, extract the channel name. 15. If the learner mentions a limitation such as internet problems, extract it as a constraint. 16. If the learner explains how they prefer to learn, extract it as preferredLearningStyle. 17. targetOutcome should describe the concrete result the learner wants to achieve. 18. Do not confuse the advisor's questions with information supplied by the learner. EXAMPLES: Learner: "I know the basics of Flutter and Dart." Extract: "currentLevel": "beginner" Learner: "I spend more than 20 hours per week." Extract: "availableStudyHoursPerWeek": 20 Learner: "Build minimarket mobile app and deploy it on playstore." Extract: "goal": "Build a minimarket mobile app and deploy it on the Play Store" "targetOutcome": "Build and deploy a minimarket mobile app" Learner: "I prefer dart." Extract: "preferredLanguage": "Dart" Learner: "Ninja youtube videos." Extract: "preferredYouTubeChannels": ["Ninja"] Learner: "Internet connection problems." Extract: "constraints": ["Internet connection problems"] Learner: "working on hands-on projects with code examples." Extract: "preferredLearningStyle": "hands-on projects with code examples" Return ONLY valid JSON. Do not return markdown. Do not return ```json. Do not return explanations. Do not return comments. Use exactly this JSON structure: { "goal": "string or null", "subjectTopic": "string or null", "currentLevel": "string or null", "targetOutcome": "string or null", "availableStudyHoursPerWeek": "number or null", "preferredLanguage": "string or null", "preferredLearningStyle": "string or null", "constraints": [], "preferredYouTubeChannels": [], "knownStrengths": [], "knownWeaknesses": [] } """;

        public UnderstandingAgentService(ILlmGateway llmGateway, ILogger<UnderstandingAgentService> logger)
        {
            _llmGateway = llmGateway;
            _logger = logger;
        }

        public async Task<string> ContinueConversationAsync(
            ChatSession session, string learnerMessage, CancellationToken ct = default)
        {
            var transcript = BuildTranscript(session, includeLatestUserMessage: learnerMessage);

            var result = await _llmGateway.CompleteAsync(new LlmCompletionRequest
            {
                SystemPrompt = ConversationSystemPrompt,
                UserPrompt = transcript + "\nWrite ONLY the advisor 's next reply.",
                Temperature = 0.6
            }, ct);

            // Fix 1: check the text itself, not only the Success flag.
            if (!result.Success || string.IsNullOrWhiteSpace(result.RawText))
            {
                _logger.LogWarning("Conversation turn produced no usable text. Error: {Error}", result.ErrorMessage);
                return "Sorry, I'm having trouble responding right now - could you try again?";
            }

            return result.RawText.Trim();
        }

        public async Task<LearnerProfile?> ExtractProfileAsync(ChatSession session, CancellationToken ct = default)
        {
            var transcript = BuildTranscript(session, includeLatestUserMessage: null);

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                // Fix 4: the retry tells the model what went wrong.
                var prompt = attempt == 1
                    ? transcript
                    : transcript + "\n\nYour previous reply was not valid JSON. Reply with ONLY the JSON object.";

                var result = await _llmGateway.CompleteAsync(new LlmCompletionRequest
                {
                    SystemPrompt = ExtractionSystemPrompt,
                    UserPrompt = prompt,
                    Temperature = 0.1,
                    JsonSchemaHint = "see system prompt"
                }, ct);

                if (!result.Success)
                {
                    _logger.LogWarning("Extraction attempt {Attempt}: LLM call failed: {Error}", attempt, result.ErrorMessage);
                    continue;
                }

                try
                {
                    // Fix 3: cut out just the {...} part, ignoring fences or chatter around it.
                    var json = ExtractJsonObject(result.RawText);
                    var dto = JsonSerializer.Deserialize<ExtractedProfileDto>(json, JsonOptions);

                    if (dto is null || string.IsNullOrWhiteSpace(dto.Goal))
                    {
                        _logger.LogWarning("Extraction attempt {Attempt}: missing required Goal field.", attempt);
                        continue;
                    }

                    return MapToProfile(dto, session.LearnerId);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Extraction attempt {Attempt}: invalid JSON. Raw output: {Raw}",
                        attempt, result.RawText);
                }
            }

            // Fix 2: this log now runs once, after both attempts have failed.
            _logger.LogError("Profile extraction failed after retry for session {SessionId}.", session.Id);
            return null;
        }

        private static string ExtractJsonObject(string raw)
        {
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            return start >= 0 && end > start ? raw[start..(end + 1)] : raw;
        }

        private static string BuildTranscript(ChatSession session, string? includeLatestUserMessage)
        {
            var sb = new StringBuilder("Converstation so far:\n");
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
}