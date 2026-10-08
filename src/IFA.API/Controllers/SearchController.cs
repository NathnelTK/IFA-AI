using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new { courses = new List<object>(), lessons = new List<object>() });
            }

            var clean = q.Trim().ToLower();

            var courses = await _context.Courses
                .Where(c => c.Title.ToLower().Contains(clean) || c.Description.ToLower().Contains(clean))
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Category,
                    Type = "Course"
                })
                .Take(5)
                .ToListAsync();

            var lessons = await _context.Lessons
                .Include(l => l.Module)
                .Where(l => l.Title.ToLower().Contains(clean) || l.Summary.ToLower().Contains(clean))
                .Select(l => new
                {
                    l.Id,
                    CourseId = l.Module!.CourseId,
                    l.Title,
                    l.Summary,
                    Type = "Lesson"
                })
                .Take(5)
                .ToListAsync();

            return Ok(new { courses, lessons });
        }
    }
}
