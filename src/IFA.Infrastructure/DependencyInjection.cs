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
            services.AddSingleton(BuildAiGatewayOptions());
            services.AddTransient<ILlmProvider, GeminiClient>();
            services.AddTransient<ILlmProvider, GroqClient>();
            services.AddScoped<IAiModelGateway, FallbackAiService>();
            services.AddScoped<IFineTunedCourseBuilderClient, FineTunedCourseBuilderClient>();

            return services;
        }

        /// <summary>
        /// Builds the AI gateway options from environment variables (populated by
        /// <c>EnvFile.Load()</c> / the container runtime). A provider is only enabled
        /// when its API key is present, so <see cref="FallbackAiService"/> cleanly
        /// skips any provider that has not been configured with credentials.
        /// </summary>
        private static AiGatewayOptions BuildAiGatewayOptions()
        {
            var options = new AiGatewayOptions
            {
                Gemini = BuildProviderOptions(
                    prefix: "GEMINI",
                    defaultModel: "gemini-2.0-flash",
                    defaultBaseUrl: "https://generativelanguage.googleapis.com/v1beta"),
                Groq = BuildProviderOptions(
                    prefix: "GROQ",
                    defaultModel: "llama-3.3-70b-versatile",
                    defaultBaseUrl: "https://api.groq.com/openai/v1")
            };

            var fallbackOrder = Environment.GetEnvironmentVariable("AI_PROVIDER_FALLBACK_ORDER");
            if (!string.IsNullOrWhiteSpace(fallbackOrder))
            {
                options.ProviderFallbackOrder = fallbackOrder
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            return options;
        }

        private static ProviderOptions BuildProviderOptions(string prefix, string defaultModel, string defaultBaseUrl)
        {
            var apiKey = Environment.GetEnvironmentVariable($"{prefix}_API_KEY") ?? string.Empty;

            var enabled = !string.IsNullOrWhiteSpace(apiKey);
            if (bool.TryParse(Environment.GetEnvironmentVariable($"{prefix}_ENABLED"), out var explicitToggle))
            {
                // An explicit toggle can only disable a provider, never enable one
                // that has no credentials configured.
                enabled = enabled && explicitToggle;
            }

            var options = new ProviderOptions
            {
                Enabled = enabled,
                ApiKey = apiKey,
                Model = Coalesce(Environment.GetEnvironmentVariable($"{prefix}_MODEL"), defaultModel),
                BaseUrl = Coalesce(Environment.GetEnvironmentVariable($"{prefix}_BASE_URL"), defaultBaseUrl)
            };

            if (int.TryParse(Environment.GetEnvironmentVariable($"{prefix}_TIMEOUT_SECONDS"), out var timeout) && timeout > 0)
            {
                options.TimeoutSeconds = timeout;
            }

            return options;
        }

        private static string Coalesce(string? value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
