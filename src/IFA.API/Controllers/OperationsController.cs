using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/operations")]
    public sealed class OperationsController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly TimeProvider _timeProvider;

        public OperationsController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            TimeProvider timeProvider)
        {
            _db = db;
            _authorization = authorization;
            _timeProvider = timeProvider;
        }

        [HttpGet("dead-letters")]
        public async Task<IActionResult> DeadLetters([FromQuery] int take = 100, CancellationToken cancellationToken = default)
        {
            if (!await IsStepUpAdminAsync(cancellationToken)) return Forbid();
            take = Math.Clamp(take, 1, 200);
            var messages = await _db.OutboxMessages
                .Where(message => message.DeadLetteredAt != null)
                .OrderByDescending(message => message.DeadLetteredAt)
                .Take(take)
                .Select(message => new
                {
                    message.Id,
                    message.Type,
                    message.AggregateType,
                    message.AggregateId,
                    message.DeduplicationKey,
                    message.AttemptCount,
                    message.MaxAttempts,
                    message.LastErrorCode,
                    message.LastError,
                    message.CorrelationId,
                    message.OccurredAt,
                    message.DeadLetteredAt
                })
                .ToListAsync(cancellationToken);
            return Ok(messages);
        }

        [HttpPost("dead-letters/{id:guid}/retry")]
        public async Task<IActionResult> RetryDeadLetter(Guid id, CancellationToken cancellationToken)
        {
            if (!await IsStepUpAdminAsync(cancellationToken)) return Forbid();
            var message = await _db.OutboxMessages.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (message == null || message.DeadLetteredAt == null) return NotFound();
            message.DeadLetteredAt = null;
            message.AttemptCount = 0;
            message.AvailableAt = _timeProvider.GetUtcNow().UtcDateTime;
            message.LastErrorCode = null;
            message.LastError = null;
            message.LeaseOwner = null;
            message.LeaseToken = null;
            message.LeaseExpiresAt = null;
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { message.Id, status = "Queued" });
        }

        [HttpPost("authoring-jobs/{id:guid}/retry")]
        public async Task<IActionResult> RetryAuthoring(Guid id, CancellationToken cancellationToken)
        {
            if (!await IsStepUpAdminAsync(cancellationToken)) return Forbid();
            var job = await _db.AuthoringJobs.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (job == null || job.State != AuthoringJobState.Failed) return NotFound();
            job.State = AuthoringJobState.Queued;
            job.AttemptCount = 0;
            job.AvailableAt = _timeProvider.GetUtcNow().UtcDateTime;
            job.ErrorCode = null;
            job.ErrorDetail = null;
            job.CompletedAt = null;
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;
            job.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new { job.Id, job.State });
        }

        private async Task<bool> IsStepUpAdminAsync(CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            return int.TryParse(User.FindFirst("mfa_level")?.Value, out var mfaLevel)
                && mfaLevel >= 1
                && await _authorization.HasAccountRoleAsync(accountId, AccountRoleKind.PlatformAdmin, cancellationToken);
        }
    }
}
