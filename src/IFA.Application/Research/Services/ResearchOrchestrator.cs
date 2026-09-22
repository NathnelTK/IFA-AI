using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Learning.DTOs;
using IFA.Application.Research.DTOs;
using IFA.Domain.Entities;

namespace IFA.Application.Research.Services
{
    public interface IResearchOrchestrator
    {
        Task<ResearchPackageDto> OrchestrateResearchAsync(
            LearnerProfileDto profile,
            CancellationToken cancellationToken = default);

        ResearchPackage MapToEntity(ResearchPackageDto dto, Guid learnerProfileId);
    }

    /// <summary>
    /// Model 1: Research Orchestrator.
    /// Bridges the learner's profile with academic grounding (Scholarxiv) and
    /// technical/multimedia resources (YouTube, official documentation).
    /// </summary>
    public class ResearchOrchestrator : IResearchOrchestrator
    {
        private readonly IUnifiedResearchService _unifiedResearchService;

        public ResearchOrchestrator(IUnifiedResearchService unifiedResearchService)
        {
            _unifiedResearchService = unifiedResearchService;
        }

        public async Task<ResearchPackageDto> OrchestrateResearchAsync(
            LearnerProfileDto profile,
            CancellationToken cancellationToken = default)
        {
            var topic = string.IsNullOrWhiteSpace(profile.Goal) ? "C# Backend Engineering" : profile.Goal;
            return await _unifiedResearchService.BuildResearchPackageAsync(profile, topic, cancellationToken);
        }

        public ResearchPackage MapToEntity(ResearchPackageDto dto, Guid learnerProfileId)
        {
            var entity = new ResearchPackage
            {
                LearnerProfileId = learnerProfileId,
                AcademicSources = dto.AcademicSources.Select(a => new AcademicEvidence
                {
                    Title = a.Title,
                    Authors = a.Authors,
                    Summary = a.Abstract,
                    Doi = a.Doi,
                    PublishedYear = a.PublishedYear,
                    RelevanceNote = a.RelevanceNote
                }).ToList(),
                PracticalResources = dto.ExternalResources.Select(r => new PracticalResource
                {
                    Title = r.Title,
                    Url = r.Url,
                    SourceType = r.SourceType
                }).ToList(),
                VideoResources = dto.VideoResources.Select(v => new VideoResource
                {
                    Title = v.Title,
                    YouTubeVideoId = v.VideoId,
                    ChannelName = v.ChannelTitle,
                    FromPreferredChannel = v.IsPreferredCreator
                }).ToList()
            };

            return entity;
        }
    }
}
