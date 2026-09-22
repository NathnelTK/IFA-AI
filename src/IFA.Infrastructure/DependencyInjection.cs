using System;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.AI;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Services;
using IFA.Infrastructure.Video;
using IFA.Infrastructure.Voice;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IFA.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the persistence layer, AI model gateway, and external infrastructure
        /// service implementations used by the application layer.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "A PostgreSQL connection string is required to configure the IFA infrastructure layer.",
                    nameof(connectionString));
            }

            // Database & Persistence
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());

            // External Research & Media Services
            services.AddScoped<IScholarxivService, ScholarxivService>();
            services.AddScoped<IResearchService, ResearchService>();
            services.AddScoped<IYouTubeResourceService, YouTubeResourceService>();
            services.AddScoped<IVoxService, VoxideClient>();

            // AI Infrastructure (PR 2.1 & PR 3.5)
            services.AddHttpClient();
            services.AddSingleton(new AiGatewayOptions());
            services.AddTransient<ILlmProvider, GeminiClient>();
            services.AddTransient<ILlmProvider, GroqClient>();
            services.AddScoped<IAiModelGateway, FallbackAiService>();
            services.AddScoped<IFineTunedCourseBuilderClient, FineTunedCourseBuilderClient>();

            return services;
        }
    }
}
