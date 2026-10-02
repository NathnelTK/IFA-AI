using System;
using System.Collections.Generic;
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
        public async Task<IActionResult> SendIntakeMessage([FromBody] IntakeMessageRequest request)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var response = await _intakeService.ProcessMessageAsync(learnerId, request.Message, request.History);
            return Ok(response);
        }

        public class ProposeCourseRequest
        {
            public string Goal { get; set; } = string.Empty;
            public int HoursPerWeek { get; set; } = 5;
            public string PreferredCreator { get; set; } = "freeCodeCamp";
        }

        [HttpPost("course/propose")]
        public async Task<IActionResult> ProposeCourse([FromBody] ProposeCourseRequest request)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var profile = await _context.LearnerProfiles
                .FirstOrDefaultAsync(p => p.LearnerId == learnerId);

            profile ??= new LearnerProfile
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                LearningGoal = request.Goal,
                Subject = request.Goal,
                WeeklyStudyHours = request.HoursPerWeek > 0 ? request.HoursPerWeek : 5
            };

            var proposal = await _generationService.GenerateCoursePipelineProposalAsync(profile, null);

            return Ok(proposal);
        }

        [HttpPost("module/generate")]
        public async Task<IActionResult> GenerateModule([FromBody] JitModuleGenerationRequest request)
        {
            var result = await _generationService.GenerateJitModuleAsync(request);
            return Ok(result);
        }

        [HttpPost("tutor/chat")]
        public async Task<IActionResult> TutorChat([FromBody] TutorMessageRequest request)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var response = await _tutorService.ChatAsync(learnerId, request);
            return Ok(response);
        }
    }
}
