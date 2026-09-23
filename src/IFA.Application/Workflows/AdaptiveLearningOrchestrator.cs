using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Courses.Commands;
using IFA.Application.Courses.DTOs;
using IFA.Application.Courses.Services;
using IFA.Application.Progress.Services;
using IFA.Application.Research.DTOs;
using IFA.Domain.Entities;

namespace IFA.Application.Workflows
{
    public class CompleteModuleAndAdaptNextResult
    {
        public AssessmentResultDto Assessment { get; set; } = new();
        public ModuleSpecificationDto? NextModuleSpecification { get; set; }
        public List<string> AdaptationNotes { get; set; } = new();
    }

    public interface IAdaptiveLearningOrchestrator
    {
        Task<CompleteModuleAndAdaptNextResult> ProcessModuleAssessmentAndAdaptAsync(
            Quiz quiz,
            QuizSubmissionDto submission,
            CourseBlueprintDto blueprint,
            ResearchPackageDto research,
            int completedModuleNumber,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Adaptive Learning Engine: Workflow Orchestrator.
    /// Closes the loop between learner assessment results and next module JIT materialization.
    /// Updates skill gaps, derives adaptation constraints, and triggers Model 2.
    /// </summary>
    public class AdaptiveLearningOrchestrator : IAdaptiveLearningOrchestrator
    {
        private readonly ISkillAssessmentService _assessmentService;
        private readonly INextModuleAdaptationService _adaptationService;
        private readonly MaterializeNextModuleCommandHandler _nextModuleHandler;

        public AdaptiveLearningOrchestrator(
            ISkillAssessmentService assessmentService,
            INextModuleAdaptationService adaptationService,
            MaterializeNextModuleCommandHandler nextModuleHandler)
        {
            _assessmentService = assessmentService;
            _adaptationService = adaptationService;
            _nextModuleHandler = nextModuleHandler;
        }

        public async Task<CompleteModuleAndAdaptNextResult> ProcessModuleAssessmentAndAdaptAsync(
            Quiz quiz,
            QuizSubmissionDto submission,
            CourseBlueprintDto blueprint,
            ResearchPackageDto research,
            int completedModuleNumber,
            CancellationToken cancellationToken = default)
        {
            // 1. Evaluate quiz performance and update skill metrics
            var assessment = await _assessmentService.EvaluateQuizSubmissionAsync(quiz, submission, cancellationToken);

            // 2. Derive adaptation constraints for the next module
            var nextModuleNumber = completedModuleNumber + 1;
            var constraints = await _adaptationService.GenerateAdaptationConstraintsAsync(assessment, nextModuleNumber, cancellationToken);

            // 3. Materialize next module specification with adaptation constraints injected
            var nextModuleCommand = new MaterializeNextModuleCommand
            {
                CourseId = blueprint.CourseId ?? Guid.NewGuid(),
                NextModuleNumber = nextModuleNumber,
                Blueprint = blueprint,
                ResearchPackage = research,
                IdentifiedSkillGaps = assessment.NewlyIdentifiedWeakAreas
            };

            var nextModuleSpec = await _nextModuleHandler.HandleAsync(nextModuleCommand, cancellationToken);
            nextModuleSpec.AdaptationConstraints = constraints;

            return new CompleteModuleAndAdaptNextResult
            {
                Assessment = assessment,
                NextModuleSpecification = nextModuleSpec,
                AdaptationNotes = constraints
            };
        }
    }
}
