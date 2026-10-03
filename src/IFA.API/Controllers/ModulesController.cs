using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public ModulesController(
            IApplicationDbContext context,
            CourseGenerationService generationService,
            IAdaptiveEngine adaptiveEngine,
            IResearchService researchService)
        {
            _context = context;
            _generationService = generationService;
            _adaptiveEngine = adaptiveEngine;
            _researchService = researchService;
        }

        [HttpPost("{id}/generate")]
        public async Task<IActionResult> GenerateModule(Guid id, CancellationToken cancellationToken)
        {
            var module = await _context.Modules
                .Include(m => m.Course)
                .Include(m => m.Lessons)
                .Include(m => m.ModuleQuiz)
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
                var research = await _researchService.ConductResearchAsync(
                    $"{module.Course?.Title} — {module.Title}",
                    learnerId,
                    module.CourseId,
                    cancellationToken);

                var academicSources = research.Sources
                    .Where(source => source.SourceType == "Academic")
                    .ToList();
                if (academicSources.Count == 0)
                {
                    throw new InvalidOperationException(
                        "ScholarXiv returned no academic sources for this module. No lesson was generated.");
                }

                var researchContext = string.Join(
                    "\n\n",
                    academicSources.Select(source =>
                        $"Title: {source.Title}\nAuthors: {source.Authors}\nYear: {source.PublishedYear?.ToString() ?? "unknown"}\nURL: {source.Url}\nAbstract: {source.Snippet}"));
                var weakAreas = await _adaptiveEngine.GetAdaptiveConstraintsForNextModuleAsync(
                    learnerId,
                    module.CourseId);

                var jitRequest = new JitModuleGenerationRequest
                {
                    CourseTitle = module.Course?.Title ?? "Ethiopian Grade 12 Natural Science Entrance Exam",
                    ModuleNumber = module.ModuleNumber,
                    ModuleTitle = module.Title,
                    TargetGoal = module.Summary,
                    PriorQuizWeakAreas = weakAreas,
                    ResearchContext = researchContext
                };

                var generated = await _generationService.GenerateJitModuleAsync(jitRequest, cancellationToken);
                generated.Lesson.ModuleId = module.Id;
                generated.Quiz.ModuleId = module.Id;
                module.GenerationStatus = ModuleGenerationStatus.Ready;
                module.GeneratedAt = DateTime.UtcNow;

                _context.Add(generated.Lesson);
                _context.Add(generated.Quiz);
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(module);
            }
            catch
            {
                module.GenerationStatus = ModuleGenerationStatus.Failed;
                await _context.SaveChangesAsync(CancellationToken.None);
                throw;
            }
        }
    }
}
