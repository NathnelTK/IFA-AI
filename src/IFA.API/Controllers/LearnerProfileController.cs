using System;
using System.Text.Json;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/learners/me")]
    public class LearnerProfileController : BaseApiController
    {
        private readonly IApplicationDbContext _context;

        public LearnerProfileController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var profile = await _context.LearnerProfiles.FirstOrDefaultAsync(p => p.LearnerId == learnerId);

            if (profile == null)
            {
                profile = new LearnerProfile
                {
                    Id = Guid.NewGuid(),
                    LearnerId = learnerId,
                    LearningGoal = "Master Software Engineering & Clean Architecture",
                    Subject = "Software Engineering",
                    CurrentLevel = "Intermediate",
                    TargetOutcome = "Exit Exam Ready",
                    WeeklyStudyHours = 6,
                    PreferredLanguage = "en",
                    LearningStyle = "Hands-on",
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Add(profile);
                await _context.SaveChangesAsync();
            }

            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] LearnerProfile updated)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var profile = await _context.LearnerProfiles.FirstOrDefaultAsync(p => p.LearnerId == learnerId);

            if (profile == null)
            {
                profile = new LearnerProfile
                {
                    Id = Guid.NewGuid(),
                    LearnerId = learnerId
                };
                _context.Add(profile);
            }

            profile.LearningGoal = updated.LearningGoal ?? profile.LearningGoal;
            profile.Subject = updated.Subject ?? profile.Subject;
            profile.CurrentLevel = updated.CurrentLevel ?? profile.CurrentLevel;
            profile.TargetOutcome = updated.TargetOutcome ?? profile.TargetOutcome;
            profile.WeeklyStudyHours = updated.WeeklyStudyHours > 0 ? updated.WeeklyStudyHours : profile.WeeklyStudyHours;
            profile.PreferredLanguage = updated.PreferredLanguage ?? profile.PreferredLanguage;
            profile.LearningStyle = updated.LearningStyle ?? profile.LearningStyle;
            profile.Constraints = updated.Constraints ?? profile.Constraints;
            profile.PreferredYouTubeChannelsJson = updated.PreferredYouTubeChannelsJson ?? profile.PreferredYouTubeChannelsJson;
            profile.KnownStrengthsJson = updated.KnownStrengthsJson ?? profile.KnownStrengthsJson;
            profile.KnownWeaknessesJson = updated.KnownWeaknessesJson ?? profile.KnownWeaknessesJson;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(profile);
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetState()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var state = await _context.LearnerStates.FirstOrDefaultAsync(s => s.LearnerId == learnerId);

            if (state == null)
            {
                state = new LearnerState
                {
                    Id = Guid.NewGuid(),
                    LearnerId = learnerId,
                    ActiveStreakDays = 3,
                    TotalHoursLearned = 14.5,
                    CompletedLessonsCount = 8,
                    CompletedQuizzesCount = 3,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Add(state);
                await _context.SaveChangesAsync();
            }

            return Ok(state);
        }
    }
}
