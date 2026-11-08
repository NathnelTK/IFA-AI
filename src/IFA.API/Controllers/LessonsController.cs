using System;
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

            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var access = await CourseAccess.GetAsync(_context, lesson.Module!.CourseId, learnerId, HttpContext.RequestAborted);
            access.RequireContent();
            var progress = await CourseProgress.GetAsync(_context, access.Course, access.IsOwner ? null : learnerId, HttpContext.RequestAborted);
            return Ok(CourseProgress.LessonDto(lesson, true, progress));
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteLesson(Guid id)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var ct = HttpContext.RequestAborted;
            await using var transaction = await CourseAccess.BeginMutationAsync(_context, ct);
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m!.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();

            var access = await CourseAccess.GetAsync(_context, lesson.Module!.CourseId, learnerId, ct);
            access.RequireContent(learnerMutation: true);

            // Completion is per-learner and lives on LessonProgress.
            var progress = await _context.LessonProgress
                .FirstOrDefaultAsync(p => p.LessonId == id && p.LearnerId == learnerId);
            var alreadyCompleted = progress?.IsCompleted == true;
            if (progress == null)
            {
                _context.Add(new LessonProgress
                {
                    Id = Guid.NewGuid(),
                    LessonId = id,
                    LearnerId = learnerId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                });
            }
            else if (!alreadyCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt ??= DateTime.UtcNow;
            }

            if (!alreadyCompleted)
            {
                var state = await _context.LearnerStates.FirstOrDefaultAsync(s => s.LearnerId == learnerId, ct);
                if (state != null)
                {
                    state.CompletedLessonsCount++;
                    state.TotalHoursLearned += Math.Max(0, lesson.ReadingTimeMinutes) / 60.0;
                    state.CurrentCourseId = access.Course.Id;
                    state.CurrentModuleId = lesson.ModuleId;
                    state.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(ct);
                await _activityService.RecordActivityAsync(
                    learnerId,
                    "lesson_completed",
                    $"Completed lesson: {lesson.Title}",
                    $"Read and completed {lesson.Title} ({lesson.ReadingTimeMinutes} mins).", ct: ct);
            }

            var courseProgress = await CourseProgress.UpdateAsync(_context, access.Course, access.Enrollment!, ct,
                updateLastAccessed: !alreadyCompleted);
            if (transaction != null) await transaction.CommitAsync(ct);
            return Ok(new
            {
                success = true,
                lessonId = id,
                isCompleted = true,
                alreadyCompleted,
                courseId = access.Course.Id,
                progressPercentage = courseProgress.Percentage,
                progress = courseProgress
            });
        }
    }
}
