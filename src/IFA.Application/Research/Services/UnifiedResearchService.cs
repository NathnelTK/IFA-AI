using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Application.Learning.DTOs;
using IFA.Application.Research.DTOs;

namespace IFA.Application.Research.Services
{
    public interface IUnifiedResearchService
    {
        Task<ResearchPackageDto> BuildResearchPackageAsync(
            LearnerProfileDto profile,
            string topic,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Model 1 Research Orchestrator core service.
    /// Unifies academic evidence from Scholarxiv, educational videos from YouTube,
    /// and official technical documentation into a cohesive ResearchPackageDto.
    /// </summary>
    public class UnifiedResearchService : IUnifiedResearchService
    {
        private readonly IScholarxivService _scholarxivService;
        private readonly IYouTubeResourceService _youtubeService;
        private readonly IExternalResourceResearchService _externalResourceService;

        public UnifiedResearchService(
            IScholarxivService scholarxivService,
            IYouTubeResourceService youtubeService,
            IExternalResourceResearchService externalResourceService)
        {
            _scholarxivService = scholarxivService;
            _youtubeService = youtubeService;
            _externalResourceService = externalResourceService;
        }

        public async Task<ResearchPackageDto> BuildResearchPackageAsync(
            LearnerProfileDto profile,
            string topic,
            CancellationToken cancellationToken = default)
        {
            var preferredCreator = profile.PreferredCreators.FirstOrDefault() ?? string.Empty;

            // 1. Fetch Scholarxiv academic evidence
            var papers = await _scholarxivService.SearchPapersAsync(topic, 3, cancellationToken);

            // 2. Fetch YouTube educational videos (respecting learner's preferred creator)
            var videos = await _youtubeService.SearchEducationalVideosAsync(topic, preferredCreator, 2, cancellationToken);

            // 3. Fetch official documentation and technical tutorials
            var docs = await _externalResourceService.SearchDocumentationAndTutorialsAsync(topic, cancellationToken);

            // 4. Synthesize research package
            var package = new ResearchPackageDto
            {
                LearnerContext = profile,
                AcademicSources = papers.Select(p => new AcademicSourceDto
                {
                    Title = p.Title,
                    Authors = p.Authors,
                    Abstract = p.Abstract,
                    Doi = p.Doi,
                    PublishedYear = p.PublishedYear,
                    RelevanceNote = $"Academic grounding for pedagogical structure in {topic}."
                }).ToList(),
                VideoResources = videos.Select(v => new VideoResourceDto
                {
                    VideoId = v.VideoId,
                    Title = v.Title,
                    ChannelTitle = v.ChannelTitle,
                    Duration = v.Duration,
                    ThumbnailUrl = v.ThumbnailUrl,
                    IsPreferredCreator = !string.IsNullOrEmpty(preferredCreator) &&
                                         v.ChannelTitle.Contains(preferredCreator, System.StringComparison.OrdinalIgnoreCase)
                }).ToList(),
                ExternalResources = docs,
                ResearchFindings = new List<string>
                {
                    $"Academic consensus emphasizes layered separation of concerns and progressive cognitive load for '{topic}'.",
                    $"Formative assessments with distractor rationales significantly improve retention for {profile.CurrentLevel} learners.",
                    $"Direct alignment with real-world tooling produces highest employment readiness outcomes."
                },
                LearningObjectives = new List<string>
                {
                    $"Master core architectural paradigms and syntax for {topic}",
                    $"Apply domain separation principles to structured backend workflows",
                    $"Validate competency through hands-on diagnostics and progressive evaluations"
                },
                RecommendedSequence = new List<string>
                {
                    $"{topic}: Conceptual Foundations & High-Level Architecture",
                    $"{topic}: Practical Hands-On Implementation & Patterns",
                    $"{topic}: Testing, Diagnostics & Real-World Best Practices"
                }
            };

            return package;
        }
    }
}
