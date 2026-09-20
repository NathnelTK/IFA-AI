// IFA.API/Contracts/LearnerProfileContracts.cs
namespace IFA.API.Contracts
{
    public record CreateLearnerProfileRequest(
        string Goal,
        string? SubjectTopic,
        string? CurrentLevel,
        string? TargetOutcome,
        int? AvailableStudyHoursPerWeek,
        string? PreferredLanguage,
        string? PreferredLearningStyle,
        List<string>? Constraints,
        List<string>? PreferredYouTubeChannels,
        List<string>? KnownStrengths,
        List<string>? KnownWeaknesses
    );
}