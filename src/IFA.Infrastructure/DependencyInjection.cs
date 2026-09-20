using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IFA.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the persistence layer and the infrastructure service
        /// implementations used by the application layer.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
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

            services.AddScoped<IScholarxivService, ScholarxivService>();
            services.AddScoped<IResearchService, ResearchService>();
            

            return services;
        }
        
    }
}
