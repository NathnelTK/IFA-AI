using System;
using System.Collections.Generic;
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
    public class RecommendationsController : BaseApiController
    {
        private readonly IApplicationDbContext _context;

        public RecommendationsController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendations()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);

            var weakSkills = await _context.SkillMetrics
                .Where(s => s.LearnerId == learnerId && s.IsWeakArea)
                .Select(s => s.SkillName)
                .ToListAsync();

            var hasActivity = await _context.CourseEnrollments
                .AnyAsync(e => e.LearnerId == learnerId);

            var list = new List<object>();

            // A brand-new learner has no weak-skill history and no enrollments, so
            // there is nothing to base a recommendation on. Return an empty list
            // instead of handing everyone the seeded course catalog.
            if (!weakSkills.Any() && !hasActivity)
            {
                return Ok(list);
            }

            if (weakSkills.Any())
            {
                foreach (var weak in weakSkills.Take(2))
                {
                    list.Add(new
                    {
                        Id = Guid.NewGuid(),
                        Title = $"Mastering {weak}: Deep-Dive Remediation",
                        Reason = $"Based on your recent assessment score in {weak}.",
                        Category = "Remediation",
                        EstimatedHours = 4,
                        ConfidenceScore = 0.94,
                        ActionType = "create_course",
                        Topic = weak
                    });
                }
            }

            var courses = await _context.Courses
                .Include(c => c.Modules)
                .Take(6)
                .ToListAsync();

            foreach (var course in courses)
            {
                list.Add(new
                {
                    course.Id,
                    course.Title,
                    Reason = "Trending among peers studying software engineering exit exams.",
                    Category = course.Category,
                    EstimatedHours = 8,
                    ConfidenceScore = 0.88,
                    ActionType = "view_course",
                    CourseId = course.Id
                });
            }

            return Ok(list);
        }
    }
}
