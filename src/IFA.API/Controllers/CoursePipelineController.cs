using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Conversations.Orchestrator;
using IFA.Application.Courses.Commands;
using IFA.Application.Courses.DTOs;
using IFA.Application.Courses.Services;
using IFA.Application.Learning.DTOs;
using IFA.Application.Progress.Services;
using IFA.Application.Research.DTOs;
using IFA.Application.Research.Services;
using IFA.Application.Workflows;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursePipelineController : ControllerBase
    {
        private readonly IConversationOrchestrator _conversationOrchestrator;
        private readonly IResearchOrchestrator _researchOrchestrator;
        private readonly ICourseArchitectService _courseArchitectService;
        private readonly MaterializeInitialModuleCommandHandler _materializeInitialHandler;
        private readonly BuildCurrentModuleCommandHandler _buildModuleHandler;
        private readonly IAdaptiveLearningOrchestrator _adaptiveOrchestrator;

        public CoursePipelineController(
            IConversationOrchestrator conversationOrchestrator,
            IResearchOrchestrator researchOrchestrator,
            ICourseArchitectService courseArchitectService,
            MaterializeInitialModuleCommandHandler materializeInitialHandler,
            BuildCurrentModuleCommandHandler buildModuleHandler,
            IAdaptiveLearningOrchestrator adaptiveOrchestrator)
        {
            _conversationOrchestrator = conversationOrchestrator;
            _researchOrchestrator = researchOrchestrator;
            _courseArchitectService = courseArchitectService;
            _materializeInitialHandler = materializeInitialHandler;
            _buildModuleHandler = buildModuleHandler;
            _adaptiveOrchestrator = adaptiveOrchestrator;
        }

        /// <summary>
        /// Model 1: Conversational intake turn.
        /// </summary>
        [HttpPost("intake")]
        public async Task<IActionResult> IntakeTurn(
            [FromBody] IntakeTurnRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _conversationOrchestrator.ProcessTurnAsync(
                request.UserMessage,
                request.History ?? new List<string>(),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Model 2: Generate course blueprint from Learner Profile and Research Package.
        /// Keeps future modules as blueprints (Lazy / JIT generation).
        /// </summary>
        [HttpPost("blueprint")]
        public async Task<IActionResult> GenerateBlueprint(
            [FromBody] GenerateBlueprintRequest request,
            CancellationToken cancellationToken = default)
        {
            var blueprint = await _courseArchitectService.DesignCourseBlueprintAsync(
                request.Profile,
                request.ResearchPackage,
                cancellationToken);

            return Ok(blueprint);
        }

        /// <summary>
        /// JIT Materialization: Materialize Module 1 specification for Model 3.
        /// </summary>
        [HttpPost("materialize-initial")]
        public async Task<IActionResult> MaterializeInitialModule(
            [FromBody] MaterializeInitialModuleCommand command,
            CancellationToken cancellationToken = default)
        {
            var spec = await _materializeInitialHandler.HandleAsync(command, cancellationToken);
            return Ok(spec);
        }

        /// <summary>
        /// Model 3: Fine-Tuned Course Builder turns Module Specification into
        /// learner-ready lessons, videos, exercises, and quiz.
        /// </summary>
        [HttpPost("build-module")]
        public async Task<IActionResult> BuildModule(
            [FromBody] BuildCurrentModuleCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _buildModuleHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Adaptive Learning Engine: Process quiz submission, recalibrate skills,
        /// and adaptively prepare the next module blueprint.
        /// </summary>
        [HttpPost("assess-and-adapt")]
        public async Task<IActionResult> AssessAndAdapt(
            [FromBody] AssessAndAdaptRequest request,
            CancellationToken cancellationToken = default)
        {
            var quiz = new Quiz
            {
                Id = request.Submission.QuizId,
                Title = "Diagnostic Quiz",
                PassingScorePercentage = 70
            };

            foreach (var q in request.Questions)
            {
                quiz.Questions.Add(new Question
                {
                    Id = q.Id,
                    Prompt = q.Prompt,
                    CorrectOptionIndex = q.CorrectOptionIndex,
                    TargetSkillName = q.AssociatedSkill
                });
            }

            var result = await _adaptiveOrchestrator.ProcessModuleAssessmentAndAdaptAsync(
                quiz,
                request.Submission,
                request.Blueprint,
                request.ResearchPackage,
                request.CompletedModuleNumber,
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// One-click end-to-end pipeline execution for rapid demo & testing.
        /// </summary>
        [HttpPost("quick-start")]
        public async Task<IActionResult> QuickStartCourse(
            [FromBody] QuickStartRequest request,
            CancellationToken cancellationToken = default)
        {
            // 1. Model 1 Intake
            var intake = await _conversationOrchestrator.ProcessTurnAsync(
                request.Prompt,
                new List<string>(),
                cancellationToken);

            var profile = intake.ResultingProfile ?? new LearnerProfileDto
            {
                Goal = request.Prompt,
                CurrentLevel = "Beginner"
            };

            // 2. Model 1 Research Orchestration
            var research = await _researchOrchestrator.OrchestrateResearchAsync(profile, cancellationToken);

            // 3. Model 2 Course Architect Blueprint
            var blueprint = await _courseArchitectService.DesignCourseBlueprintAsync(profile, research, cancellationToken);

            // 4. JIT Materialization for Module 1
            var module1Spec = await _materializeInitialHandler.HandleAsync(new MaterializeInitialModuleCommand
            {
                Blueprint = blueprint,
                ResearchPackage = research
            }, cancellationToken);

            // 5. Model 3 Course Builder
            var generatedModule = await _buildModuleHandler.HandleAsync(new BuildCurrentModuleCommand
            {
                Specification = module1Spec
            }, cancellationToken);

            return Ok(new
            {
                profile,
                researchPackage = research,
                blueprint,
                module1 = generatedModule
            });
        }
    }

    public class IntakeTurnRequest
    {
        public string UserMessage { get; set; } = string.Empty;
        public List<string>? History { get; set; }
    }

    public class GenerateBlueprintRequest
    {
        public LearnerProfileDto Profile { get; set; } = new();
        public ResearchPackageDto ResearchPackage { get; set; } = new();
    }

    public class AssessAndAdaptRequest
    {
        public QuizSubmissionDto Submission { get; set; } = new();
        public List<QuizQuestionInputDto> Questions { get; set; } = new();
        public CourseBlueprintDto Blueprint { get; set; } = new();
        public ResearchPackageDto ResearchPackage { get; set; } = new();
        public int CompletedModuleNumber { get; set; } = 1;
    }

    public class QuizQuestionInputDto
    {
        public Guid Id { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public int CorrectOptionIndex { get; set; }
        public string AssociatedSkill { get; set; } = string.Empty;
    }

    public class QuickStartRequest
    {
        public string Prompt { get; set; } = "I want to learn C# for backend development";
    }
}
