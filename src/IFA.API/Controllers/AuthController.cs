using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(IApplicationDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public class RegisterRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var existing = await _context.Learners.FirstOrDefaultAsync(l => l.Email == cleanEmail);
            if (existing != null)
            {
                return Conflict(new { message = "User with this email already exists." });
            }

            var learner = new Learner
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(request.Name) ? cleanEmail.Split('@')[0] : request.Name.Trim(),
                Email = cleanEmail,
                PasswordHash = HashPassword(request.Password),
                Role = "Learner",
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&h=100&fit=crop&crop=faces",
                OverallProgress = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Learners.Add(learner);

            // Initialize learner profile & state
            var profile = new LearnerProfile
            {
                Id = Guid.NewGuid(),
                LearnerId = learner.Id,
                LearningGoal = "Master Software Engineering and Clean Architecture",
                Subject = "Software Engineering",
                CurrentLevel = "Intermediate",
                TargetOutcome = "Exit Exam Ready",
                WeeklyStudyHours = 6,
                UpdatedAt = DateTime.UtcNow
            };
            _context.LearnerProfiles.Add(profile);

            var state = new LearnerState
            {
                Id = Guid.NewGuid(),
                LearnerId = learner.Id,
                ActiveStreakDays = 1,
                UpdatedAt = DateTime.UtcNow
            };
            _context.LearnerStates.Add(state);

            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(learner);
            return Ok(token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var cleanEmail = request.Email?.Trim().ToLowerInvariant();
            var learner = await _context.Learners.FirstOrDefaultAsync(l => l.Email == cleanEmail);

            if (learner == null || (learner.PasswordHash != null && learner.PasswordHash != HashPassword(request.Password)))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var token = _tokenService.GenerateToken(learner);
            return Ok(token);
        }

        [HttpPost("demo-login")]
        public async Task<IActionResult> DemoLogin()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var learner = await _context.Learners.FirstAsync(l => l.Id == learnerId);

            var token = _tokenService.GenerateToken(learner);
            return Ok(token);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var learner = await _context.Learners
                .Include(l => l.Profile)
                .Include(l => l.State)
                .Include(l => l.Settings)
                .FirstOrDefaultAsync(l => l.Id == learnerId);

            if (learner == null) return NotFound();

            return Ok(new
            {
                learner.Id,
                learner.Name,
                learner.Email,
                learner.Role,
                learner.AvatarUrl,
                learner.OverallProgress,
                learner.Profile,
                learner.State,
                learner.Settings
            });
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("IFA_SALT_" + password));
            return Convert.ToBase64String(bytes);
        }
    }
}
