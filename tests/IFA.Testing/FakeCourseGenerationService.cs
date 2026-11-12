using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;

namespace IFA.Testing;

public sealed class FakeCourseGenerationService : ICourseGenerationService
{
    public Func<JitModuleGenerationRequest, CancellationToken, Task<GeneratedModuleResult>> ModuleHandler { get; set; } =
        (request, _) => Task.FromResult(new GeneratedModuleResult
        {
            Module = new Module { ModuleNumber = request.ModuleNumber, Title = request.ModuleTitle },
            Lessons =
            [
                new Lesson
                {
                    Title = $"{request.ModuleTitle} lesson",
                    ContentMarkdown = "Deterministic test content",
                    LessonNumber = 1
                }
            ]
        });

    public List<JitModuleGenerationRequest> ModuleRequests { get; } = [];

    public Task<CoursePipelineProposal> GenerateCoursePipelineProposalAsync(
        LearnerProfile profile,
        ResearchPackage? researchPackage,
        CancellationToken cancellationToken = default,
        IReadOnlyList<string>? externalMaterials = null) =>
        Task.FromResult(new CoursePipelineProposal());

    public Task<CoursePipelineProposal> GenerateCoursePipelineProposalAsync(
        CourseGenerationPrompt prompt,
        ResearchPackage? researchPackage,
        CancellationToken cancellationToken = default,
        IReadOnlyList<string>? externalMaterials = null) =>
        Task.FromResult(new CoursePipelineProposal
        {
            CourseTitle = prompt.Goal,
            TargetGoal = prompt.Goal,
            Category = prompt.Category,
            TargetAudience = prompt.TargetAudience,
            TotalEstimatedHours = Math.Max(1, prompt.WeeklyStudyHours)
        });

    public Task<GeneratedModuleResult> GenerateJitModuleAsync(
        JitModuleGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ModuleRequests.Add(request);
        return ModuleHandler(request, cancellationToken);
    }
}
