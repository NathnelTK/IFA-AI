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
        /// 
        /// 
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "A PostgreSQL connection string is required to configure the IFA infrastructure layer.",
                    nameof(connectionString));
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<SkillProfileService>();
            services.AddScoped<CourseSharingService>();

            services.AddScoped<IResearchService, ResearchService>();

            services.AddScoped<IUnderstandingAgentService, UnderstandingAgentService>();
            // services.AddScoped<IScholarxivService, ScholarxivService>();
            var useMock = configuration.GetValue<bool>("Scholarxiv:UseMockData", true);

            if (useMock)
            {
                services.AddSingleton<IScholarxivService, MockScholarxivService>();
            }
            else
            {
                services.AddHttpClient<IScholarxivService, ScholarxivService>(client =>
                {
                    client.BaseAddress = new Uri("https://scholarxiv.com");
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var apiKey = configuration["SCHOLARXIV_API_KEY_IFA"];

                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        throw new InvalidOperationException(
                            "SCHOLARXIV_API_KEY_IFA not set.");
                    }
                    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                });
            }

            services.AddHttpClient<OllamaLlmProvider>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:11434");
                client.Timeout = TimeSpan.FromSeconds(120);

            });
            // services.AddScoped<OllamaLlmProvider>();
            services.AddScoped<ILlmGateway, LlmGateway>();
            services.AddScoped<ICourseArchitectService, CourseArchitectService>();
            services.AddScoped<ICourseOrchestrationService, CourseOrchestrationService>();


            return services;

        }

    }


}





