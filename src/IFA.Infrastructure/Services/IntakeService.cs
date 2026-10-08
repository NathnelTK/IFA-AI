using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Services
{
    public class IntakeService : IIntakeService
    {
        private readonly ILlmGateway _llmGateway;
        private readonly IApplicationDbContext _context;

        public IntakeService(ILlmGateway llmGateway, IApplicationDbContext context)
        {
            _llmGateway = llmGateway;
            _context = context;
        }

        public async Task<IntakeResponse> ProcessMessageAsync(Guid learnerId, string userMessage, List<IntakeMessageDto> conversationHistory, CancellationToken ct = default)
        {
            var historyText = string.Join("\n", (conversationHistory ?? new List<IntakeMessageDto>())
                .Select(h => $"{h.Role.ToUpper()}: {h.Content}"));

            var userPrompt = $"CONVERSATION HISTORY:\n{historyText}\n\nCURRENT USER MESSAGE:\n{userMessage}";

            var rawResponse = await _llmGateway.CompleteAsync(
                PromptRegistry.Model1_IntakeSystemPrompt,
                userPrompt,
                LlmRole.Model1_Intake,
                ct);

            var response = new IntakeResponse();

            // Extract JSON if model returned structured profile
            var match = Regex.Match(rawResponse, @"```(?:json)?\s*(\{[\s\S]*?\})\s*```", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var jsonStr = match.Groups[1].Value.Trim();
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var parsed = JsonSerializer.Deserialize<IntakeProfilePayload>(jsonStr, options);
                    if (parsed != null)
                    {
                        response.IsProfileReady = true;
                        response.SuggestedResearchTopics = parsed.SuggestedResearchTopics ?? new List<string>();
                        response.Profile = new LearnerProfileDto
                        {
                            LearningGoal = parsed.LearningGoal ?? userMessage,
                            Subject = parsed.Subject ?? "Software Engineering",
                            CurrentLevel = parsed.CurrentLevel ?? "Beginner",
                            TargetOutcome = parsed.TargetOutcome ?? "Mastery",
                            WeeklyStudyHours = parsed.WeeklyStudyHours > 0 ? parsed.WeeklyStudyHours : 5,
                            PreferredLanguage = parsed.PreferredLanguage ?? "en",
                            LearningStyle = parsed.LearningStyle ?? "Hands-on",
                            Constraints = parsed.Constraints ?? "",
                            PreferredYouTubeChannels = parsed.PreferredYouTubeChannels ?? new List<string>(),
                            KnownStrengths = parsed.KnownStrengths ?? new List<string>(),
                            KnownWeaknesses = parsed.KnownWeaknesses ?? new List<string>()
                        };

                        // Persist to DB
                        var existing = await _context.LearnerProfiles.FirstOrDefaultAsync(p => p.LearnerId == learnerId, ct);
                        if (existing == null)
                        {
                            existing = new LearnerProfile
                            {
                                Id = Guid.NewGuid(),
                                LearnerId = learnerId
                            };
                            _context.Add(existing);
                        }

                        existing.LearningGoal = response.Profile.LearningGoal;
                        existing.Subject = response.Profile.Subject;
                        existing.CurrentLevel = response.Profile.CurrentLevel;
                        existing.TargetOutcome = response.Profile.TargetOutcome;
                        existing.WeeklyStudyHours = response.Profile.WeeklyStudyHours;
                        existing.PreferredLanguage = response.Profile.PreferredLanguage;
                        existing.LearningStyle = response.Profile.LearningStyle;
                        existing.Constraints = response.Profile.Constraints;
                        existing.PreferredYouTubeChannelsJson = JsonSerializer.Serialize(response.Profile.PreferredYouTubeChannels);
                        existing.KnownStrengthsJson = JsonSerializer.Serialize(response.Profile.KnownStrengths);
                        existing.KnownWeaknessesJson = JsonSerializer.Serialize(response.Profile.KnownWeaknesses);
                        existing.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync(ct);
                    }
                }
                catch
                {
                    // Fall through and present conversational reply
                }

                // Clean the conversational reply by removing the JSON codeblock
                response.Reply = Regex.Replace(rawResponse, @"```(?:json)?[\s\S]*?```", "").Trim();
                if (string.IsNullOrWhiteSpace(response.Reply))
                {
                    response.Reply = "I have prepared your personalized learning profile! Would you like me to build your custom course now?";
                }
            }
            else
            {
                response.Reply = rawResponse.Trim();
                response.IsProfileReady = false;
                response.FollowUpQuestions = new List<string>
                {
                    "What specific projects do you want to build?",
                    "How many hours per week can you dedicate?"
                };
            }

            return response;
        }

        private class IntakeProfilePayload
        {
            public string? LearningGoal { get; set; }
            public string? Subject { get; set; }
            public string? CurrentLevel { get; set; }
            public string? TargetOutcome { get; set; }
            public int WeeklyStudyHours { get; set; }
            public string? PreferredLanguage { get; set; }
            public string? LearningStyle { get; set; }
            public string? Constraints { get; set; }
            public List<string>? PreferredYouTubeChannels { get; set; }
            public List<string>? KnownStrengths { get; set; }
            public List<string>? KnownWeaknesses { get; set; }
            public List<string>? SuggestedResearchTopics { get; set; }
        }
    }
}
