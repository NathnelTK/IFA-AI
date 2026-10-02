using System;
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

        public ModulesController(
            IApplicationDbContext context,
            CourseGenerationService generationService,
            IAdaptiveEngine adaptiveEngine)
        {
            _context = context;
            _generationService = generationService;
            _adaptiveEngine = adaptiveEngine;
        }

        [HttpPost("{id}/generate")]
        public async Task<IActionResult> GenerateModule(Guid id)
        {
            var module = await _context.Modules
                .Include(m => m.Course)
                .Include(m => m.Lessons)
                .Include(m => m.ModuleQuiz)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null) return NotFound(new { message = "Module not found." });

            if (module.IsGenerated && module.Lessons.Count > 0)
            {
                return Ok(module);
            }

            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var weakAreas = await _adaptiveEngine.GetAdaptiveConstraintsForNextModuleAsync(learnerId, module.CourseId);

            var jitRequest = new JitModuleGenerationRequest
            {
                CourseTitle = module.Course?.Title ?? "Software Engineering",
                ModuleNumber = module.ModuleNumber,
                ModuleTitle = module.Title,
                TargetGoal = module.Summary,
                PriorQuizWeakAreas = weakAreas
            };

            var generated = await _generationService.GenerateJitModuleAsync(jitRequest);

            module.IsGenerated = true;
            module.GeneratedAt = DateTime.UtcNow;

            generated.Lesson.ModuleId = module.Id;
            _context.Lessons.Add(generated.Lesson);

            generated.Quiz.ModuleId = module.Id;
            _context.Quizzes.Add(generated.Quiz);

            await _context.SaveChangesAsync();

            return Ok(module);
        }
    }
}
