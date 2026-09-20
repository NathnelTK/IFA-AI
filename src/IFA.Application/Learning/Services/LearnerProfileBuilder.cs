using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IFA.Application.Learning.DTOs;
using IFA.Domain.Entities;

namespace IFA.Application.Learning.Services
{
    public interface ILearnerProfileBuilder
    {
        LearnerProfileDto BuildProfileFromConversation(string userPrompt, IEnumerable<string>? conversationHistory = null);
        LearnerProfile MapToEntity(LearnerProfileDto dto, Guid learnerId);
    }

    /// <summary>
    /// Builder service responsible for parsing conversational intake into
    /// a structured, validated LearnerProfileDto and domain entity.
    /// </summary>
    public class LearnerProfileBuilder : ILearnerProfileBuilder
    {
        public LearnerProfileDto BuildProfileFromConversation(string userPrompt, IEnumerable<string>? conversationHistory = null)
        {
            var combinedText = userPrompt;
            if (conversationHistory != null)
            {
                combinedText += " " + string.Join(" ", conversationHistory);
            }

            var lower = combinedText.ToLower();

            // Extract target goal
            var goal = userPrompt.Trim();

            if (lower.Contains("c#") || lower.Contains(".net"))
            {
                goal = "Become a proficient C# and ASP.NET Core backend engineer";
            }
            else if (lower.Contains("python"))
            {
                goal = "Master Python data structures and data science fundamentals";
            }
            else if (lower.Contains("web") || lower.Contains("frontend"))
            {
                goal = "Build responsive full-stack web applications";
            }

            // Extract study hours
            var hours = 5;
            var hoursMatch = Regex.Match(combinedText, @"(\d+)\s*(?:hours?|hrs?)(?:\s*(?:per|\/)\s*week)?", RegexOptions.IgnoreCase);
            if (hoursMatch.Success && int.TryParse(hoursMatch.Groups[1].Value, out var parsedHours))
            {
                hours = Math.Clamp(parsedHours, 1, 40);
            }

            // Extract level
            var level = "Beginner";
            if (lower.Contains("advanced") || lower.Contains("senior"))
            {
                level = "Advanced";
            }
            else if (lower.Contains("intermediate") || lower.Contains("some experience") || lower.Contains("have done basic"))
            {
                level = "Intermediate";
            }

            // Extract preferred creators
            var preferredCreators = new List<string>();
            if (lower.Contains("nick chapsas") || lower.Contains("chapsas"))
            {
                preferredCreators.Add("Nick Chapsas");
            }
            if (lower.Contains("freecodecamp"))
            {
                preferredCreators.Add("freeCodeCamp.org");
            }
            if (lower.Contains("traversy"))
            {
                preferredCreators.Add("Traversy Media");
            }
            if (lower.Contains("amichai") || lower.Contains("mantinband"))
            {
                preferredCreators.Add("Amichai Mantinband");
            }

            return new LearnerProfileDto
            {
                Goal = goal,
                CurrentLevel = level,
                WeeklyHours = hours,
                TargetTimeline = hours >= 10 ? "3 weeks" : "6 weeks",
                PreferredLanguage = lower.Contains("amharic") || lower.Contains("am") ? "am" : "en",
                PreferredCreators = preferredCreators,
                LearningPreferences = new List<string> { "interactive code examples", "concise explanations", "quiz diagnostics" },
                KnownSkills = level == "Beginner" ? new List<string> { "Basic programming logic" } : new List<string> { "C# Syntax", "OOP Fundamentals" },
                SkillGaps = new List<string> { "REST APIs", "Clean Architecture", "JWT Authentication", "Entity Framework Core" },
                Constraints = new List<string> { $"{hours} hours/week commitment limit" }
            };
        }

        public LearnerProfile MapToEntity(LearnerProfileDto dto, Guid learnerId)
        {
            return new LearnerProfile
            {
                LearnerId = learnerId,
                Goal = dto.Goal,
                SubjectTopic = dto.Goal,
                CurrentLevel = dto.CurrentLevel,
                TargetOutcome = dto.TargetTimeline,
                AvailableStudyHoursPerWeek = dto.WeeklyHours,
                PreferredLanguage = dto.PreferredLanguage,
                PreferredLearningStyle = string.Join(", ", dto.LearningPreferences),
                PreferredYouTubeChannels = dto.PreferredCreators,
                KnownStrengths = dto.KnownSkills,
                KnownWeaknesses = dto.SkillGaps,
                Constraints = dto.Constraints
            };
        }
    }
}
