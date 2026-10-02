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
    public class NotificationService : INotificationService
    {
        private readonly IApplicationDbContext _context;

        public NotificationService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendNotificationAsync(Guid learnerId, string type, string title, string message, string? linkUrl = null, CancellationToken ct = default)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                Type = type,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(notification);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<List<Notification>> GetNotificationsAsync(Guid learnerId, bool unreadOnly = false, CancellationToken ct = default)
        {
            var query = _context.Notifications.Where(n => n.LearnerId == learnerId);
            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }
            return await query.OrderByDescending(n => n.CreatedAt).ToListAsync(ct);
        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid learnerId, CancellationToken ct = default)
        {
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.LearnerId == learnerId, ct);
            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task MarkAllAsReadAsync(Guid learnerId, CancellationToken ct = default)
        {
            var unread = await _context.Notifications.Where(n => n.LearnerId == learnerId && !n.IsRead).ToListAsync(ct);
            foreach (var n in unread)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync(ct);
        }
    }
}
