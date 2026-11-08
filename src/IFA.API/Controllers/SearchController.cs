using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : BaseApiController
    {
        private readonly IApplicationDbContext _context;

        public SearchController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new { courses = new List<object>(), lessons = new List<object>() });
            }

            var clean = q.Trim().ToLowerInvariant();
            if (clean.Length > 200) return BadRequest(new { message = "Search query is limited to 200 characters." });
            var hasLearner = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var learnerId);

            var courses = await _context.Courses
                .Where(c =>
                    ((c.IsPublished && c.Visibility == "Public") ||
                     (hasLearner && (c.CreatorLearnerId == learnerId ||
                        (c.IsPublished && c.Enrollments.Any(e => e.LearnerId == learnerId && e.Status == "Active"))))) &&
                    (c.Title.ToLower().Contains(clean) || c.Description.ToLower().Contains(clean)))
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Category,
                    Type = "Course"
                })
                .Take(5)
                .ToListAsync(ct);

            var lessons = await _context.Lessons
                .Include(l => l.Module)
                .Where(l =>
                    ((l.Module!.Course!.IsPublished && l.Module.Course.Visibility == "Public") ||
                     (hasLearner && (l.Module.Course.CreatorLearnerId == learnerId ||
                        (l.Module.Course.IsPublished && l.Module.Course.Enrollments.Any(e => e.LearnerId == learnerId && e.Status == "Active"))))) &&
                    (l.Title.ToLower().Contains(clean) || l.Summary.ToLower().Contains(clean)))
                .Select(l => new
                {
                    l.Id,
                    CourseId = l.Module!.CourseId,
                    l.Title,
                    l.Summary,
                    Type = "Lesson"
                })
                .Take(5)
                .ToListAsync(ct);

            return Ok(new { courses, lessons });
        }
    }
}
