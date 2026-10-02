using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public class IntakeMessageDto
    {
        public string Role { get; set; } = "user"; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class IntakeResponse
    {
        public string Reply { get; set; } = string.Empty;
        public bool IsProfileReady { get; set; }
        public LearnerProfileDto? Profile { get; set; }
        public List<string> SuggestedResearchTopics { get; set; } = new List<string>();
        public List<string> FollowUpQuestions { get; set; } = new List<string>();
    }

    public class LearnerProfileDto
    {
        public string LearningGoal { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string CurrentLevel { get; set; } = "Beginner";
        public string TargetOutcome { get; set; } = string.Empty;
        public int WeeklyStudyHours { get; set; } = 5;
        public string PreferredLanguage { get; set; } = "en";
        public string LearningStyle { get; set; } = "Hands-on";
        public string Constraints { get; set; } = string.Empty;
        public List<string> PreferredYouTubeChannels { get; set; } = new List<string>();
        public List<string> KnownStrengths { get; set; } = new List<string>();
        public List<string> KnownWeaknesses { get; set; } = new List<string>();
    }

    public interface IIntakeService
    {
        Task<IntakeResponse> ProcessMessageAsync(Guid learnerId, string userMessage, List<IntakeMessageDto> conversationHistory, CancellationToken ct = default);
    }
}
