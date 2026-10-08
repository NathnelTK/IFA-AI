using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModulesController : BaseApiController
    {
        private readonly IApplicationDbContext _context;
        private readonly CourseGenerationService _generationService;
        private readonly IAdaptiveEngine _adaptiveEngine;
        private readonly IResearchService _researchService;
        private readonly ILogger<ModulesController> _logger;

        public ModulesController(
            IApplicationDbContext context,
            CourseGenerationService generationService,
            IAdaptiveEngine adaptiveEngine,
            IResearchService researchService,
            ILogger<ModulesController> logger)
        {
            _context = context;
            _generationService = generationService;
            _adaptiveEngine = adaptiveEngine;
            _researchService = researchService;
            _logger = logger;
        }

        [HttpPost("{id}/generate")]
        public async Task<IActionResult> GenerateModule(Guid id, CancellationToken cancellationToken)
        {
            var module = await _context.Modules
                .Include(m => m.Course)
                .Include(m => m.Lessons)
                .Include(m => m.Quizzes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null) return NotFound(new { message = "Module not found." });

            if (module.GenerationStatus == ModuleGenerationStatus.Ready && module.Lessons.Count > 0)
            {
                return Ok(module);
            }

            if (module.GenerationStatus == ModuleGenerationStatus.Generating)
            {
                return Conflict(new { message = "This module is already being generated." });
            }

            module.GenerationStatus = ModuleGenerationStatus.Generating;
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var learnerId = await GetCurrentLearnerIdAsync(_context);

                // Pipeline stage 2 (Research Agent) runs once per course and is
                // persisted as a ResearchPackage. Stage 3 (this endpoint) only
                // builds content from the research already on hand; it must never
                // re-run research for each module, and it must generate only the
                // module the learner actually clicked. If a course was seeded
                // without research yet, run stage 2 once here and reuse it later.
                var research = await _researchService.GetResearchForCourseAsync(module.CourseId, cancellationToken);
                if (research is null)
                {
                    try
                    {
                        research = await _researchService.ConductResearchAsync(
                            module.Course?.Title ?? module.Title,
                            learnerId,
                            module.CourseId,
                            cancellationToken);
                    }
                    catch (Exception researchEx)
                    {
                        // Research is a best-effort grounding step; generation
                        // must still work when the research provider is down.
                        _logger.LogWarning(researchEx, "Research for course {CourseId} failed; generating from module summary.", module.CourseId);
                    }
                }

                var sources = research?.Sources ?? new List<ResearchSource>();
                var academicSources = sources.Where(s => s.SourceType == "Academic").ToList();
                var videoSources = sources.Where(s => s.SourceType == "Video").ToList();
                var imageSources = sources.Where(s => s.SourceType == "Image" || s.SourceType == "Graph").ToList();

                var contextParts = new List<string>();
                foreach (var s in academicSources)
                {
                    contextParts.Add(
                        $"[PAPER] Title: {s.Title}\nAuthors: {s.Authors}\nYear: {s.PublishedYear?.ToString() ?? "unknown"}\nURL: {s.Url}\nAbstract: {s.Snippet}");
                }
                foreach (var v in videoSources)
                {
                    // Passing the video URL lets the builder embed a real, verified
                    // YouTube video (it extracts the id) instead of inventing one.
                    contextParts.Add($"[VIDEO] Title: {v.Title}\nChannel: {v.Authors}\nURL: {v.Url}");
                }
                foreach (var img in imageSources)
                {
                    // Real hosted image/diagram URLs the builder may embed as markdown.
                    var label = img.SourceType == "Graph" ? "GRAPH" : "IMAGE";
                    contextParts.Add($"[{label}] Title: {img.Title}\nURL: {img.Url}");
                }

                var researchContext = contextParts.Count > 0
                    ? string.Join("\n\n", contextParts)
                    : $"No external sources were available. Build these lessons from the module summary: {module.Summary}";

                var weakAreas = await _adaptiveEngine.GetAdaptiveConstraintsForNextModuleAsync(
                    learnerId,
                    module.CourseId);

                var jitRequest = new JitModuleGenerationRequest
                {
                    CourseTitle = module.Course?.Title ?? "IFA Personalized Course",
                    ModuleNumber = module.ModuleNumber,
                    ModuleTitle = module.Title,
                    TargetGoal = module.Summary,
                    PriorQuizWeakAreas = weakAreas,
                    ResearchContext = researchContext,
                    ExternalMaterials = GetCourseMaterialsAsync(module.Course, cancellationToken)
                };

                var generated = await _generationService.GenerateJitModuleAsync(jitRequest, cancellationToken);

                // The builder returns its own throwaway Module that groups the
                // lessons and quiz. Only the lessons and quiz belong to the real
                // (tracked) module, so detach the throwaway graph first — otherwise
                // EF tries to persist a second Module row and the update of the real
                // module fails with a concurrency error.
                foreach (var lesson in generated.Lessons)
                {
                    lesson.Module = null;
                    lesson.ModuleId = module.Id;
                    _context.Add(lesson);
                }

                foreach (var quiz in generated.Quizzes)
                {
                    quiz.Module = null;
                    foreach (var question in quiz.Questions)
                    {
                        question.Quiz = null;
                    }
                    quiz.ModuleId = module.Id;
                    _context.Add(quiz);
                }

                module.GenerationStatus = ModuleGenerationStatus.Ready;
                module.GeneratedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                var updated = await _context.Modules
                    .Include(m => m.Lessons)
                    .Include(m => m.Quizzes)
                    .ThenInclude(q => q.Questions)
                    .FirstAsync(m => m.Id == module.Id, cancellationToken);

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                // The AI provider returned content that failed validation. This is
                // a recoverable request error (422), not a server crash.
                _logger.LogWarning(ex, "Module {ModuleId} generation rejected: {Message}", id, ex.Message);
                module.GenerationStatus = ModuleGenerationStatus.Failed;
                try { await _context.SaveChangesAsync(CancellationToken.None); }
                catch { /* the failure status is best-effort; the request error already carries the cause */ }
                return UnprocessableEntity(new
                {
                    message = $"Could not generate this module: {ex.Message}",
                    moduleId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Module {ModuleId} generation failed unexpectedly.", id);
                module.GenerationStatus = ModuleGenerationStatus.Failed;
                try { await _context.SaveChangesAsync(CancellationToken.None); }
                catch { /* the failure status is best-effort; the request error already carries the cause */ }
                return StatusCode(500, new
                {
                    message = "Module generation failed. Please try again, or check the AI provider configuration.",
                    moduleId = id
                });
            }
        }
        /// <summary>
        /// Materials captured when the course was created, stored on the course
        /// itself so each course sees only its own sources.
        /// </summary>
        private static List<string> GetCourseMaterialsAsync(Course? course, CancellationToken ct)
        {
            if (course is null || string.IsNullOrWhiteSpace(course.ExternalMaterials))
            {
                return new List<string>();
            }

            return course.ExternalMaterials
                .Split(';', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                .Where(s => s.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
