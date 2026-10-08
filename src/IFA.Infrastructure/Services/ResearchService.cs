using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    public class ResearchService : IResearchService
    {
        private readonly IApplicationDbContext _context;
        private readonly IScholarxivService _scholarxivService;
        private readonly IYouTubeResourceService _youtubeService;
        private readonly IImageResourceService _imageService;
        private readonly Microsoft.Extensions.Logging.ILogger<ResearchService> _logger;

        public ResearchService(
            IApplicationDbContext context,
            IScholarxivService scholarxivService,
            IYouTubeResourceService youtubeService,
            IImageResourceService imageService,
            Microsoft.Extensions.Logging.ILogger<ResearchService> logger)
        {
            _context = context;
            _scholarxivService = scholarxivService;
            _youtubeService = youtubeService;
            _imageService = imageService;
            _logger = logger;
        }

        /// <summary>External research APIs regularly return strings longer than
        /// the persistence schema allows; clamp at the persistence boundary.</summary>
        private static string Clamp(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty
            : value.Length <= max ? value
            : value.Substring(0, max);

        public async Task<ResearchPackage> ConductResearchAsync(string topic, Guid? learnerId = null, Guid? courseId = null, CancellationToken ct = default)
        {
            var cleanTopic = string.IsNullOrWhiteSpace(topic) ? "Software Architecture" : topic.Trim();

            var papersTask = _scholarxivService.SearchPapersAsync(cleanTopic, 3, ct);
            var videosTask = _youtubeService.SearchEducationalVideosAsync(cleanTopic, "", 3, ct);
            var imagesTask = _imageService.SearchImagesAsync(cleanTopic, 3, ct);
            var graphsTask = _imageService.SearchImagesAsync($"{cleanTopic} diagram", 2, ct);

            await Task.WhenAll(papersTask, videosTask, imagesTask, graphsTask);

            var papers = await papersTask;
            var videos = await videosTask;
            var images = await imagesTask;
            var graphs = await graphsTask;

            var keyConcepts = papers.SelectMany(p => p.KeyTopics).Distinct().Take(5).ToList();

            var researchPackage = new ResearchPackage
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                CourseId = courseId,
                Topic = Clamp(cleanTopic, 300),
                Summary = papers.Count == 0
                    ? $"No academic papers were returned for {cleanTopic}. Try a narrower search or verify the ScholarXiv configuration."
                    : $"ScholarXiv returned {papers.Count} academic result(s) for {cleanTopic}. Review each paper's abstract and publication details before relying on it.",
                KeyConceptsJson = JsonSerializer.Serialize(keyConcepts),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var paper in papers)
            {
                researchPackage.Sources.Add(new ResearchSource
                {
                    Id = Guid.NewGuid(),
                    ResearchPackageId = researchPackage.Id,
                    Title = Clamp(paper.Title, 300),
                    Url = Clamp(
                        string.IsNullOrWhiteSpace(paper.Doi)
                            ? string.Empty
                            : paper.Doi.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? paper.Doi
                                : $"https://doi.org/{paper.Doi}",
                        1000),
                    SourceType = "Academic",
                    Authors = Clamp(paper.Authors, 300),
                    Snippet = Clamp(paper.Abstract, 20000),
                    RelevanceScore = 0,
                    PublishedYear = int.TryParse(paper.PublishedYear, out var yr) ? yr : null
                });
            }

            foreach (var video in videos)
            {
                researchPackage.Sources.Add(new ResearchSource
                {
                    Id = Guid.NewGuid(),
                    ResearchPackageId = researchPackage.Id,
                    Title = Clamp(video.Title, 300),
                    Url = Clamp($"https://www.youtube.com/watch?v={video.VideoId}", 1000),
                    SourceType = "Video",
                    Authors = Clamp(video.ChannelTitle, 300),
                    Snippet = $"Educational video resource covering {cleanTopic}.",
                    RelevanceScore = 0,
                    PublishedYear = DateTime.UtcNow.Year
                });
            }

            // Representative images and diagrams/graphs the builder can embed to
            // illustrate lessons (real hosted URLs, never hallucinated).
            foreach (var image in images)
            {
                researchPackage.Sources.Add(new ResearchSource
                {
                    Id = Guid.NewGuid(),
                    ResearchPackageId = researchPackage.Id,
                    Title = Clamp(image.Title, 300),
                    Url = Clamp(image.ImageUrl, 1000),
                    SourceType = "Image",
                    Authors = Clamp(image.Source, 300),
                    Snippet = $"Illustration for {cleanTopic}.",
                    RelevanceScore = 0
                });
            }

            foreach (var graph in graphs)
            {
                researchPackage.Sources.Add(new ResearchSource
                {
                    Id = Guid.NewGuid(),
                    ResearchPackageId = researchPackage.Id,
                    Title = Clamp(graph.Title, 300),
                    Url = Clamp(graph.ImageUrl, 1000),
                    SourceType = "Graph",
                    Authors = Clamp(graph.Source, 300),
                    Snippet = $"Diagram/graph related to {cleanTopic}.",
                    RelevanceScore = 0
                });
            }

            _context.Add(researchPackage);
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception saveEx)
            {
                // Research persistence is best-effort: never let a saving failure
                // abort course/module generation. Keep the in-memory package so
                // this generation can still use it; detach so later saves in the
                // request are not poisoned by the failed entity.
                _logger.LogWarning(saveEx, "Saving the research package failed; continuing with in-memory research only.");
                if (_context is DbContext concreteContext)
                {
                    concreteContext.Entry(researchPackage).State = EntityState.Detached;
                }
            }

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
