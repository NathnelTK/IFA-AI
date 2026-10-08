using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Application.Skills.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    /// <summary>
    /// Aggregates every number the Progress page shows from this learner's OWN
    /// rows: enrollments, learner state, assessments and learning activity.
    /// Nothing here is hard-coded, so two learners with different activity get
    /// different progress.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProgressController : BaseApiController
    {
        private readonly IApplicationDbContext _context;
        private readonly SkillProfileService _skillProfileService;

        public ProgressController(IApplicationDbContext context, SkillProfileService skillProfileService)
        {
            _context = context;
            _skillProfileService = skillProfileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProgress()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var enrollments = await _context.CourseEnrollments
                .Where(e => e.LearnerId == learnerId)
                .Include(e => e.Course)
                .ToListAsync();

            var totalCourses = enrollments.Count;
            var completedCourses = enrollments.Count(e => e.ProgressPercentage >= 100);
            var inProgressCourses = enrollments.Count(e => e.ProgressPercentage is > 0 and < 100);
            var overallProgress = totalCourses == 0
                ? 0
                : (int)Math.Round(enrollments.Average(e => e.ProgressPercentage));

            var state = await _context.LearnerStates.FirstOrDefaultAsync(s => s.LearnerId == learnerId);
            var totalHours = state?.TotalHoursLearned ?? 0;
            var streakDays = state?.ActiveStreakDays ?? 0;

            // ---- Weekly activity: derive hours per weekday from real
            // lesson-completion timestamps in the last 7 days. ----
            var since = DateTime.UtcNow.AddDays(-6).Date;
            var completions = await _context.LessonProgress
                .Where(p => p.LearnerId == learnerId && p.IsCompleted && p.CompletedAt != null)
                .Select(p => new { p.CompletedAt, p.Lesson!.ReadingTimeMinutes })
                .ToListAsync();

            var weekStart = DateTime.UtcNow.Date.AddDays(-6);
            var weeklyBuckets = Enumerable.Range(0, 7).Select(offset =>
            {
                var day = weekStart.AddDays(offset);
                var minutes = completions
                    .Where(c => c.CompletedAt!.Value.Date == day)
                    .Sum(c => c.ReadingTimeMinutes);
                return new
                {
                    day = day.ToString("ddd"),
                    date = day.ToString("yyyy-MM-dd"),
                    hours = Math.Round(minutes / 60.0, 1)
                };
            }).ToList();

            var weeklyStudyHours = Math.Round(weeklyBuckets.Sum(b => b.hours), 1);

            // ---- Assessment history from real Assessment rows. ----
            var totalQuestions = await _context.Questions.CountAsync();

            var assessments = await _context.Assessments
                .Where(a => a.LearnerId == learnerId)
                .OrderByDescending(a => a.SubmittedAt)
                .Take(20)
                .Select(a => new
                {
                    a.Id,
                    a.ScorePercentage,
                    a.IsPassed,
                    a.AttemptNumber,
                    a.SubmittedAt,
                    Title = a.Quiz!.Title,
                    Kind = (int)a.Quiz.Kind,
                    Course = a.Quiz.Module!.Course!.Title,
                    CourseId = a.Quiz.Module.Course.Id
                })
                .ToListAsync();

            var assessmentDtos = assessments.Select(a => new
            {
                id = a.Id,
                type = a.Kind == 1 ? "test" : "quiz",
                title = a.Title,
                course = a.Course,
                courseId = a.CourseId,
                score = a.ScorePercentage,
                passed = a.IsPassed,
                attemptNumber = a.AttemptNumber,
                submittedAt = a.SubmittedAt
            }).ToList();

            var avgQuizScore = assessments.Count == 0
                ? 0
                : (int)Math.Round(assessments.Average(a => a.ScorePercentage));

            // ---- Skills (real per-learner profile). ----
            var skillsResult = await _skillProfileService.GetLearnerSkillsAsync(learnerId);

            var weakAreas = skillsResult.Categories
                .SelectMany(c => c.Skills)
                .Where(s => s.Percentage < 50)
                .OrderBy(s => s.Percentage)
                .Select(s => new
                {
                    skill = s.Name,
                    currentLevel = s.Percentage,
                    targetLevel = Math.Min(100, s.Percentage + 30)
                })
                .ToList();

            return Ok(new
            {
                stats = new
                {
                    totalCourses,
                    completedCourses,
                    inProgressCourses,
                    overallProgress,
                    totalStudyTime = FormatHours(totalHours),
                    totalHours = Math.Round(totalHours, 1),
                    weeklyStudyTime = FormatHours(weeklyStudyHours),
                    weeklyStudyHours,
                    learningStreak = streakDays
                },
                weeklyActivity = weeklyBuckets,
                skillProgress = skillsResult.Categories
                    .SelectMany(c => c.Skills)
                    .Select(s => new { s.Name, s.Percentage, s.Improvement, s.Color })
                    .ToList(),
                assessments = assessmentDtos,
                weakAreas,
                avgQuizScore,
                totalQuestions
            });
        }

        private static string FormatHours(double hours)
        {
            var totalMinutes = (int)Math.Round(hours * 60);
            var h = totalMinutes / 60;
            var m = totalMinutes % 60;
            if (h <= 0) return $"{m}m";
            return $"{h}h {m}m";
        }
    }
}
