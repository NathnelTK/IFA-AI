using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Courses.DTOs;
using IFA.Application.Learning.DTOs;
using IFA.Application.Research.DTOs;

namespace IFA.Application.Courses.Commands
{
    public class MaterializeNextModuleCommand
    {
        public Guid CourseId { get; set; }
        public int NextModuleNumber { get; set; } = 2;
        public CourseBlueprintDto Blueprint { get; set; } = new();
        public ResearchPackageDto ResearchPackage { get; set; } = new();
        public List<string> IdentifiedSkillGaps { get; set; } = new();
    }

    public class MaterializeNextModuleCommandHandler
    {
        public Task<ModuleSpecificationDto> HandleAsync(
            MaterializeNextModuleCommand command,
            CancellationToken cancellationToken = default)
        {
            var nextBlueprint = command.Blueprint.Modules
                .FirstOrDefault(m => m.ModuleNumber == command.NextModuleNumber)
                ?? new ModuleBlueprintDto
                {
                    ModuleNumber = command.NextModuleNumber,
                    Title = $"Module {command.NextModuleNumber}: Advanced Exploration",
                    Summary = "Continuing your personalized journey."
                };

            var adaptationNotes = command.IdentifiedSkillGaps.Count > 0
                ? command.IdentifiedSkillGaps.Select(gap => $"Reinforcing detected weak area: {gap} with targeted code examples.").ToList()
                : new List<string> { "Standard progression pace maintained." };

            var spec = new ModuleSpecificationDto
            {
                CourseId = command.CourseId,
                ModuleNumber = command.NextModuleNumber,
                Title = nextBlueprint.Title,
                Topic = nextBlueprint.Title,
                TargetAudience = command.Blueprint.TargetGoal,
                LearnerProfile = command.ResearchPackage.LearnerContext,
                LearningObjectives = nextBlueprint.LearningObjectives.Count > 0
                    ? nextBlueprint.LearningObjectives
                    : new List<string> { $"Master concepts in {nextBlueprint.Title}", "Apply adaptive remediation" },
                ResearchFindings = command.ResearchPackage.ResearchFindings,
                Resources = command.ResearchPackage.ExternalResources,
                PreferredVideos = command.ResearchPackage.VideoResources,
                AdaptationConstraints = adaptationNotes,
                Lessons = new List<LessonSpecificationDto>
                {
                    new LessonSpecificationDto
                    {
                        LessonNumber = 1,
                        Title = $"{nextBlueprint.Title} - Core Concepts",
                        Summary = $"Adaptive lesson designed to address prerequisites and {string.Join(", ", command.IdentifiedSkillGaps)}.",
                        ReadingTimeMinutes = 12,
                        PracticalExercises = new List<string>
                        {
                            $"Implement a practice exercise focusing on {nextBlueprint.Title}",
                            "Review past quiz mistakes and verify error boundaries"
                        },
                        KeyTakeaways = new List<string>
                        {
                            $"Key rule: Ensure proper persistence and error propagation",
                            "Continuous testing prevents regression in core modules"
                        }
                    }
                },
                FormativeQuiz = new QuizSpecificationDto
                {
                    Title = $"{nextBlueprint.Title} Checkpoint Quiz",
                    PassingScorePercentage = 75,
                    Questions = new List<QuestionSpecificationDto>
                    {
                        new QuestionSpecificationDto
                        {
                            Prompt = $"What is the best practice when handling database queries in {nextBlueprint.Title}?",
                            Options = new List<string> { "Use asynchronous queries (async/await)", "Use synchronous blocking calls", "Disable indexes", "Never use transactions" },
                            CorrectOptionIndex = 0,
                            Explanation = "Asynchronous queries free up the ASP.NET Core thread pool threads to process other incoming HTTP requests, drastically boosting scalability.",
                            AssociatedSkill = "Databases"
                        }
                    }
                }
            };

            return Task.FromResult(spec);
        }
    }
}
