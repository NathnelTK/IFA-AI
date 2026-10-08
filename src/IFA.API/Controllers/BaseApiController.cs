using System;
using System.Security.Claims;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected static readonly Guid DemoLearnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        protected async Task<Guid> GetCurrentLearnerIdAsync(IApplicationDbContext context)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && Guid.TryParse(claim.Value, out var id))
            {
                return id;
            }

            // Ensure demo learner exists
            var demo = await context.Learners.FirstOrDefaultAsync(l => l.Id == DemoLearnerId);
            if (demo == null)
            {
                demo = new Learner
                {
                    Id = DemoLearnerId,
                    Name = "Alex Chen",
                    Email = "alex.chen@ifa.ai",
                    Role = "Learner",
                    AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&h=100&fit=crop&crop=faces",
                    OverallProgress = 42,
                    CreatedAt = DateTime.UtcNow
                };
                context.Add(demo);
                await context.SaveChangesAsync();
            }

            return DemoLearnerId;
        }
    }
}
