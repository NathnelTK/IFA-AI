using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesController : BaseApiController
    {
        private readonly IApplicationDbContext _context;
        private readonly IAdaptiveEngine _adaptiveEngine;

        public QuizzesController(IApplicationDbContext context, IAdaptiveEngine adaptiveEngine)
        {
            _context = context;
            _adaptiveEngine = adaptiveEngine;
        }

        public class SubmitQuizDto
        {
            public Dictionary<Guid, int> Answers { get; set; } = new Dictionary<Guid, int>();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuiz(Guid id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Module)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound(new { message = "Quiz not found." });

            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var lastAttempt = await _context.Assessments
                .Where(a => a.QuizId == id && a.LearnerId == learnerId)
                .OrderByDescending(a => a.SubmittedAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                quiz.Id,
                quiz.ModuleId,
                quiz.Title,
                quiz.PassingScorePercentage,
                LastScorePercentage = lastAttempt?.ScorePercentage,
                IsPassed = lastAttempt?.IsPassed ?? false,
                Questions = quiz.Questions.Select(q => new
                {
                    q.Id,
                    q.Prompt,
                    q.Options,
                    q.TargetSkillName,
                    q.BloomTaxonomyLevel
                })
            });
        }

        [HttpPost("{id}/submit")]
        public async Task<IActionResult> SubmitQuiz(Guid id, [FromBody] SubmitQuizDto dto)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var request = new QuizSubmissionRequest
            {
                QuizId = id,
                Answers = dto.Answers ?? new Dictionary<Guid, int>()
            };

            var result = await _adaptiveEngine.EvaluateAndAdaptAsync(learnerId, request);
            return Ok(result);
        }
    }
}
