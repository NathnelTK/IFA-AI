using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Application.Courses.Commands;
using IFA.Application.Courses.Services;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : BaseApiController
    {
        private readonly IApplicationDbContext _context;
        private readonly CourseGenerationService _generationService;
        private readonly ICourseSharingService _sharingService;
        private readonly IActivityService _activityService;

        public CoursesController(
            IApplicationDbContext context,
            CourseGenerationService generationService,
            ICourseSharingService sharingService,
            IActivityService activityService)
        {
            _context = context;
            _generationService = generationService;
            _sharingService = sharingService;
            _activityService = activityService;
        }

        /// <summary>Mirrors Course.ThumbnailUrl's column length.</summary>
        private const int MaxCoverImageUrlLength = 4096;

        public class CreateCourseRequest
        {
            public string Goal { get; set; } = string.Empty;
            public int HoursPerWeek { get; set; } = 5;
            public string PreferredCreator { get; set; } = "freeCodeCamp";
            /// <summary>Optional links (slides, repos, papers) the Course Architect should fold into the blueprint.</summary>
            public List<string>? Materials { get; set; }
            /// <summary>Optional cover image URL; when omitted the default IFA artwork is used.</summary>
            public string? CoverImageUrl { get; set; }
            /// <summary>When false, only the blueprint is saved (no Module 1 content); content is generated later, per module, from the course page.</summary>
            public bool GenerateFirstModule { get; set; } = true;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCourses()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var enrollments = await _context.CourseEnrollments
                .Include(e => e.Course)
                .ThenInclude(c => c!.Modules)
                .ThenInclude(m => m.Lessons)
                .Where(e => e.LearnerId == learnerId)
                .OrderByDescending(e => e.LastAccessedAt)
                .ToListAsync();

            // A new learner's library stays empty until they create or enrol in a
            // course of their own. (Previously any learner with zero enrollments was
            // silently auto-enrolled into the seeded "Entrance Exam" course — or had a
            // course auto-generated — which made brand-new accounts show a course they
            // never started.)

            var completedLessonIds = await _context.LessonProgress
                .Where(p => p.LearnerId == learnerId && p.IsCompleted)
                .Select(p => p.LessonId)
                .ToListAsync();

            var courses = enrollments.Select(e => new
            {
                e.Course!.Id,
                e.Course.Title,
                e.Course.Description,
                e.Course.Category,
                e.Course.TargetAudience,
                e.Course.ThumbnailUrl,
                e.Course.ProviderName,
                e.Course.EstimatedDuration,
                e.Course.Rating,
                e.Course.ReviewCount,
                e.Course.IsPublic,
                e.Course.ShareCode,
                e.ProgressPercentage,
                e.LastAccessedAt,
                TotalModules = e.Course.Modules.Count,
                CompletedModules = e.Course.Modules.Count(m => m.Lessons.Count > 0 && m.Lessons.All(l => completedLessonIds.Contains(l.Id))),
                ActiveModuleNumber = e.Course.Modules
                    .OrderBy(m => m.ModuleNumber)
                    .FirstOrDefault(m => !(m.Lessons.Count > 0 && m.Lessons.All(l => completedLessonIds.Contains(l.Id))))?.ModuleNumber
                    ?? e.Course.Modules.Count
            });

            return Ok(courses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseById(Guid id)
        {
            var course = await _context.Courses
                .Include(c => c.Modules.OrderBy(m => m.ModuleNumber))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.LessonNumber))
                .Include(c => c.Modules)
                .ThenInclude(m => m.Quizzes)
                .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound(new { message = "Course not found." });

            return Ok(course);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Goal))
            {
                return BadRequest(new { message = "Goal is required." });
            }

            // Covers are stored inline (the client generates a data URL when the
            // learner asks for an auto cover). Reject anything the column cannot
            // hold instead of letting the insert fail with a 500.
            if (request.CoverImageUrl is { Length: > MaxCoverImageUrlLength })
            {
                return BadRequest(new
                {
                    message = $"Cover image is too large (max {MaxCoverImageUrlLength} characters). Use an image URL instead of an inline image."
                });
            }

            var learnerId = await GetCurrentLearnerIdAsync(_context);

            Course course;
            try
            {
                course = await _generationService.CreateFullCourseAsync(
                    learnerId,
                    request.Goal.Trim(),
                    request.HoursPerWeek > 0 ? request.HoursPerWeek : 5,
                    string.IsNullOrWhiteSpace(request.PreferredCreator) ? "freeCodeCamp" : request.PreferredCreator.Trim(),
                    ct: HttpContext.RequestAborted,
                    externalMaterials: request.Materials,
                    coverImageUrl: request.CoverImageUrl,
                    generateFirstModule: request.GenerateFirstModule);
            }
            catch (InvalidOperationException ex)
            {
                // The AI provider returned content that failed validation, or no
                // provider was reachable and the offline fallback also failed.
                return UnprocessableEntity(new { message = $"Could not generate the course: {ex.Message}" });
            }

            await _activityService.RecordActivityAsync(
                learnerId,
                "course_started",
                $"Started course: {course.Title}",
                $"Created AI curriculum tailored for: {request.Goal}");

            return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, course);
        }

        [HttpPost("{id}/publish")]
        public async Task<IActionResult> PublishCourse(Guid id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            course.IsPublic = !course.IsPublic;
            await _context.SaveChangesAsync();

            var learnerId = await GetCurrentLearnerIdAsync(_context);
            if (course.IsPublic)
            {
                await _activityService.RecordActivityAsync(
                    learnerId,
                    "course_published",
                    $"Published course to marketplace",
                    $"Course '{course.Title}' is now public.");
            }

            return Ok(new { isPublic = course.IsPublic });
        }

        [HttpGet("marketplace")]
        public async Task<IActionResult> GetMarketplace([FromQuery] string? category, [FromQuery] string? search)
        {
            var query = _context.Courses
                .Include(c => c.Modules)
                .Where(c => c.IsPublic);

            if (!string.IsNullOrWhiteSpace(category) && category != "all")
            {
                query = query.Where(c => c.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(s) || c.Description.ToLower().Contains(s));
            }

            var results = await query
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Rating)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Category,
                    c.TargetAudience,
                    c.ThumbnailUrl,
                    c.ProviderName,
                    c.Rating,
                    c.ReviewCount,
                    c.EstimatedDuration,
                    c.Badge,
                    ModuleCount = c.Modules.Count,
                    c.ShareCode
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareCourse(Guid id)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var result = await _sharingService.ShareCourseAsync(new ShareCourseCommand
            {
                CourseId = id,
                SharedByLearnerId = learnerId
            });

            await _activityService.RecordActivityAsync(
                learnerId,
                "course_shared",
                "Shared course invite",
                $"Generated share invite code: {result.ShareCode}");

            return Ok(result);
        }

        [HttpGet("join/{code}")]
        public async Task<IActionResult> PreviewJoinCourse(string code)
        {
            var course = await _context.Courses
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(c => c.ShareCode == code);

            if (course == null)
            {
                var invite = await _context.CourseShareInvites
                    .Include(i => i.Course)
                    .ThenInclude(c => c!.Modules)
                    .FirstOrDefaultAsync(i => i.ShareCode == code
                        && !i.IsAccepted
                        && i.CreatedAt >= DateTime.UtcNow.AddDays(-7));

                course = invite?.Course;
            }

            if (course == null) return NotFound(new { message = "Invalid share code." });

            return Ok(new
            {
                course.Id,
                course.Title,
                course.Description,
                course.Category,
                course.EstimatedDuration,
                ModuleCount = course.Modules.Count,
                course.ProviderName
            });
        }

        [HttpPost("join/{code}")]
        public async Task<IActionResult> JoinCourse(string code)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var success = await _sharingService.EnrollViaShareCodeAsync(code, learnerId);
            if (!success) return BadRequest(new { message = "Invalid or expired share code." });

            var course = await _context.Courses
                .FirstOrDefaultAsync(item => item.ShareCode == code && item.IsPublic);
            course ??= await _context.CourseShareInvites
                .Where(invite => invite.ShareCode == code)
                .Select(invite => invite.Course)
                .FirstOrDefaultAsync();
            return Ok(new { success = true, courseId = course?.Id });
        }

        [HttpGet("{id}/peer-comparison")]
        public async Task<IActionResult> GetPeerComparison(Guid id)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var enrollments = await _context.CourseEnrollments
                .Where(e => e.CourseId == id)
                .ToListAsync();

            var current = enrollments.FirstOrDefault(e => e.LearnerId == learnerId);
            var userProgress = current?.ProgressPercentage ?? 42;

            return Ok(new
            {
                courseId = id,
                userProgress = userProgress,
                peerAverage = enrollments.Any() ? (int)enrollments.Average(e => e.ProgressPercentage) : 45,
                topPercentile = 78,
                totalEnrolled = Math.Max(enrollments.Count, 128),
                rank = 14,
                paceStatus = userProgress >= 45 ? "Ahead of peers" : "On track"
            });
        }
    }
}
