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
    public class MaterializeInitialModuleCommand
    {
        public Guid? CourseId { get; set; }
        public CourseBlueprintDto Blueprint { get; set; } = new();
        public ResearchPackageDto ResearchPackage { get; set; } = new();
    }

    public class MaterializeInitialModuleCommandHandler
    {
        public Task<ModuleSpecificationDto> HandleAsync(
            MaterializeInitialModuleCommand command,
            CancellationToken cancellationToken = default)
        {
            var firstModuleBlueprint = command.Blueprint.Modules.FirstOrDefault(m => m.ModuleNumber == 1)
                ?? new ModuleBlueprintDto
                {
                    ModuleNumber = 1,
                    Title = "Core Foundations & Architectural Principles",
                    Summary = "Introduction to core concepts and architecture."
                };

            var topVideo = command.ResearchPackage.VideoResources.FirstOrDefault();
            var topPaper = command.ResearchPackage.AcademicSources.FirstOrDefault();

            var spec = new ModuleSpecificationDto
            {
                CourseId = command.CourseId,
                ModuleNumber = 1,
                Title = firstModuleBlueprint.Title,
                Topic = firstModuleBlueprint.Title,
                TargetAudience = command.Blueprint.TargetGoal,
                LearnerProfile = command.ResearchPackage.LearnerContext,
                LearningObjectives = firstModuleBlueprint.LearningObjectives.Count > 0
                    ? firstModuleBlueprint.LearningObjectives
                    : new List<string> { "Understand core concepts", "Implement first practical sample" },
                ResearchFindings = command.ResearchPackage.ResearchFindings,
                Resources = command.ResearchPackage.ExternalResources,
                PreferredVideos = command.ResearchPackage.VideoResources,
                Lessons = new List<LessonSpecificationDto>
                {
                    new LessonSpecificationDto
                    {
                        LessonNumber = 1,
                        Title = "HTTP Request Lifecycle & API Endpoints",
                        Summary = "Understand how requests travel through middleware into endpoint handlers.",
                        ReadingTimeMinutes = 10,
                        VideoResource = topVideo,
                        AcademicCitation = topPaper,
                        PracticalExercises = new List<string>
                        {
                            "Create a Minimal API endpoint returning a JSON response",
                            "Verify status code 200 vs 404 behavior using curl or Postman"
                        },
                        KeyTakeaways = new List<string>
                        {
                            "REST APIs rely on standard HTTP verbs (GET, POST, PUT, DELETE)",
                            "Separation of concerns keeps endpoints lightweight and testable"
                        }
                    },
                    new LessonSpecificationDto
                    {
                        LessonNumber = 2,
                        Title = "Clean Architecture Layers: Domain, Application & Infrastructure",
                        Summary = "Step-by-step breakdown of Onion/Clean architecture boundaries.",
                        ReadingTimeMinutes = 12,
                        PracticalExercises = new List<string>
                        {
                            "Organize project folders into Domain, Application, Infrastructure, and API",
                            "Define a domain entity with private setters and constructor invariants"
                        },
                        KeyTakeaways = new List<string>
                        {
                            "Domain layer must have zero dependencies on external libraries or databases",
                            "Infrastructure implements application abstractions via Dependency Injection"
                        }
                    }
                },
                FormativeQuiz = new QuizSpecificationDto
                {
                    Title = "Module 1 Diagnostic: REST & Clean Architecture",
                    PassingScorePercentage = 70,
                    Questions = new List<QuestionSpecificationDto>
                    {
                        new QuestionSpecificationDto
                        {
                            Prompt = "Which architectural layer in Clean Architecture should have NO dependencies on external frameworks or databases?",
                            Options = new List<string> { "Domain Layer", "Infrastructure Layer", "API / Presentation Layer", "Application Layer" },
                            CorrectOptionIndex = 0,
                            Explanation = "The Domain layer is the enterprise core and contains pure business rules and entities without any external dependencies.",
                            AssociatedSkill = "Clean Architecture"
                        },
                        new QuestionSpecificationDto
                        {
                            Prompt = "Which HTTP status code should be returned when a resource is successfully created via a POST request?",
                            Options = new List<string> { "200 OK", "201 Created", "204 No Content", "202 Accepted" },
                            CorrectOptionIndex = 1,
                            Explanation = "HTTP 201 Created signifies that the request succeeded and resulted in the creation of a new resource, typically accompanied by a Location header.",
                            AssociatedSkill = "REST APIs"
                        },
                        new QuestionSpecificationDto
                        {
                            Prompt = "What is the primary benefit of using Dependency Injection in ASP.NET Core?",
                            Options = new List<string> { "Increases compilation speed", "Decouples components and enables testability with mocks", "Replaces database tables", "Enables multithreading automatically" },
                            CorrectOptionIndex = 1,
                            Explanation = "Dependency Injection inverts control, allowing dependencies to be swapped for test doubles and decoupling high-level policy from low-level detail.",
                            AssociatedSkill = "Dependency Injection"
                        }
                    }
                }
            };

            return Task.FromResult(spec);
        }
    }
}
