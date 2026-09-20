// IFA.Infrastructure/Services/ResearchService.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    /// <summary>
    /// Research Agent (PR 2.3). Wraps IScholarxivService and normalizes
    /// its raw output into a ResearchPackage BEFORE anything downstream
    /// (Course Architect / Model 2) ever sees it. Model 2 must only ever
    /// see AcademicEvidence, never a ScholarxivPaperSummary directly -
    /// that's the boundary this class exists to enforce.
    /// </summary>
    public class ResearchService : IResearchService
    {
        // Independent of whatever timeout the HttpClient inside
        // ScholarxivService has - this guarantees the PIPELINE never
        // hangs waiting on research, even if the underlying client's
        // own timeout is misconfigured or missing.
        private static readonly TimeSpan ScholarxivTimeout = TimeSpan.FromSeconds(20);
        private const int MaxPapersPerQuery = 5;

        private readonly IScholarxivService _scholarxivService;
        private readonly ILogger<ResearchService> _logger;

        public ResearchService(IScholarxivService scholarxivService, ILogger<ResearchService> logger)
        {
            _scholarxivService = scholarxivService;
            _logger = logger;
        }

        public async Task<ResearchPackage> BuildResearchPackageAsync(
            LearnerProfile profile,
            CancellationToken cancellationToken = default)
        {
            var package = new ResearchPackage { LearnerProfileId = profile.Id };
            var query = BuildQuery(profile);

            try
            {
                using var timeoutCts = new CancellationTokenSource(ScholarxivTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                var papers = await _scholarxivService.SearchPapersAsync(
                    query, MaxPapersPerQuery, linkedCts.Token);

                package.AcademicSources = papers
                    .Select(p => MapToEvidence(p, profile))
                    .ToList();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Our 20s timeout fired, not the caller's own cancellation.
                // This is the "failures/timeouts are handled" acceptance
                // criterion from PR 2.3 - a slow Scholarxiv response must
                // degrade gracefully, not take down the whole orchestrator.
                _logger.LogWarning(
                    "Scholarxiv search timed out after {Timeout}s for query '{Query}'. " +
                    "Continuing with no academic sources.", ScholarxivTimeout.TotalSeconds, query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Scholarxiv search failed for query '{Query}'. Continuing with no academic sources.", query);
            }

            // PracticalResources / VideoResources stay empty here - PR 2.4
            // populates those. Empty, never null, so nothing downstream
            // needs a null-check on these collections.
            return package;
        }

        private static string BuildQuery(LearnerProfile profile) =>
            string.IsNullOrWhiteSpace(profile.SubjectTopic)
                ? profile.Goal
                : $"{profile.Goal} {profile.SubjectTopic}";

        private static AcademicEvidence MapToEvidence(ScholarxivPaperSummary paper, LearnerProfile profile) =>
            new()
            {
                Title = paper.Title,
                Authors = paper.Authors,
                Summary = paper.Abstract,
                Doi = paper.Doi,
                // Placeholder relevance logic - good enough for MVP, but
                // flag it: real relevance scoring (matching against
                // profile.KnownWeaknesses, semantic similarity, etc.)
                // is worth revisiting once PR 2.4/3.1 land, not now.
                RelevanceNote = $"Relevant to goal: {profile.Goal}"
            };
    }
}