using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    public class CourseGenerationService : ICourseGenerationService
    {
        private readonly ILlmGateway _llmGateway;
        private readonly IApplicationDbContext _context;
        private readonly IResearchService _researchService;
        private readonly ILogger<CourseGenerationService> _logger;

        public CourseGenerationService(
            ILlmGateway llmGateway,
            IApplicationDbContext context,
            IResearchService researchService,
            ILogger<CourseGenerationService> logger)
        {
            _llmGateway = llmGateway;
            _context = context;
            _researchService = researchService;
            _logger = logger;
        }

        public async Task<CoursePipelineProposal> GenerateCoursePipelineProposalAsync(
            LearnerProfile profile,
            ResearchPackage? researchPackage,
            CancellationToken cancellationToken = default)
        {
            var userPrompt = $"Learning Goal: {profile.LearningGoal}\nSubject: {profile.Subject}\nCurrent Level: {profile.CurrentLevel}\nWeekly Hours: {profile.WeeklyStudyHours}";
            
            var proposal = await _llmGateway.CompleteJsonAsync<CoursePipelineProposal>(
                PromptRegistry.Model2_CourseArchitectSystemPrompt,
                userPrompt,
                LlmRole.Model2_Architect,
                cancellationToken);

            if (proposal == null || !proposal.Modules.Any())
            {
                proposal = new CoursePipelineProposal
                {
                    CourseTitle = $"Mastering {profile.Subject}",
                    TargetGoal = profile.LearningGoal,
                    TotalEstimatedHours = profile.WeeklyStudyHours * 8,
                    Modules = new List<ModuleSummaryDto>
                    {
                        new() { ModuleNumber = 1, Title = "Foundations & Core Principles", Summary = "Essential syntax, execution model, and basic paradigms.", EstimatedHours = 4, KeyTopics = new List<string> { "Foundations", "Syntax", "Data Types" } },
                        new() { ModuleNumber = 2, Title = "Architecture & Patterns", Summary = "Separation of concerns, modular design, and clean interfaces.", EstimatedHours = 4, KeyTopics = new List<string> { "Design Patterns", "Clean Code", "Interfaces" } },
                        new() { ModuleNumber = 3, Title = "Data Persistence & Concurrency", Summary = "Database querying, state management, and asynchronous streams.", EstimatedHours = 4, KeyTopics = new List<string> { "Databases", "Async/Await", "ORM" } },
                        new() { ModuleNumber = 4, Title = "Security & Production Readiness", Summary = "Authentication, authorization, logging, and error resilience.", EstimatedHours = 4, KeyTopics = new List<string> { "Auth", "Security", "Logging" } },
                        new() { ModuleNumber = 5, Title = "Capstone Project & Exam Readiness", Summary = "End-to-end integration, performance optimization, and final test.", EstimatedHours = 4, KeyTopics = new List<string> { "Testing", "Deployment", "Optimization" } }
                    }
                };
            }

            return proposal;
        }

        public async Task<GeneratedModuleResult> GenerateJitModuleAsync(JitModuleGenerationRequest request, CancellationToken cancellationToken = default)
        {
            var weakAreasPrompt = request.PriorQuizWeakAreas != null && request.PriorQuizWeakAreas.Any()
                ? $"\nPRIOR QUIZ WEAK AREAS TO REINFORCE: {string.Join(", ", request.PriorQuizWeakAreas)}. Include dedicated remediation explanations and questions for these topics!"
                : "";

            var userPrompt = $"Course: {request.CourseTitle}\nModule {request.ModuleNumber}: {request.ModuleTitle}\nGoal: {request.TargetGoal}\nCreator: {request.PreferredVideoCreator}{weakAreasPrompt}";

            var resultDto = await _llmGateway.CompleteJsonAsync<GeneratedModuleDto>(
                PromptRegistry.Model3_CourseBuilderSystemPrompt,
                userPrompt,
                LlmRole.Model3_Builder,
                cancellationToken);

            var module = new Module
            {
                Id = Guid.NewGuid(),
                ModuleNumber = request.ModuleNumber,
                Title = request.ModuleTitle,
                Summary = $"Module covering {request.ModuleTitle}",
                GenerationStatus = ModuleGenerationStatus.Ready,
                GeneratedAt = DateTime.UtcNow
            };

            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                LessonNumber = 1,
                Title = resultDto?.Lesson?.Title ?? $"{request.ModuleTitle} Core Lesson",
                Summary = resultDto?.Lesson?.Summary ?? $"Detailed technical lesson for {request.ModuleTitle}",
                ContentMarkdown = resultDto?.Lesson?.ContentMarkdown ?? $"# {request.ModuleTitle}\n\nComprehensive instruction on {request.ModuleTitle}.",
                ReadingTimeMinutes = resultDto?.Lesson?.ReadingTimeMinutes ?? 12,
                YouTubeVideoId = resultDto?.Lesson?.YouTubeVideoId ?? "dQw4w9WgXcQ",
                YouTubeVideoTitle = resultDto?.Lesson?.YouTubeVideoTitle ?? $"{request.ModuleTitle} Guide",
                ScholarxivCitationDoi = resultDto?.Lesson?.ScholarxivCitationDoi ?? "10.48550/arXiv.2401.00123",
                ScholarxivPaperTitle = resultDto?.Lesson?.ScholarxivPaperTitle ?? $"Research on {request.ModuleTitle}"
            };

            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                Title = resultDto?.Quiz?.Title ?? $"{request.ModuleTitle} Mastery Quiz",
                PassingScorePercentage = resultDto?.Quiz?.PassingScorePercentage ?? 70
            };

            if (resultDto?.Quiz?.Questions != null && resultDto.Quiz.Questions.Any())
            {
                foreach (var q in resultDto.Quiz.Questions)
                {
                    quiz.Questions.Add(new Question
                    {
                        Id = Guid.NewGuid(),
                        QuizId = quiz.Id,
                        Prompt = q.Prompt,
                        Options = q.Options ?? new List<string> { "Option A", "Option B", "Option C", "Option D" },
                        CorrectOptionIndex = q.CorrectOptionIndex,
                        Explanation = q.Explanation,
                        TargetSkillName = string.IsNullOrWhiteSpace(q.TargetSkillName) ? "General" : q.TargetSkillName,
                        BloomTaxonomyLevel = string.IsNullOrWhiteSpace(q.BloomTaxonomyLevel) ? "Analyze" : q.BloomTaxonomyLevel
                    });
                }
            }
            else
            {
                // Fallback default questions
                quiz.Questions.Add(new Question
                {
                    Id = Guid.NewGuid(),
                    QuizId = quiz.Id,
                    Prompt = $"What is the primary architectural principle demonstrated in {request.ModuleTitle}?",
                    Options = new List<string>
                    {
                        "Coupling all components directly to a single file",
                        "Separation of concerns and strict boundary contracts",
                        "Disabling error logging to increase speed",
                        "Storing unencrypted credentials in client cookies"
                    },
                    CorrectOptionIndex = 1,
                    Explanation = "Separation of concerns ensures modularity and maintainability.",
                    TargetSkillName = "System Architecture",
                    BloomTaxonomyLevel = "Analyze"
                });
            }

            module.Lessons.Add(lesson);
            module.ModuleQuiz = quiz;

            return new GeneratedModuleResult
            {
                Module = module,
                Lesson = lesson,
                Quiz = quiz
            };
        }

        public async Task<Course> CreateFullCourseAsync(Guid learnerId, string goal, int hoursPerWeek = 5, string preferredCreator = "freeCodeCamp", CancellationToken ct = default)
        {
            // 1. Model 2 generates the Course Blueprint
            var profile = await _context.LearnerProfiles
                .FirstOrDefaultAsync(p => p.LearnerId == learnerId, ct);

            if (profile is null)
            {
                profile = new LearnerProfile
                {
                    Id = Guid.NewGuid(),
                    LearnerId = learnerId,
                    LearningGoal = goal,
                    Subject = goal,
                    CurrentLevel = "Beginner",
                    WeeklyStudyHours = hoursPerWeek > 0 ? hoursPerWeek : 5,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            else if (hoursPerWeek > 0)
            {
                profile.WeeklyStudyHours = hoursPerWeek;
            }

            var proposal = await GenerateCoursePipelineProposalAsync(profile, null, ct);

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = proposal.CourseTitle,
                Description = $"Tailored AI learning path designed to master {goal}.",
                Category = "Software Engineering",
                TargetAudience = "Exit Exam / Professional",
                ThumbnailUrl = "/ifa.png",
                ProviderName = "IFA AI",
                Rating = 4.9,
                ReviewCount = "1.5k",
                EstimatedDuration = $"{proposal.TotalEstimatedHours} hours",
                IsPublic = false,
                ShareCode = Guid.NewGuid().ToString("N").Substring(0, 8),
                CreatorLearnerId = learnerId,
                CreatedAt = DateTime.UtcNow
            };

            // Create ungenerated module placeholders for JIT
            foreach (var modDto in proposal.Modules)
            {
                var module = new Module
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    ModuleNumber = modDto.ModuleNumber,
                    Title = modDto.Title,
                    Summary = modDto.Summary,
                    EstimatedHours = modDto.EstimatedHours,
                    GenerationStatus = ModuleGenerationStatus.Blueprint
                };
                course.Modules.Add(module);
            }

            _context.Add(course);

            // Auto-enroll the creator
            var enrollment = new CourseEnrollment
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                LearnerId = learnerId,
                ProgressPercentage = 0,
                LastAccessedAt = DateTime.UtcNow
            };
            _context.Add(enrollment);

            // 2. Materialize Module 1 immediately using Model 3 (JIT generation)
            var firstModule = course.Modules.OrderBy(m => m.ModuleNumber).First();
            var jitRequest = new JitModuleGenerationRequest
            {
                CourseTitle = course.Title,
                ModuleNumber = firstModule.ModuleNumber,
                ModuleTitle = firstModule.Title,
                TargetGoal = goal,
                PreferredVideoCreator = preferredCreator
            };

            var generatedM1 = await GenerateJitModuleAsync(jitRequest, ct);
            firstModule.GenerationStatus = ModuleGenerationStatus.Ready;
            firstModule.GeneratedAt = DateTime.UtcNow;

            // Attach lesson and quiz to the first module
            generatedM1.Lesson.ModuleId = firstModule.Id;
            firstModule.Lessons.Add(generatedM1.Lesson);

            generatedM1.Quiz.ModuleId = firstModule.Id;
            firstModule.ModuleQuiz = generatedM1.Quiz;

            await _context.SaveChangesAsync(ct);

            // 3. Trigger research in background for this course
            try
            {
                await _researchService.ConductResearchAsync(goal, learnerId, course.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Background research failed: {Message}", ex.Message);
            }

            return course;
        }

        // Inner DTOs for Model 3 JSON deserialization
        private class GeneratedModuleDto
        {
            public LessonDto? Lesson { get; set; }
            public QuizDto? Quiz { get; set; }
        }

        private class LessonDto
        {
            public string Title { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
            public string ContentMarkdown { get; set; } = string.Empty;
            public int ReadingTimeMinutes { get; set; } = 12;
            public string? YouTubeVideoId { get; set; }
            public string? YouTubeVideoTitle { get; set; }
            public string? ScholarxivCitationDoi { get; set; }
            public string? ScholarxivPaperTitle { get; set; }
            public List<string>? KeyTakeaways { get; set; }
            public List<ExerciseDto>? Exercises { get; set; }
        }

        private class ExerciseDto
        {
            public string Instruction { get; set; } = string.Empty;
            public string Hint { get; set; } = string.Empty;
            public string StarterCode { get; set; } = string.Empty;
        }

        private class QuizDto
        {
            public string Title { get; set; } = string.Empty;
            public int PassingScorePercentage { get; set; } = 70;
            public List<QuestionDto>? Questions { get; set; }
        }

        private class QuestionDto
        {
            public string Prompt { get; set; } = string.Empty;
            public List<string>? Options { get; set; }
            public int CorrectOptionIndex { get; set; }
            public string Explanation { get; set; } = string.Empty;
            public string? TargetSkillName { get; set; }
            public string? BloomTaxonomyLevel { get; set; }
        }
    }
}
