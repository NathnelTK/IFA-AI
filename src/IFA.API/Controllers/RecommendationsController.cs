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
                .OrderBy(s => s.CompetencyPercentage)
                .Select(s => new { s.Id, s.SkillName, s.CompetencyPercentage, s.LastAssessedAt })
                .ToListAsync();

            var list = new List<object>();

            // Without an assessment-derived gap there is no evidence for a
            // personalized recommendation yet.
            if (!weakSkills.Any())
            {
                return Ok(list);
            }

            var publicCourses = await _context.Courses
                .Where(c => c.IsPublished && c.Visibility == "Public")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            var addedCourseIds = new HashSet<Guid>();
            foreach (var weak in weakSkills.Take(5))
            {
                var evidence = $"Saved assessment evidence records {weak.CompetencyPercentage}% mastery in {weak.SkillName} ({weak.LastAssessedAt:yyyy-MM-dd}).";
                list.Add(new
                {
                    weak.Id,
                    Title = $"Review {weak.SkillName}",
                    Reason = evidence,
                    Category = "Learning gap",
                    EstimatedDuration = (string?)null,
                    ConfidenceScore = (double?)null,
                    ActionType = "practice",
                    Topic = weak.SkillName,
                    EvidenceType = "Persisted assessment result"
                });

                var term = weak.SkillName.Trim();
                foreach (var course in publicCourses.Where(c =>
                    c.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Category.Contains(term, StringComparison.OrdinalIgnoreCase)).Take(2))
                {
                    if (!addedCourseIds.Add(course.Id)) continue;
                    list.Add(new
                    {
                        course.Id,
                        course.Title,
                        Reason = $"This published course explicitly matches the identified {weak.SkillName} gap. {evidence}",
                        course.Category,
                        course.EstimatedDuration,
                        ConfidenceScore = (double?)null,
                        ActionType = "view_course",
                        CourseId = course.Id,
                        EvidenceType = "Course metadata and persisted assessment result"
                    });
                }
            }

            return Ok(list);
        }
    }
}
