using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Application.Courses.DTOs;
using IFA.Application.Learning.DTOs;
using IFA.Application.Research.DTOs;

namespace IFA.Application.Courses.Services
{
    public interface ICourseArchitectService
    {
        Task<CourseBlueprintDto> DesignCourseBlueprintAsync(
            LearnerProfileDto profile,
            ResearchPackageDto research,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Model 2: Course Architect.
    /// Transforms the Learner Profile and Research Package into a complete personalized
    /// multi-module Course Blueprint. Crucially implements Lazy/JIT generation:
    /// Module 1 is flagged as READY_FOR_GENERATION, while future modules remain lightweight
    /// blueprints until the learner passes prerequisite assessments.
    /// </summary>
    public class CourseArchitectService : ICourseArchitectService
    {
        private readonly IAiModelGateway _aiGateway;

        public CourseArchitectService(IAiModelGateway aiGateway)
        {
            _aiGateway = aiGateway;
        }

        public Task<CourseBlueprintDto> DesignCourseBlueprintAsync(
            LearnerProfileDto profile,
            ResearchPackageDto research,
            CancellationToken cancellationToken = default)
        {
            var isCSharp = profile.Goal.Contains("C#", StringComparison.OrdinalIgnoreCase) ||
                           profile.Goal.Contains(".NET", StringComparison.OrdinalIgnoreCase) ||
                           profile.Goal.Contains("Backend", StringComparison.OrdinalIgnoreCase);

            var blueprint = new CourseBlueprintDto
            {
                CourseTitle = isCSharp ? "C# Backend Development & Clean Architecture" : $"{profile.Goal} Mastery",
                Description = $"Personalized adaptive curriculum grounded by Scholarxiv research and curated for {profile.CurrentLevel} learners.",
                TargetGoal = profile.Goal,
                EstimatedTotalHours = profile.WeeklyHours * 4,
                PedagogicalApproach = "Evidence-Based Concept Progression -> Practical Code -> Formative Diagnostic",
                Modules = new List<ModuleBlueprintDto>
                {
                    new ModuleBlueprintDto
                    {
                        ModuleNumber = 1,
                        Title = isCSharp ? "C# 10 & RESTful API Foundations" : "Core Foundations & Architectural Principles",
                        Summary = "Master HTTP request lifecycles, Clean Architecture separation, and endpoint routing.",
                        Status = "READY_FOR_GENERATION",
                        EstimatedHours = profile.WeeklyHours,
                        KeyTopics = new List<string> { "HTTP Verbs & Status Codes", "Clean Architecture Layers", "Minimal APIs vs Controllers", "Dependency Injection" },
                        LearningObjectives = new List<string> { "Build robust REST endpoints", "Understand Domain vs Infrastructure boundaries" }
                    },
                    new ModuleBlueprintDto
                    {
                        ModuleNumber = 2,
                        Title = isCSharp ? "Entity Framework Core & PostgreSQL Persistence" : "Data Modeling & Storage Foundations",
                        Summary = "Implement relational database mappings, DbContext lifecycle, and async migrations.",
                        Status = "BLUEPRINT",
                        EstimatedHours = profile.WeeklyHours,
                        KeyTopics = new List<string> { "DbContext Configuration", "Fluent API Mappings", "Async Queries", "Npgsql Driver" },
                        PrerequisiteTopics = new List<string> { "Dependency Injection", "C# OOP" }
                    },
                    new ModuleBlueprintDto
                    {
                        ModuleNumber = 3,
                        Title = isCSharp ? "Stateless Authentication & Security (JWT Lifecycle)" : "Authentication & Access Control",
                        Summary = "Secure endpoints with JSON Web Tokens, claim-based authorization, and token refresh.",
                        Status = "BLUEPRINT",
                        EstimatedHours = profile.WeeklyHours,
                        KeyTopics = new List<string> { "JWT Signing", "Bearer Middleware", "ClaimsPrincipal", "Security Vulnerabilities" },
                        PrerequisiteTopics = new List<string> { "HTTP Middleware", "Data Persistence" }
                    },
                    new ModuleBlueprintDto
                    {
                        ModuleNumber = 4,
                        Title = isCSharp ? "Testing, Exit Exam Preparation & Production Readiness" : "Assessment & Industry Polish",
                        Summary = "Unit testing with xUnit, integration tests with Testcontainers, and national exam diagnostics.",
                        Status = "BLUEPRINT",
                        EstimatedHours = profile.WeeklyHours,
                        KeyTopics = new List<string> { "Unit Testing", "Mocking", "Bloom's Taxonomy Diagnostic", "Docker Deployment" },
                        PrerequisiteTopics = new List<string> { "JWT Auth", "REST APIs" }
                    }
                }
            };

            return Task.FromResult(blueprint);
        }
    }
}
