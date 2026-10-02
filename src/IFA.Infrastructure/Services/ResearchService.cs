using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Services
{
    public class ResearchService : IResearchService
    {
        private readonly IApplicationDbContext _context;
        private readonly IScholarxivService _scholarxivService;
        private readonly IYouTubeResourceService _youtubeService;

        public ResearchService(
            IApplicationDbContext context,
            IScholarxivService scholarxivService,
            IYouTubeResourceService youtubeService)
        {
            _context = context;
            _scholarxivService = scholarxivService;
            _youtubeService = youtubeService;
        }

        public async Task<ResearchPackage> ConductResearchAsync(string topic, Guid? learnerId = null, Guid? courseId = null, CancellationToken ct = default)
        {
            var cleanTopic = string.IsNullOrWhiteSpace(topic) ? "Software Architecture" : topic.Trim();

            var papersTask = _scholarxivService.SearchPapersAsync(cleanTopic, 3, ct);
            var videosTask = _youtubeService.SearchEducationalVideosAsync(cleanTopic, "", 3, ct);

            await Task.WhenAll(papersTask, videosTask);

            var papers = await papersTask;
            var videos = await videosTask;

            var keyConcepts = papers.SelectMany(p => p.KeyTopics).Distinct().Take(5).ToList();
            if (!keyConcepts.Any())
            {
                keyConcepts = new List<string> { cleanTopic, "Architecture", "Engineering", "Evaluation" };
            }

            var researchPackage = new ResearchPackage
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                CourseId = courseId,
                Topic = cleanTopic,
                Summary = $"Synthesized research on {cleanTopic} from peer-reviewed publications and leading engineering resources.",
                KeyConceptsJson = JsonSerializer.Serialize(keyConcepts),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var paper in papers)
            {
                researchPackage.Sources.Add(new ResearchSource
                {
                    Id = Guid.NewGuid(),
                    ResearchPackageId = researchPackage.Id,
                    Title = paper.Title,
                    Url = !string.IsNullOrWhiteSpace(paper.Doi) && paper.Doi.StartsWith("http") ? paper.Doi : $"https://doi.org/{paper.Doi}",
                    SourceType = "Academic",
                    Authors = paper.Authors,
                    Snippet = paper.Abstract,
                    RelevanceScore = 0.95,
                    PublishedYear = int.TryParse(paper.PublishedYear, out var yr) ? yr : 2024
                });
            }

            foreach (var video in videos)
            {
                researchPackage.Sources.Add(new ResearchSource
                {
                    Id = Guid.NewGuid(),
                    ResearchPackageId = researchPackage.Id,
                    Title = video.Title,
                    Url = $"https://www.youtube.com/watch?v={video.VideoId}",
                    SourceType = "Video",
                    Authors = video.ChannelTitle,
                    Snippet = $"Educational video resource covering {cleanTopic}.",
                    RelevanceScore = 0.90,
                    PublishedYear = DateTime.UtcNow.Year
                });
            }

            _context.ResearchPackages.Add(researchPackage);
            await _context.SaveChangesAsync(ct);

            return researchPackage;
        }

        public async Task<ResearchPackage?> GetResearchForCourseAsync(Guid courseId, CancellationToken ct = default)
        {
            return await _context.ResearchPackages
                .Include(r => r.Sources)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(r => r.CourseId == courseId, ct);
        }
    }
}
