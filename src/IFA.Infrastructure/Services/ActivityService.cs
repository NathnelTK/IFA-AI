using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Services
{
    public class ActivityService : IActivityService
    {
        private readonly IApplicationDbContext _context;

        public ActivityService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordActivityAsync(Guid learnerId, string activityType, string title, string description, string metadataJson = "{}", CancellationToken ct = default)
        {
            var activity = new LearningActivity
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                ActivityType = activityType,
                Title = title,
                Description = description,
                MetadataJson = metadataJson,
                CreatedAt = DateTime.UtcNow
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<List<LearningActivity>> GetRecentActivitiesAsync(Guid learnerId, int limit = 20, CancellationToken ct = default)
        {
            return await _context.Activities
                .Where(a => a.LearnerId == learnerId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);
        }
    }
}
