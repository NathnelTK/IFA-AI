using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : BaseApiController
    {
        private readonly IApplicationDbContext _context;
        private readonly IChapaPaymentService _chapa;

        public PaymentsController(IApplicationDbContext context, IChapaPaymentService chapa)
        {
            _context = context;
            _chapa = chapa;
        }

        public class InitializePaymentRequest
        {
            public Guid CourseId { get; set; }
        }

        /// <summary>
        /// Starts a Chapa checkout for a paid, published course. The learner's
        /// enrollment is created (or found) as PendingPayment; the enrollment
        /// only advances to PendingApproval after the payment verifies.
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] InitializePaymentRequest request, CancellationToken ct)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);

            if (!_chapa.IsConfigured)
            {
                return StatusCode(503, new { message = "Payments are not configured" });
            }

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
            if (course == null) return NotFound(new { message = "Course not found." });
            if (!course.IsPublic) return BadRequest(new { message = "This course is not published." });
            if (!course.IsPaid || course.PriceAmount <= 0)
            {
                return BadRequest(new { message = "This course is free — enroll directly instead of paying." });
            }

            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.CourseId == course.Id && e.LearnerId == learnerId, ct);
            if (enrollment == null)
            {
                enrollment = new CourseEnrollment
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LearnerId = learnerId,
                    Status = "PendingPayment"
                };
                _context.Add(enrollment);
            }
            else if (enrollment.Status == "Active")
            {
                return BadRequest(new { message = "You are already enrolled in this course." });
            }
            else if (enrollment.Status == "PendingPayment" || enrollment.Status == "PendingApproval")
            {
                return Ok(new
                {
                    message = "A payment is already in progress for this course.",
                    enrollmentStatus = enrollment.Status
                });
            }
            else if (enrollment.Status == "Rejected")
            {
                return StatusCode(403, new { message = "Your enrollment request for this course was rejected." });
            }

            var learner = await _context.Learners.FirstOrDefaultAsync(l => l.Id == learnerId, ct);
            var txRef = $"IFA-{Guid.NewGuid():N}".Substring(0, 20);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                CourseId = course.Id,
                Amount = course.PriceAmount,
                Currency = string.IsNullOrWhiteSpace(course.Currency) ? "ETB" : course.Currency,
                Provider = "Chapa",
                ProviderTxRef = txRef,
                Status = "Pending"
            };
            _context.Add(payment);
            await _context.SaveChangesAsync(ct);

            var result = await _chapa.InitializeAsync(new ChapaInitializeRequest
            {
                TxRef = txRef,
                Amount = course.PriceAmount,
                Currency = payment.Currency,
                RedirectUrl = null,
                CustomerEmail = string.IsNullOrWhiteSpace(learner?.Email) ? "learner@ifa.ai" : learner!.Email,
                CustomerFirstName = string.IsNullOrWhiteSpace(learner?.Name) ? "IFA Learner" : learner!.Name.Split(' ')[0],
                Title = course.Title
            }, ct);

            if (!result.Success || string.IsNullOrWhiteSpace(result.CheckoutUrl))
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync(ct);
                return StatusCode(502, new { message = result.Error ?? "Could not start the payment." });
            }

            payment.CheckoutUrl = result.CheckoutUrl;
            await _context.SaveChangesAsync(ct);

            return Ok(new { checkoutUrl = result.CheckoutUrl, txRef });
        }

        /// <summary>
        /// Verifies a transaction with Chapa. On success the payment is marked
        /// Completed and the enrollment advances PendingPayment →
        /// PendingApproval (paid courses still need educator approval).
        /// </summary>
        [HttpGet("verify/{txRef}")]
        public async Task<IActionResult> Verify(string txRef, CancellationToken ct)
        {
            if (!_chapa.IsConfigured)
            {
                return StatusCode(503, new { message = "Payments are not configured" });
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ProviderTxRef == txRef, ct);
            if (payment == null) return NotFound(new { message = "Unknown transaction reference." });

            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.CourseId == payment.CourseId && e.LearnerId == payment.LearnerId, ct);

            if (payment.Status == "Completed")
            {
                return Ok(new { status = payment.Status, enrollmentStatus = enrollment?.Status });
            }

            var verification = await _chapa.VerifyAsync(txRef, ct);
            if (!verification.Success)
            {
                return Ok(new { status = verification.Status ?? "pending", enrollmentStatus = enrollment?.Status });
            }

            return Ok(await MarkPaymentCompletedAsync(payment, enrollment, ct));
        }

        /// <summary>
        /// Chapa webhook callback. The HMAC-SHA256 signature of the raw body is
        /// verified with CHAPA_WEBHOOK_SECRET when configured (skipped only
        /// when no secret exists). Processing is idempotent; always returns 200
        /// quickly so Chapa does not retry an already-handled event.
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook(CancellationToken ct)
        {
            string rawBody;
            using (var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8))
            {
                rawBody = await reader.ReadToEndAsync(ct);
            }

            var secret = HttpContext.RequestServices
                .GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
                ["CHAPA_WEBHOOK_SECRET"];
            var signature = Request.Headers.TryGetValue("x-chapa-signature", out var values)
                ? values.FirstOrDefault()
                : null;

            if (!ChapaWebhookVerifier.IsValid(rawBody, signature, secret))
            {
                // Invalid signature: acknowledge but do not process.
                return Ok(new { received = true });
            }

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                var txRef =
                    TryGetString(doc.RootElement, "tx_ref")
                    ?? TryGetString(doc.RootElement, "data", "tx_ref")
                    ?? TryGetString(doc.RootElement, "data", "reference");

                if (string.IsNullOrWhiteSpace(txRef))
                {
                    return Ok(new { received = true });
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.ProviderTxRef == txRef, ct);
                if (payment == null || payment.Status == "Completed")
                {
                    return Ok(new { received = true });
                }

                // Trust-but-verify: confirm the transaction with Chapa before
                // granting access (the webhook body alone is not authoritative).
                var verification = await _chapa.VerifyAsync(txRef, ct);
                if (!verification.Success)
                {
                    return Ok(new { received = true });
                }

                var enrollment = await _context.CourseEnrollments
                    .FirstOrDefaultAsync(e => e.CourseId == payment.CourseId && e.LearnerId == payment.LearnerId, ct);

                await MarkPaymentCompletedAsync(payment, enrollment, ct);
            }
            catch
            {
                // Never fail the webhook: Chapa will retry otherwise.
            }

            return Ok(new { received = true });
        }

        private async Task<object> MarkPaymentCompletedAsync(
            Payment payment, CourseEnrollment? enrollment, CancellationToken ct)
        {
            payment.Status = "Completed";
            payment.CompletedAt = DateTime.UtcNow;

            string? enrollmentStatus = enrollment?.Status;
            if (enrollment != null && enrollment.Status == "PendingPayment")
            {
                // Paid courses require educator approval after payment.
                enrollment.Status = "PendingApproval";
                enrollmentStatus = enrollment.Status;
            }

            await _context.SaveChangesAsync(ct);

            return new { status = payment.Status, enrollmentStatus };
        }

        private static string? TryGetString(JsonElement element, params string[] path)
        {
            var current = element;
            foreach (var key in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(key, out current)) return null;
            }
            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }
    }
}
