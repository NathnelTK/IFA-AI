using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [EnableRateLimiting("generation")]
    [Route("api/[controller]")]
    public class AiController : BaseApiController
    {
        private readonly IIntakeService _intakeService;
        private readonly CourseGenerationService _generationService;
        private readonly IAiTutorService _tutorService;
        private readonly IApplicationDbContext _context;

        public AiController(
            IIntakeService intakeService,
            CourseGenerationService generationService,
            IAiTutorService tutorService,
            IApplicationDbContext context)
        {
            _intakeService = intakeService;
            _generationService = generationService;
            _tutorService = tutorService;
            _context = context;
        }

        public class IntakeMessageRequest
        {
            public string Message { get; set; } = string.Empty;
            public List<IntakeMessageDto> History { get; set; } = new List<IntakeMessageDto>();
        }

        [HttpPost("intake/message")]
        public async Task<IActionResult> SendIntakeMessage([FromBody] IntakeMessageRequest request, CancellationToken ct)
        {
            if (!IsCurrentUserInRole("Educator"))
            {
                return StatusCode(403, new { message = "Educator role required for course-creation assistance." });
            }

            if (!IsValidConversation(request.Message, request.History))
                return BadRequest(new { message = "Provide a message up to 8000 characters and at most 40 user/assistant history entries (40000 characters total)." });
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var response = await _intakeService.ProcessMessageAsync(learnerId, request.Message, request.History, ct);
            return Ok(response);
        }

        public class ProposeCourseRequest
        {
            public string Goal { get; set; } = string.Empty;
            public int HoursPerWeek { get; set; } = 5;
            public string PreferredCreator { get; set; } = "freeCodeCamp";
            /// <summary>Optional learner-supplied links the architect should incorporate.</summary>
            public List<string>? Materials { get; set; }
        }

        [HttpPost("course/propose")]
        public async Task<IActionResult> ProposeCourse([FromBody] ProposeCourseRequest request, CancellationToken ct)
        {
            if (!IsCurrentUserInRole("Educator"))
            {
                return StatusCode(403, new { message = "Educator role required to generate course blueprints." });
            }

            if (string.IsNullOrWhiteSpace(request.Goal) || request.Goal.Length > 4000 ||
                request.HoursPerWeek is < 1 or > 168 || !AreValidMaterials(request.Materials))
                return BadRequest(new { message = "Provide a goal up to 4000 characters, 1-168 weekly hours and at most 20 valid HTTP(S) reference links." });
            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var profile = await _context.LearnerProfiles
                .AsNoTracking().FirstOrDefaultAsync(p => p.LearnerId == learnerId, ct);

            if (profile is null)
            {
                profile = new LearnerProfile
                {
                    Id = Guid.NewGuid(),
                    LearnerId = learnerId,
                    LearningGoal = request.Goal,
                    Subject = request.Goal,
                    WeeklyStudyHours = request.HoursPerWeek > 0 ? request.HoursPerWeek : 5
                };
            }
            else
            {
                // The learner just described a NEW goal in the chat; the stored
                // profile must not override it.
                profile.LearningGoal = request.Goal;
                profile.Subject = request.Goal;
                if (request.HoursPerWeek > 0) profile.WeeklyStudyHours = request.HoursPerWeek;
            }

            var proposal = await _generationService.GenerateCoursePipelineProposalAsync(
                profile,
                null,
                ct,
                request.Materials);

            return Ok(proposal);
        }

        [HttpPost("module/generate")]
        public async Task<IActionResult> GenerateModule([FromBody] JitModuleGenerationRequest request, CancellationToken ct)
        {
            if (!IsCurrentUserInRole("Educator"))
                return StatusCode(403, new { message = "Educator role required for course generation." });
            if (string.IsNullOrWhiteSpace(request.CourseTitle) || request.CourseTitle.Length > 200 ||
                string.IsNullOrWhiteSpace(request.ModuleTitle) || request.ModuleTitle.Length > 200 ||
                request.ModuleNumber is < 1 or > 50 || request.TargetGoal?.Length > 4000 ||
                request.ResearchContext?.Length > 80000 || !AreValidMaterials(request.ExternalMaterials) ||
                request.KeyTopics?.Count > 8 || request.KeyTopics?.Any(t => string.IsNullOrWhiteSpace(t) || t.Length > 200) == true)
                return BadRequest(new { message = "Provide bounded course/module details, up to 8 topics and up to 20 reference links." });
            request.ResearchContext ??= string.Empty;
            var result = await _generationService.GenerateJitModuleAsync(request, ct);
            return Ok(result);
        }

        [HttpPost("tutor/chat")]
        public async Task<IActionResult> TutorChat([FromBody] TutorMessageRequest request, CancellationToken ct)
        {
            if (!IsValidConversation(request.Message, request.History))
                return BadRequest(new { message = "Provide a message up to 8000 characters and bounded user/assistant history." });
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var courseId = request.CourseId;
            if (request.LessonId.HasValue)
            {
                var lesson = await _context.Lessons.AsNoTracking()
                    .Where(l => l.Id == request.LessonId.Value)
                    .Select(l => new { l.ModuleId, l.Module!.CourseId })
                    .FirstOrDefaultAsync(ct);
                if (lesson == null) return NotFound(new { message = "Lesson not found." });
                if ((courseId.HasValue && courseId != lesson.CourseId) ||
                    (request.ModuleId.HasValue && request.ModuleId != lesson.ModuleId))
                    return BadRequest(new { message = "Tutor course, module and lesson must refer to the same course." });
                courseId = lesson.CourseId;
            }
            if (request.ModuleId.HasValue)
            {
                var module = await _context.Modules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.ModuleId.Value, ct);
                if (module == null) return NotFound(new { message = "Module not found." });
                if (courseId.HasValue && courseId != module.CourseId)
                    return BadRequest(new { message = "Tutor course and module must refer to the same course." });
                courseId = module.CourseId;
            }
            if (courseId.HasValue)
            {
                var access = await CourseAccess.GetAsync(_context, courseId.Value, learnerId, ct);
                access.RequireContent();
                if (!access.Course.CourseAssistantEnabled)
                {
                    return StatusCode(403, new
                    {
                        code = "course_assistant_disabled",
                        message = "The educator disabled Course Assistant for this course."
                    });
                }
                request.CourseId = courseId;
            }
            var response = await _tutorService.ChatAsync(learnerId, request, ct);
            return Ok(response);
        }

        private static bool IsValidConversation(string? message, List<IntakeMessageDto>? history) =>
            !string.IsNullOrWhiteSpace(message) && message.Length <= 8000 &&
            (history == null || (history.Count <= 40 &&
                history.All(h => h != null && h.Role is "user" or "assistant" &&
                    !string.IsNullOrWhiteSpace(h.Content) && h.Content.Length <= 8000) &&
                history.Sum(h => h.Content.Length) <= 40000));

        private static bool AreValidMaterials(List<string>? materials) =>
            materials == null || (materials.Count <= 20 && materials.All(m =>
                !string.IsNullOrWhiteSpace(m) && m.Length <= 2048 &&
                Uri.TryCreate(m, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" &&
                string.IsNullOrEmpty(uri.UserInfo)));
    }
}
