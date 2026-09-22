using System.Collections.Generic;

namespace IFA.Application.Learning.DTOs
{
    /// <summary>
    /// Contract produced by Model 1 (Learning Advisor) representing
    /// structured learner intake, preferences, and baseline competencies.
    /// </summary>
    public class LearnerProfileDto
    {
        public string Goal { get; set; } = string.Empty;
        public string CurrentLevel { get; set; } = "Beginner";
        public List<string> KnownSkills { get; set; } = new();
        public List<string> SkillGaps { get; set; } = new();
        public int WeeklyHours { get; set; } = 5;
        public string TargetTimeline { get; set; } = "4 weeks";
        public string PreferredLanguage { get; set; } = "en";
        public List<string> LearningPreferences { get; set; } = new();
        public List<string> PreferredCreators { get; set; } = new();
        public List<string> Constraints { get; set; } = new();
    }
}
