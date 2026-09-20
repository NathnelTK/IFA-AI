using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Research.DTOs;

namespace IFA.Application.Research.Services
{
    public interface IExternalResourceResearchService
    {
        Task<List<ExternalResourceDto>> SearchDocumentationAndTutorialsAsync(
            string topic,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service responsible for fetching verified external documentation, official guides,
    /// and standard specifications (e.g. Microsoft Learn, MDN, RFCs).
    ///
    /// =========================================================================
    /// ASSIGNED TO BACKEND TEAM
    /// =========================================================================
    /// RESPONSIBILITY:
    /// Search the web or developer documentation portals for high-quality, reputable
    /// learning references related to the current course topic.
    ///
    /// STEP-BY-STEP IMPLEMENTATION INSTRUCTIONS:
    /// 1. Use a search API (e.g. Google Custom Search, Tavily, Bing, or DuckDuckGo)
    ///    with site filters prioritized for developer learning:
    ///    - "site:learn.microsoft.com", "site:developer.mozilla.org", "site:github.com"
    /// 2. Extract Title, Url, and a concise summary snippet.
    /// 3. Classify SourceType as:
    ///    - "documentation" (official specs/docs)
    ///    - "tutorial" (walkthroughs / step-by-step guides)
    ///    - "article" (deep dives / best practices)
    /// 4. Fallback Handling:
    ///    - When offline or if API search limits are hit, return curated foundational
    ///      resources so downstream Model 2 & Model 3 always receive valid context.
    /// =========================================================================
    /// </summary>
    public class ExternalResourceResearchService : IExternalResourceResearchService
    {
        private static readonly List<ExternalResourceDto> SeededResources = new()
        {
            new ExternalResourceDto
            {
                Title = "ASP.NET Core Web API Best Practices & Architecture",
                Url = "https://learn.microsoft.com/en-us/aspnet/core/web-api/",
                SourceType = "documentation",
                Summary = "Official architectural guidelines for designing RESTful services, middleware pipelines, and dependency injection in modern .NET."
            },
            new ExternalResourceDto
            {
                Title = "RESTful API Design: RFC 7231 & HTTP Semantics",
                Url = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods",
                SourceType = "documentation",
                Summary = "Comprehensive reference for idempotent HTTP verbs, status codes, and header management in web APIs."
            },
            new ExternalResourceDto
            {
                Title = "Domain-Driven Design and Clean Architecture Patterns",
                Url = "https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures",
                SourceType = "article",
                Summary = "In-depth guide on Onion / Clean Architecture layers, entity modeling, and repository abstractions."
            }
        };

        public Task<List<ExternalResourceDto>> SearchDocumentationAndTutorialsAsync(
            string topic,
            CancellationToken cancellationToken = default)
        {
            // TODO (Backend Team): Connect live search crawler or Tavily/Google Search API.
            // For now, return curated docs filtered by topic or foundational fallbacks.
            var cleanTopic = topic.ToLower();
            var results = SeededResources
                .Where(r => r.Title.ToLower().Contains(cleanTopic) || r.Summary.ToLower().Contains(cleanTopic))
                .ToList();

            if (!results.Any())
            {
                results = SeededResources;
            }

            return Task.FromResult(results);
        }
    }
}
