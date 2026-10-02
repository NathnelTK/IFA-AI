using System;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : BaseApiController
    {
        private readonly INotificationService _notificationService;
        private readonly IApplicationDbContext _context;

        public NotificationsController(INotificationService notificationService, IApplicationDbContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            var list = await _notificationService.GetNotificationsAsync(learnerId, unreadOnly);
            return Ok(list);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            await _notificationService.MarkAsReadAsync(id, learnerId);
            return Ok(new { success = true });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            await _notificationService.MarkAllAsReadAsync(learnerId);
            return Ok(new { success = true });
        }
    }
}
