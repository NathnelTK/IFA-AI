using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
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
                .ThenInclude(m => m!.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound(new { message = "Quiz not found." });

            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var access = await CourseAccess.GetAsync(_context, quiz.Module!.CourseId, learnerId, HttpContext.RequestAborted);
            access.RequireContent();
            var progress = await CourseProgress.GetAsync(_context, access.Course, access.IsOwner ? null : learnerId, HttpContext.RequestAborted);
            return Ok(CourseProgress.QuizDto(quiz, access.Course, access.IsOwner, progress));
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

            var ct = HttpContext.RequestAborted;
            var result = await _adaptiveEngine.EvaluateAndAdaptAsync(learnerId, request, ct);
            var quiz = await _context.Quizzes.Include(q => q.Module).ThenInclude(m => m!.Course).SingleAsync(q => q.Id == id, ct);
            var course = quiz.Module!.Course!;
            var progress = await CourseProgress.GetAsync(_context, course, learnerId, ct);
            var attempts = progress.AttemptsFor(id);
            return Ok(new
            {
                result.AssessmentId,
                result.ScorePercentage,
                result.IsPassed,
                result.TotalQuestions,
                result.CorrectAnswersCount,
                result.QuestionFeedbacks,
                result.MasteredSkills,
                result.WeakSkillsToReinforce,
                result.AdaptiveNextAction,
                CourseId = course.Id,
                AttemptNumber = attempts.Single(a => a.Id == result.AssessmentId).AttemptNumber,
                AttemptCount = attempts.Count,
                MaxAttempts = course.MaxQuizAttempts,
                ProgressPercentage = progress.Percentage,
                Progress = progress
            });
        }
    }
}
