using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Application.Common.Services;
using IFA.Application.Courses.Commands;
using IFA.Application.Courses.Services;
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
                .Where(e => e.LearnerId == learnerId && (e.Course!.IsPublished || e.Course.CreatorLearnerId == learnerId))
                .OrderByDescending(e => e.LastAccessedAt)
                .ToListAsync();

            var courses = new List<object>();
            foreach (var e in enrollments)
            {
                var course = e.Course!;
                var isOwner = course.CreatorLearnerId == learnerId;
                var progress = await CourseProgress.GetAsync(_context, course, isOwner ? null : learnerId, HttpContext.RequestAborted);
                courses.Add(new
                {
                    course.Id,
                    course.Title,
                    course.Description,
                    course.Category,
                    course.TargetAudience,
                    course.ThumbnailUrl,
                    course.ProviderName,
                    course.EstimatedDuration,
                    course.Rating,
                    course.ReviewCount,
                    course.IsPublic,
                    course.IsPublished,
                    course.Visibility,
                    IsOwner = isOwner,
                    HasContentAccess = isOwner || (course.IsPublished && e.Status == "Active"),
                    EnrollmentStatus = e.Status,
                    ProgressPercentage = progress.Percentage,
                    progress.IsCompleted,
                    e.LastAccessedAt,
                    progress.TotalModules,
                    progress.CompletedModules,
                    ActiveModuleNumber = course.Modules.OrderBy(m => m.ModuleNumber)
                        .FirstOrDefault(m => !progress.IsModuleCompleted(m.Id))?.ModuleNumber ?? course.Modules.Count
                });
            }

            var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
            var ownedCourses = await _context.Courses
                .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
                .Where(c => c.CreatorLearnerId == learnerId && !enrolledCourseIds.Contains(c.Id))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            foreach (var course in ownedCourses)
            {
                var progress = await CourseProgress.GetAsync(_context, course, null, HttpContext.RequestAborted);
                courses.Add(new
                {
                    course.Id, course.Title, course.Description, course.Category, course.TargetAudience,
                    course.ThumbnailUrl, course.ProviderName, course.EstimatedDuration, course.Rating,
                    course.ReviewCount, course.IsPublic, course.IsPublished, course.Visibility,
                    IsOwner = true,
                    HasContentAccess = true,
                    EnrollmentStatus = "Owner",
                    ProgressPercentage = 0,
                    IsCompleted = false,
                    LastAccessedAt = course.CreatedAt,
                    progress.TotalModules,
                    CompletedModules = 0,
                    ActiveModuleNumber = course.Modules.OrderBy(m => m.ModuleNumber).FirstOrDefault()?.ModuleNumber ?? 0
                });
            }

            return Ok(courses);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourseById(Guid id)
        {
            var course = await _context.Courses
                .Include(c => c.Modules.OrderBy(m => m.ModuleNumber))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.LessonNumber))
                .Include(c => c.Modules)
                .ThenInclude(m => m.Quizzes)
                .ThenInclude(q => q.Questions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound(new { message = "Course not found." });

            var learnerId = await GetOptionalLearnerIdAsync(_context);
            var access = await CourseAccess.GetAsync(_context, id, learnerId, HttpContext.RequestAborted);
            access.RequireMetadata();
            var progress = await CourseProgress.GetAsync(_context, course,
                access.IsOwner || access.Enrollment == null ? null : learnerId, HttpContext.RequestAborted);

            return Ok(new
            {
                course.Id,
                course.Title,
                course.Description,
                course.Category,
                course.TargetAudience,
                course.ThumbnailUrl,
                course.ProviderName,
                course.ProviderLogo,
                course.Badge,
                course.Rating,
                course.ReviewCount,
                course.EstimatedDuration,
                course.IsPublic,
                course.IsPublished,
                course.Visibility,
                course.Level,
                course.Language,
                course.LearningObjectives,
                course.RequirePassingQuizzes,
                course.EnrollmentLimit,
                course.EnrollmentOpensAt,
                course.EnrollmentClosesAt,
                ShareCode = access.IsOwner ? course.ShareCode : null,
                ExternalMaterials = access.HasContentAccess ? course.ExternalMaterials : null,
                course.CreatorLearnerId,
                course.CreatedAt,
                course.IsPaid,
                course.PriceAmount,
                course.Currency,
                course.RequiresApproval,
                course.AccentColor,
                course.ThemeMode,
                course.EnrollmentMode,
                course.PassingScorePercentage,
                course.MaxQuizAttempts,
                course.Prerequisites,
                course.InstructorBio,
                course.InstructorTitle,
                course.CoverImageUrl,
                course.CoverImageSource,
                access.IsOwner,
                access.HasContentAccess,
                EnrollmentStatus = access.Enrollment?.Status,
                ProgressPercentage = progress.Percentage,
                progress.IsCompleted,
                Progress = progress,
                Modules = course.Modules.OrderBy(m => m.ModuleNumber).Select(m => new
                {
                    m.Id,
                    m.CourseId,
                    m.ModuleNumber,
                    m.Title,
                    Summary = access.HasContentAccess ? m.Summary : string.Empty,
                    m.EstimatedHours,
                    m.GenerationStatus,
                    m.GeneratedAt,
                    IsCompleted = progress.IsModuleCompleted(m.Id),
                    Lessons = m.Lessons.OrderBy(l => l.LessonNumber).Select(l => CourseProgress.LessonDto(l, access.HasContentAccess, progress)),
                    Quizzes = access.HasContentAccess
                        ? m.Quizzes.OrderBy(q => q.OrderIndex).Select(q => CourseProgress.QuizDto(q, course, access.IsOwner, progress))
                        : Enumerable.Empty<object>()
                })
            });
        }

        [HttpPost("{id}/enroll")]
        public async Task<IActionResult> EnrollCourse(Guid id)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var enrollment = await CourseAccess.EnrollAsync(_context, id, learnerId, ct: HttpContext.RequestAborted);
            return Ok(new { courseId = enrollment.CourseId, status = enrollment.Status, paymentRequired = enrollment.Status == "PendingPayment", success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
        {
            if (!IsCurrentUserInRole("Educator"))
            {
                return StatusCode(403, new { message = "Educator role required to create courses." });
            }

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
            catch (InvalidOperationException ex) when (ex is not IFA.Application.Common.Exceptions.AiUnavailableException)
            {
                // Invalid generated content is distinct from provider unavailability (503).
                return UnprocessableEntity(new { message = $"Could not generate the course: {ex.Message}" });
            }

            await _activityService.RecordActivityAsync(
                learnerId,
                "course_created",
                $"Created course: {course.Title}",
                $"Generated an educator course draft for: {request.Goal}");

            return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, course);
        }

        [HttpPost("{id}/publish")]
        public async Task<IActionResult> PublishCourse(Guid id)
        {
            // Publishing is educator-only now: learners can no longer flip the
            // public flag on their personal AI-generated courses.
            if (!IsCurrentUserInRole("Educator"))
            {
                return StatusCode(403, new { message = "Educator role required to publish courses." });
            }

            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var course = await _context.Courses.Include(c => c.Modules).ThenInclude(m => m.Lessons)
                .Include(c => c.Modules).ThenInclude(m => m.Quizzes).ThenInclude(q => q.Questions)
                .Include(c => c.Assignments)
                .AsSplitQuery().FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();
            if (course.CreatorLearnerId != learnerId)
            {
                return StatusCode(403, new { message = "You do not own this course." });
            }

            if (course.IsPublished) return Ok(new { course.IsPublic, course.IsPublished, course.Visibility });
            if (course.IsPaid) return Conflict(new { message = "Publish paid courses through Studio so price and payment readiness can be validated." });
            var errors = CoursePublication.Validate(course);
            if (errors.Count > 0) return BadRequest(new { message = "Resolve course validation errors before publishing.", errors });
            if (await _context.GenerationJobs.AnyAsync(j => j.CourseId == id && (j.Status == "Pending" || j.Status == "Queued" || j.Status == "Running")))
                return Conflict(new { message = "Wait for active generation jobs before publishing." });
            course.IsPublished = true;
            course.IsPublic = course.Visibility == "Public";
            var strandedPayments = await _context.CourseEnrollments
                .Where(e => e.CourseId == id && e.Status == "PendingPayment")
                .ToListAsync();
            foreach (var enrollment in strandedPayments) enrollment.Status = "PendingApproval";
            var projects = await _context.EducatorProjects.Where(p => p.CourseId == id && p.EducatorLearnerId == learnerId).ToListAsync();
            foreach (var project in projects)
            {
                project.Status = "Published";
                project.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            await _activityService.RecordActivityAsync(
                learnerId,
                "course_published",
                "Published course",
                $"Course '{course.Title}' is now published ({course.Visibility}).");

            return Ok(new { course.IsPublic, course.IsPublished, course.Visibility });
        }

        [HttpGet("marketplace")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMarketplace([FromQuery] string? category, [FromQuery] string? search, CancellationToken ct)
        {
            var viewerId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId) ? (Guid?)currentId : null;
            var query = _context.Courses
                .Include(c => c.Modules)
                .Where(c => c.IsPublished && c.Visibility == "Public");

            if (!string.IsNullOrWhiteSpace(category) && category != "all")
            {
                query = query.Where(c => c.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                if (search.Length > 200) return BadRequest(new { message = "Search query is limited to 200 characters." });
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
                    c.CoverImageUrl,
                    c.AccentColor,
                    c.ThemeMode,
                    c.ProviderName,
                    c.Rating,
                    c.ReviewCount,
                    c.EstimatedDuration,
                    c.Badge,
                    c.IsPublished,
                    c.Visibility,
                    c.Level,
                    c.Language,
                    c.IsPaid,
                    c.PriceAmount,
                    c.Currency,
                    c.EnrollmentMode,
                    c.EnrollmentLimit,
                    c.EnrollmentOpensAt,
                    c.EnrollmentClosesAt,
                     ModuleCount = c.Modules.Count,
                     c.ShareCode,
                     EnrollmentCount = c.Enrollments.Count(e => e.Status == "Active" && e.LearnerId != c.CreatorLearnerId),
                     IsOwner = viewerId.HasValue && c.CreatorLearnerId == viewerId.Value,
                     EnrollmentStatus = viewerId.HasValue
                         ? c.Enrollments.Where(e => e.LearnerId == viewerId.Value).Select(e => e.Status).FirstOrDefault()
                         : null,
                     HasContentAccess = viewerId.HasValue && (c.CreatorLearnerId == viewerId.Value ||
                         c.Enrollments.Any(e => e.LearnerId == viewerId.Value && e.Status == "Active"))
                 })
                 .ToListAsync(ct);

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
        [AllowAnonymous]
        public async Task<IActionResult> PreviewJoinCourse(string code)
        {
            var (course, invite) = await CourseAccess.ResolveShareAsync(_context, code, HttpContext.RequestAborted);
            if (invite?.IsAccepted == true) return NotFound(new { message = "This invitation has already been used." });
            var moduleCount = await _context.Modules.CountAsync(m => m.CourseId == course.Id);

            return Ok(new
            {
                course.Id,
                course.Title,
                course.Description,
                course.Category,
                course.EstimatedDuration,
                ModuleCount = moduleCount,
                course.ProviderName,
                course.IsPaid,
                course.PriceAmount,
                course.Currency,
                course.Visibility,
                course.EnrollmentMode,
                course.EnrollmentOpensAt,
                course.EnrollmentClosesAt,
                ExpiresAt = invite?.CreatedAt.AddDays(7)
            });
        }

        [HttpPost("join/{code}")]
        public async Task<IActionResult> JoinCourse(string code)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var (course, invite) = await CourseAccess.ResolveShareAsync(_context, code, HttpContext.RequestAborted);
            var enrollment = await CourseAccess.EnrollAsync(_context, course.Id, learnerId, invite?.Id, HttpContext.RequestAborted);
            return Ok(new { success = true, courseId = course.Id, status = enrollment.Status, paymentRequired = enrollment.Status == "PendingPayment" });
        }

        [HttpGet("{id}/peer-comparison")]
        public async Task<IActionResult> GetPeerComparison(Guid id)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var access = await CourseAccess.GetAsync(_context, id, learnerId, HttpContext.RequestAborted);
            access.RequireContent();
            var enrollments = await _context.CourseEnrollments
                .Where(e => e.CourseId == id && e.Status == "Active" && e.LearnerId != access.Course.CreatorLearnerId)
                .ToListAsync();

            var current = enrollments.FirstOrDefault(e => e.LearnerId == learnerId);
            var progressByLearner = new Dictionary<Guid, int>();
            foreach (var enrollment in enrollments)
                progressByLearner[enrollment.LearnerId] = (await CourseProgress.GetAsync(_context, access.Course, enrollment.LearnerId, HttpContext.RequestAborted)).Percentage;
            int? userProgress = current == null ? null : progressByLearner[learnerId];
            var peerProgress = progressByLearner.Where(item => item.Key != learnerId).Select(item => item.Value).ToList();
            double? peerAverage = peerProgress.Count > 0 ? Math.Round(peerProgress.Average(), 1) : null;

            return Ok(new
            {
                courseId = id,
                userProgress = userProgress,
                peerAverage,
                topPercentile = userProgress.HasValue ? (int?)Math.Round(100.0 * progressByLearner.Values.Count(p => p <= userProgress.Value) / enrollments.Count) : null,
                totalEnrolled = enrollments.Count,
                rank = userProgress.HasValue ? (int?)(1 + progressByLearner.Values.Count(p => p > userProgress.Value)) : null,
                paceStatus = !userProgress.HasValue || !peerAverage.HasValue ? "Not enough peer data"
                    : userProgress.Value > peerAverage ? "Ahead of peers" : userProgress.Value < peerAverage ? "Behind peers" : "At peer average"
            });
        }

        // ------------------------------------------------------------------
        // Live Sessions (Learner View)
        // ------------------------------------------------------------------

        [HttpGet("{id}/live-sessions")]
        public async Task<IActionResult> GetCourseLiveSessions(Guid id)
        {
            await RequireCourseContentAsync(id);
            var sessions = await _context.LiveSessions
                .Where(s => s.CourseId == id)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return Ok(sessions.Select(s => new
                {
                    s.Id,
                    s.CourseId,
                    s.Title,
                    s.Description,
                    s.StartTime,
                    s.DurationMinutes,
                    s.TimeZone,
                    s.MeetingUrl,
                    Status = EffectiveSessionStatus(s),
                    s.CreatedAt
                }));
        }

        private static string EffectiveSessionStatus(LiveSession session)
        {
            if (session.Status != "Scheduled") return session.Status;
            var now = DateTime.UtcNow;
            if (session.StartTime <= now && session.StartTime.AddMinutes(session.DurationMinutes) > now) return "Live";
            return session.StartTime.AddMinutes(session.DurationMinutes) <= now ? "Completed" : "Scheduled";
        }

        // ------------------------------------------------------------------
        // Course Discussions / Q&A
        // ------------------------------------------------------------------

        [HttpGet("{id}/discussions")]
        public async Task<IActionResult> GetCourseDiscussions(Guid id, [FromQuery] Guid? lessonId)
        {
            await RequireCourseContentAsync(id);
            if (lessonId.HasValue && !await _context.Lessons.AnyAsync(l => l.Id == lessonId && l.Module!.CourseId == id))
                return BadRequest(new { message = "The lesson does not belong to this course." });
            var query = _context.Discussions
                .Include(d => d.Learner)
                .Include(d => d.Replies)
                .ThenInclude(r => r.Learner)
                .Where(d => d.CourseId == id && d.ParentId == null);

            if (lessonId.HasValue)
            {
                query = query.Where(d => d.LessonId == lessonId.Value);
            }

            var discussions = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.CourseId,
                    d.LessonId,
                    d.Title,
                    d.Content,
                    d.IsInstructorReply,
                    d.CreatedAt,
                    Author = new
                    {
                        Name = d.Learner != null ? d.Learner.Name : "User",
                        AvatarUrl = d.Learner != null ? d.Learner.AvatarUrl : ""
                    },
                    Replies = d.Replies.OrderBy(r => r.CreatedAt).Select(r => new
                    {
                        r.Id,
                        r.Content,
                        r.IsInstructorReply,
                        r.CreatedAt,
                        Author = new
                        {
                            Name = r.Learner != null ? r.Learner.Name : "User",
                            AvatarUrl = r.Learner != null ? r.Learner.AvatarUrl : ""
                        }
                    })
                })
                .ToListAsync();

            return Ok(discussions);
        }

        public class PostDiscussionDto
        {
            public Guid? LessonId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }

        [HttpPost("{id}/discussions")]
        public async Task<IActionResult> PostDiscussion(Guid id, [FromBody] PostDiscussionDto dto)
        {
            var access = await RequireCourseContentAsync(id);
            if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 10000 || dto.Title?.Length > 200)
            {
                return BadRequest(new { message = "Use a discussion title of at most 200 characters and content of 1-10000 characters." });
            }
            if (dto.LessonId.HasValue && !await _context.Lessons.AnyAsync(l => l.Id == dto.LessonId && l.Module!.CourseId == id))
                return BadRequest(new { message = "The lesson does not belong to this course." });

            var discussion = new CourseDiscussion
            {
                CourseId = id,
                LessonId = dto.LessonId,
                LearnerId = access.LearnerId!.Value,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? "Question" : dto.Title.Trim(),
                Content = dto.Content.Trim(),
                IsInstructorReply = access.IsOwner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Add(discussion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                discussion.Id,
                discussion.CourseId,
                discussion.LessonId,
                discussion.Title,
                discussion.Content,
                discussion.IsInstructorReply,
                discussion.CreatedAt
            });
        }

        public class PostReplyDto
        {
            public string Content { get; set; } = string.Empty;
        }

        [HttpPost("{id}/discussions/{discussionId:guid}/reply")]
        public async Task<IActionResult> PostReply(Guid id, Guid discussionId, [FromBody] PostReplyDto dto)
        {
            var access = await RequireCourseContentAsync(id);
            if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 10000)
            {
                return BadRequest(new { message = "Reply content must contain 1-10000 characters." });
            }

            var parent = await _context.Discussions.FirstOrDefaultAsync(d => d.Id == discussionId && d.CourseId == id && d.ParentId == null);
            if (parent == null) return NotFound(new { message = "Discussion thread not found." });

            var reply = new CourseDiscussion
            {
                CourseId = id,
                LessonId = parent.LessonId,
                LearnerId = access.LearnerId!.Value,
                ParentId = discussionId,
                Title = "Reply",
                Content = dto.Content.Trim(),
                IsInstructorReply = access.IsOwner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Add(reply);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                reply.Id,
                reply.ParentId,
                reply.Content,
                reply.IsInstructorReply,
                reply.CreatedAt
            });
        }

        // ------------------------------------------------------------------
        // Course Assignments (Learner View & Submission)
        // ------------------------------------------------------------------

        [HttpGet("{id}/assignments")]
        public async Task<IActionResult> GetCourseAssignments(Guid id)
        {
            var access = await RequireCourseContentAsync(id);
            var learnerId = access.LearnerId!.Value;
            var enrolledAt = access.IsOwner ? (DateTime?)null : access.Enrollment?.EnrolledAt;
            var assignments = await _context.Assignments
                .Include(a => a.Submissions)
                .Where(a => a.CourseId == id)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.CourseId,
                    a.Title,
                    a.InstructionsMarkdown,
                    a.DueDaysAfterEnrollment,
                    a.MaxScore,
                    a.AllowedFileTypes,
                    a.MaxFileSizeMb,
                    a.CreatedAt,
                    DueAt = enrolledAt.HasValue ? (DateTime?)enrolledAt.Value.AddDays(a.DueDaysAfterEnrollment) : null,
                    SubmissionMode = "TextOnly",
                    MySubmission = a.Submissions
                        .Where(s => s.LearnerId == learnerId)
                        .OrderByDescending(s => s.SubmittedAt)
                        .Select(s => new
                        {
                            s.Id,
                            s.SubmissionText,
                            s.FileUrl,
                            s.FileName,
                            s.SubmittedAt,
                            s.Status,
                            s.Score,
                            s.Feedback
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(assignments);
        }

        public class SubmitAssignmentDto
        {
            public string SubmissionText { get; set; } = string.Empty;
            public string? FileUrl { get; set; }
            public string? FileName { get; set; }
        }

        [HttpPost("{id}/assignments/{assignmentId:guid}/submit")]
        public async Task<IActionResult> SubmitAssignment(Guid id, Guid assignmentId, [FromBody] SubmitAssignmentDto dto)
        {
            await using var transaction = await CourseAccess.BeginMutationAsync(_context, HttpContext.RequestAborted);
            var access = await RequireCourseContentAsync(id, learnerMutation: true);
            var learnerId = access.LearnerId!.Value;
            if (string.IsNullOrWhiteSpace(dto.SubmissionText) || dto.SubmissionText.Length > 50000)
                return BadRequest(new { message = "Submission text must contain 1-50000 characters." });
            if (!string.IsNullOrWhiteSpace(dto.FileUrl) || !string.IsNullOrWhiteSpace(dto.FileName))
                return BadRequest(new { message = "File uploads are not supported. Submit text only; external URLs are not verified uploaded files." });
            var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == assignmentId && a.CourseId == id);
            if (assignment == null) return NotFound(new { message = "Assignment not found." });
            if (assignment.DueDaysAfterEnrollment < 0 ||
                (assignment.DueDaysAfterEnrollment > 0 && DateTime.UtcNow > access.Enrollment!.EnrolledAt.AddDays(assignment.DueDaysAfterEnrollment)))
                return Conflict(new { message = "The assignment submission deadline has passed. Contact the educator." });

            var existing = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.LearnerId == learnerId);

            if (existing != null)
            {
                if (existing.Status != "ResubmissionRequested")
                    return Conflict(new { message = existing.Status == "Graded"
                        ? "This assignment is already graded. The educator must request a resubmission before it can be replaced."
                        : "A submission is already awaiting instructor review." });
                existing.SubmissionText = dto.SubmissionText.Trim();
                existing.FileUrl = null;
                existing.FileName = null;
                existing.SubmittedAt = DateTime.UtcNow;
                existing.Status = "Submitted";
                existing.Score = null;
                existing.Feedback = null;
                existing.GradedAt = null;
                existing.GradedByEducatorId = null;
            }
            else
            {
                existing = new AssignmentSubmission
                {
                    AssignmentId = assignmentId,
                    LearnerId = learnerId,
                    SubmissionText = dto.SubmissionText.Trim(),
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Submitted"
                };
                _context.Add(existing);
            }

            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync(HttpContext.RequestAborted);
            return Ok(new
            {
                existing.Id,
                existing.AssignmentId,
                existing.SubmissionText,
                existing.FileUrl,
                existing.FileName,
                existing.SubmittedAt,
                existing.Status,
                existing.Score,
                existing.Feedback
            });
        }

        private async Task<CourseAccessResult> RequireCourseContentAsync(Guid id, bool learnerMutation = false)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var access = await CourseAccess.GetAsync(_context, id, learnerId, HttpContext.RequestAborted);
            access.RequireContent(learnerMutation);
            return access;
        }
    }
}
