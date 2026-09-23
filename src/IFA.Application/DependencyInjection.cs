using IFA.Application.Conversations.Orchestrator;
using IFA.Application.Courses.Commands;
using IFA.Application.Courses.Services;
using IFA.Application.Learning.Services;
using IFA.Application.Progress.Services;
using IFA.Application.Research.Queries;
using IFA.Application.Research.Services;
using IFA.Application.Voice.Commands;
using IFA.Application.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace IFA.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Model 1: Conversational Learning Advisor & Profiling
            services.AddScoped<ILearnerProfileBuilder, LearnerProfileBuilder>();
            services.AddScoped<IConversationOrchestrator, ConversationOrchestrator>();

            // Model 1: Research Orchestration & Grounding
            services.AddScoped<IExternalResourceResearchService, ExternalResourceResearchService>();
            services.AddScoped<IUnifiedResearchService, UnifiedResearchService>();
            services.AddScoped<IResearchOrchestrator, ResearchOrchestrator>();
            services.AddScoped<SearchScholarxivQueryHandler>();

            // Model 2: Course Architect & JIT Materialization
            services.AddScoped<ICourseArchitectService, CourseArchitectService>();
            services.AddScoped<MaterializeInitialModuleCommandHandler>();
            services.AddScoped<MaterializeNextModuleCommandHandler>();

            // Model 3: Fine-Tuned Course Builder Execution
            services.AddScoped<BuildCurrentModuleCommandHandler>();

            // Adaptive Learning Engine (PR 3.6)
            services.AddScoped<ISkillAssessmentService, SkillAssessmentService>();
            services.AddScoped<INextModuleAdaptationService, NextModuleAdaptationService>();
            services.AddScoped<IAdaptiveLearningOrchestrator, AdaptiveLearningOrchestrator>();

            // Voice Interaction Layer (PR 2.5)
            services.AddScoped<ParseVoiceIntentCommandHandler>();

            return services;
        }
    }
}
