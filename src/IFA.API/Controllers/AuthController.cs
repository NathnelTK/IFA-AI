using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Services;
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
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public AuthController(
            IApplicationDbContext context,
            ITokenService tokenService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _tokenService = tokenService;
            _environment = environment;
            _configuration = configuration;
        }

        private bool DemoAccessEnabled => _environment.IsDevelopment()
            || _configuration.GetValue<bool>("DemoData:Enabled")
            || string.Equals(_configuration["DEMO_DATA_ENABLED"], "true", StringComparison.OrdinalIgnoreCase);

        public class RegisterRequest
        {
            [Required, StringLength(120)]
            public string Name { get; set; } = string.Empty;
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = string.Empty;
            [Required, StringLength(128, MinimumLength = 8)]
            public string Password { get; set; } = string.Empty;
            /// <summary>"Learner" (default) or "Educator" — chosen at sign-up.</summary>
            [StringLength(16)]
            public string? Role { get; set; }
        }

        public class LoginRequest
        {
            [Required, EmailAddress, StringLength(254)]
            public string Email { get; set; } = string.Empty;
            [Required, StringLength(128)]
            public string Password { get; set; } = string.Empty;
        }

        public class UpdateMeRequest
        {
            [StringLength(120)]
            public string? Name { get; set; }
            [StringLength(2048)]
            public string? AvatarUrl { get; set; }
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

            var role = request.Role?.Trim();
            if (!string.Equals(role, "Educator", StringComparison.OrdinalIgnoreCase))
            {
                role = "Learner";
            }
            else
            {
                role = "Educator";
            }

            var learner = new Learner
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(request.Name) ? cleanEmail.Split('@')[0] : request.Name.Trim(),
                Email = cleanEmail,
                PasswordHash = PasswordHasher.Hash(request.Password),
                Role = role,
                AvatarUrl = string.Empty,
                OverallProgress = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(learner);

            // Initialize learner profile & state
            var profile = new LearnerProfile
            {
                Id = Guid.NewGuid(),
                LearnerId = learner.Id,
                LearningGoal = string.Empty,
                Subject = string.Empty,
                CurrentLevel = "Not set",
                TargetOutcome = string.Empty,
                WeeklyStudyHours = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Add(profile);

            var state = new LearnerState
            {
                Id = Guid.NewGuid(),
                LearnerId = learner.Id,
                ActiveStreakDays = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Add(state);

            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(learner);
            return Ok(token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var learner = await _context.Learners.FirstOrDefaultAsync(l => l.Email == cleanEmail);

            // A learner without a stored hash is a legacy/seed row that was never
            // issued credentials; it must not be sign-in-able with an arbitrary
            // password.
            if (learner == null || !PasswordHasher.Verify(learner.PasswordHash, request.Password))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (PasswordHasher.NeedsRehash(learner.PasswordHash))
            {
                learner.PasswordHash = PasswordHasher.Hash(request.Password);
                await _context.SaveChangesAsync();
            }

            var token = _tokenService.GenerateToken(learner);
            return Ok(token);
        }

        /// <summary>
        /// Read-only list of the seeded demo learners so the sign-in screen can
        /// offer one-click demo access. Never returns password hashes.
        /// </summary>
        [HttpGet("demo-accounts")]
        public IActionResult GetDemoAccounts()
        {
            if (!DemoAccessEnabled) return NotFound();
            var accounts = DemoAccounts.All
                .Select(a => new { a.Name, a.Email, a.Role, a.AvatarUrl })
                .ToList();

            return Ok(accounts);
        }

        [HttpPost("demo-login")]
        public async Task<IActionResult> DemoLogin()
        {
            if (!DemoAccessEnabled) return NotFound();
            var demoEmail = DemoAccounts.All[0].Email;
            var learner = await _context.Learners.FirstOrDefaultAsync(l => l.Email == demoEmail);
            if (learner == null)
                return StatusCode(503, new { message = "The demo account is not available in this environment." });

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

        /// <summary>Updates the signed-in learner's editable profile fields (name, avatar).</summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var learner = await _context.Learners.FirstOrDefaultAsync(l => l.Id == learnerId);
            if (learner == null) return NotFound();

            if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name cannot be blank." });
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl) &&
                (!Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out var avatarUri) ||
                 avatarUri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(avatarUri.UserInfo)))
                return BadRequest(new { message = "Avatar must be a valid HTTP(S) URL." });

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                learner.Name = request.Name.Trim();
            }
            if (request.AvatarUrl is not null)
            {
                learner.AvatarUrl = request.AvatarUrl.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                learner.Id,
                learner.Name,
                learner.Email,
                learner.Role,
                learner.AvatarUrl,
                learner.OverallProgress
            });
        }
    }
}
