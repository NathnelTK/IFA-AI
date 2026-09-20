using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Application.Learning.DTOs;
using IFA.Application.Learning.Services;

namespace IFA.Application.Conversations.Orchestrator
{
    public class IntakeConversationTurnResult
    {
        public string AssistantReply { get; set; } = string.Empty;
        public bool IsIntakeComplete { get; set; }
        public LearnerProfileDto? ResultingProfile { get; set; }
    }

    public interface IConversationOrchestrator
    {
        Task<IntakeConversationTurnResult> ProcessTurnAsync(
            string userMessage,
            List<string> history,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Model 1: Conversational Learning Advisor Orchestrator.
    /// Manages the contextual intake dialogue, asking focused questions to identify
    /// experience, time budget, and preferred YouTube mentors, then materializes the profile.
    /// </summary>
    public class ConversationOrchestrator : IConversationOrchestrator
    {
        private readonly ILearnerProfileBuilder _profileBuilder;
        private readonly IAiModelGateway _aiGateway;

        public ConversationOrchestrator(
            ILearnerProfileBuilder profileBuilder,
            IAiModelGateway aiGateway)
        {
            _profileBuilder = profileBuilder;
            _aiGateway = aiGateway;
        }

        public async Task<IntakeConversationTurnResult> ProcessTurnAsync(
            string userMessage,
            List<string> history,
            CancellationToken cancellationToken = default)
        {
            var combinedHistory = new List<string>(history) { userMessage };
            var fullContext = string.Join(" | ", combinedHistory).ToLower();

            // Check if we have both hours and topic/goal
            bool hasHours = fullContext.Contains("hour") || fullContext.Contains("hr");
            bool hasTopic = fullContext.Contains("c#") || fullContext.Contains(".net") ||
                            fullContext.Contains("python") || fullContext.Contains("learn") ||
                            fullContext.Contains("course") || fullContext.Contains("backend");

            if (!hasHours && history.Count == 0)
            {
                // First turn: prompt for hours and favorite mentors/creators
                return new IntakeConversationTurnResult
                {
                    AssistantReply = "That sounds like a great learning goal! To personalize your roadmap: how many hours per week can you dedicate to studying, and do you have any favorite YouTube creators (like Nick Chapsas or freeCodeCamp)?",
                    IsIntakeComplete = false,
                    ResultingProfile = null
                };
            }

            // If we have enough context or this is turn 2+, finalize intake
            var profile = _profileBuilder.BuildProfileFromConversation(userMessage, history);

            return new IntakeConversationTurnResult
            {
                AssistantReply = $"Perfect! I've structured your personalized learning plan for '{profile.Goal}'. Based on {profile.WeeklyHours} hours/week, I'll assemble your research package and construct Module 1 right away.",
                IsIntakeComplete = true,
                ResultingProfile = profile
            };
        }
    }
}
