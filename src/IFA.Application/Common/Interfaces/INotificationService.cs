using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(Guid learnerId, string type, string title, string message, string? linkUrl = null, CancellationToken ct = default);
        Task<List<Notification>> GetNotificationsAsync(Guid learnerId, bool unreadOnly = false, CancellationToken ct = default);
        Task MarkAsReadAsync(Guid notificationId, Guid learnerId, CancellationToken ct = default);
        Task MarkAllAsReadAsync(Guid learnerId, CancellationToken ct = default);
    }
}
