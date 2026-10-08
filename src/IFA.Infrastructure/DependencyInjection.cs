using IFA.Application.Common.Interfaces;
using IFA.Application.Courses.Services;
using IFA.Application.Skills.Services;
using IFA.Infrastructure.AI;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IFA.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the persistence layer and the infrastructure service
        /// implementations used by the application layer.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "A database connection string is required to configure the IFA infrastructure layer.",
                    nameof(connectionString));
            }

            // Database Configuration - PostgreSQL First, SQLite Fallback
            var dbProvider = Environment.GetEnvironmentVariable("DB_PROVIDER")?.ToLowerInvariant();
            bool useSqlite = dbProvider == "sqlite" 
                || connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) 
                || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase);

            if (useSqlite)
            {
                var sqliteConn = connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
                    ? connectionString
                    : "Data Source=ifa.db";

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(sqliteConn, sqlite =>
                        sqlite.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            }
            else
            {
                // Default to PostgreSQL for production
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            }

            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());

            // HTTP Client for external APIs
            services.AddHttpClient();

            // Domain Services
            services.AddScoped<SkillProfileService>();
            services.AddScoped<ICourseSharingService, CourseSharingService>();
            services.AddScoped<IResearchService, ResearchService>();
            services.AddScoped<IUnderstandingAgentService, UnderstandingAgentService>();

            // LLM Providers & Gateway
            services.AddScoped<GeminiLlmProvider>();
            services.AddScoped<GroqLlmProvider>();
            services.AddScoped<FallbackLlmProvider>();
            services.AddScoped<ILlmGateway, LlmGateway>();

            // External Research Services
            var useMockScholarxiv = configuration.GetValue<bool>("Scholarxiv:UseMockData");
            if (useMockScholarxiv)
            {
                services.AddSingleton<IScholarxivService, MockScholarxivService>();
            }
            else
            {
                services.AddHttpClient<IScholarxivService, ScholarxivService>(client =>
                {
                    client.BaseAddress = new Uri("https://scholarxiv.com");
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var apiKey = configuration["SCHOLARXIV_API_KEY"]
                        ?? configuration["SCHOLARXIV_API_KEY_IFA"];

                    if (!string.IsNullOrWhiteSpace(apiKey)
                        && apiKey != "change_me"
                        && !apiKey.StartsWith("your_", StringComparison.OrdinalIgnoreCase))
                    {
                        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                    }
                });
            }

            // YouTube Resource Service
            services.AddScoped<IYouTubeResourceService, YouTubeResourceService>();

            // Image/diagram research (Wikimedia, no API key required)
            services.AddHttpClient<IImageResourceService, WikimediaImageService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            // Ollama LLM Provider with HTTP Client
            services.AddHttpClient<OllamaLlmProvider>(client =>
            {
                var ollamaUrl = configuration["OLLAMA_BASE_URL"] ?? "http://localhost:11434";
                client.BaseAddress = new Uri(ollamaUrl);
                client.Timeout = TimeSpan.FromSeconds(120);
            });

            // Additional Services
            // Registered as both the interface and the concrete type: several
            // controllers (Courses, Modules) depend on the concrete
            // CourseGenerationService for JIT module generation.
            services.AddScoped<CourseGenerationService>();
            services.AddScoped<ICourseGenerationService>(provider =>
                provider.GetRequiredService<CourseGenerationService>());
            services.AddScoped<IIntakeService, IntakeService>();
            services.AddScoped<IAiTutorService, AiTutorService>();
            services.AddScoped<IAdaptiveEngine, AdaptiveEngine>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IVoxService, VoxService>();

            return services;
        }
    }
}


