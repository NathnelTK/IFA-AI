using System;
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
    public class LessonsController : BaseApiController
    {
        private readonly IApplicationDbContext _context;
        private readonly IActivityService _activityService;

        public LessonsController(IApplicationDbContext context, IActivityService activityService)
        {
            _context = context;
            _activityService = activityService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLesson(Guid id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m!.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();

            return Ok(lesson);
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteLesson(Guid id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m!.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();

            lesson.IsCompleted = true;

            var learnerId = await GetCurrentLearnerIdAsync(_context);

            // Update course enrollment progress
            if (lesson.Module?.CourseId != null)
            {
                var courseId = lesson.Module.CourseId;
                var totalLessons = await _context.Lessons.CountAsync(l => l.Module!.CourseId == courseId);
                var completedLessons = await _context.Lessons.CountAsync(l => l.Module!.CourseId == courseId && l.IsCompleted);

                var enrollment = await _context.CourseEnrollments
                    .FirstOrDefaultAsync(e => e.CourseId == courseId && e.LearnerId == learnerId);

                if (enrollment != null && totalLessons > 0)
                {
                    enrollment.ProgressPercentage = (int)Math.Round((double)completedLessons / totalLessons * 100);
                    enrollment.LastAccessedAt = DateTime.UtcNow;
                }
            }

            // Update learner state
            var state = await _context.LearnerStates.FirstOrDefaultAsync(s => s.LearnerId == learnerId);
            if (state != null)
            {
                state.CompletedLessonsCount++;
                state.TotalHoursLearned += (double)lesson.ReadingTimeMinutes / 60.0;
                state.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await _activityService.RecordActivityAsync(
                learnerId,
                "lesson_completed",
                $"Completed lesson: {lesson.Title}",
                $"Read and completed {lesson.Title} ({lesson.ReadingTimeMinutes} mins).");

            return Ok(new { success = true, lessonId = id, isCompleted = true });
        }
    }
}
