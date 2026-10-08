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
    [Route("api/[controller]")]
    public class SettingsController : BaseApiController
    {
        private readonly IApplicationDbContext _context;

        public SettingsController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var settings = await _context.LearnerSettings.FirstOrDefaultAsync(s => s.LearnerId == learnerId);

            if (settings == null)
            {
                settings = new LearnerSettings
                {
                    Id = Guid.NewGuid(),
                    LearnerId = learnerId,
                    DailyReminders = true,
                    WeeklyDigest = true,
                    AssessmentResults = true,
                    CourseRecommendations = true,
                    Theme = "system",
                    AccentColor = "pine",
                    FontSize = "medium"
                };
                _context.Add(settings);
                await _context.SaveChangesAsync();
            }

            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] LearnerSettings updated)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var settings = await _context.LearnerSettings.FirstOrDefaultAsync(s => s.LearnerId == learnerId);

            if (settings == null)
            {
                settings = new LearnerSettings
                {
                    Id = Guid.NewGuid(),
                    LearnerId = learnerId
                };
                _context.Add(settings);
            }

            settings.DailyReminders = updated.DailyReminders;
            settings.WeeklyDigest = updated.WeeklyDigest;
            settings.AssessmentResults = updated.AssessmentResults;
            settings.CourseRecommendations = updated.CourseRecommendations;
            settings.Theme = updated.Theme ?? settings.Theme;
            settings.AccentColor = updated.AccentColor ?? settings.AccentColor;
            settings.FontSize = updated.FontSize ?? settings.FontSize;
            settings.CompactMode = updated.CompactMode;
            settings.ReducedMotion = updated.ReducedMotion;
            settings.PreferredYouTubeChannelsJson = updated.PreferredYouTubeChannelsJson ?? settings.PreferredYouTubeChannelsJson;
            settings.ExcludedYouTubeChannelsJson = updated.ExcludedYouTubeChannelsJson ?? settings.ExcludedYouTubeChannelsJson;
            settings.PreferredTopicsJson = updated.PreferredTopicsJson ?? settings.PreferredTopicsJson;

            await _context.SaveChangesAsync();
            return Ok(settings);
        }
    }
}
