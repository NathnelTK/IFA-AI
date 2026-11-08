using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/payments")]
    public class PaymentsController : BaseApiController
    {
        private readonly IApplicationDbContext _context;
        private readonly IChapaPaymentService _chapa;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IApplicationDbContext context,
            IChapaPaymentService chapa,
            IConfiguration configuration,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _chapa = chapa;
            _configuration = configuration;
            _logger = logger;
        }

        public class InitializePaymentRequest
        {
            public Guid CourseId { get; set; }
            [Required, StringLength(2048)]
            public string RedirectUrl { get; set; } = string.Empty;
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
            var redirectUrl = ValidateRedirectUrl(request.RedirectUrl);
            if (redirectUrl == null)
                return BadRequest(new { message = "Payment return URL must use a configured frontend origin." });

            await using var transaction = await CourseAccess.BeginMutationAsync(_context, ct);
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
            if (course == null) return NotFound(new { message = "Course not found." });
            if (!course.IsPublished) return BadRequest(new { message = "This course is not published." });
            if (!course.IsPaid || course.PriceAmount <= 0)
            {
                return BadRequest(new { message = "This course is free — enroll directly instead of paying." });
            }
            if (course.EnrollmentMode == "ApprovalRequired" || course.EnrollmentLimit.HasValue)
                return Conflict(new { message = "This paid enrollment policy requires refund or expiring seat-reservation support and cannot start checkout." });
            if (!string.Equals(course.Currency, "ETB", StringComparison.OrdinalIgnoreCase) ||
                course.PriceAmount > 9_999_999_999.99m || decimal.Round(course.PriceAmount, 2) != course.PriceAmount)
                return Conflict(new { message = "The course price is not valid for the configured Chapa integration." });

            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.CourseId == course.Id && e.LearnerId == learnerId, ct);
            if (enrollment == null)
            {
                return Conflict(new { message = "Request enrollment before starting checkout." });
            }
            else if (enrollment.Status == "Active")
            {
                return BadRequest(new { message = "You are already enrolled in this course." });
            }
            else if (enrollment.Status == "PendingApproval")
            {
                return Ok(new
                {
                    message = "Payment is complete and educator approval is pending.",
                    enrollmentStatus = enrollment.Status
                });
            }
            else if (enrollment.Status == "Rejected")
            {
                return StatusCode(403, new { message = "Your enrollment request for this course was rejected." });
            }
            else if (enrollment.Status != "PendingPayment")
            {
                return Conflict(new { message = $"Checkout cannot start from enrollment status '{enrollment.Status}'." });
            }

            var learner = await _context.Learners.FirstOrDefaultAsync(l => l.Id == learnerId, ct);
            if (string.IsNullOrWhiteSpace(learner?.Email) || string.IsNullOrWhiteSpace(learner.Name))
                return BadRequest(new { message = "Add your name and email before starting checkout." });

            var pendingPayment = await _context.Payments
                .Where(p => p.CourseId == course.Id && p.LearnerId == learnerId && (p.Status == "Pending" || p.Status == "Initializing"))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(ct);
            var currency = string.IsNullOrWhiteSpace(course.Currency) ? "ETB" : course.Currency;
            if (pendingPayment?.Status == "Initializing" && pendingPayment.CreatedAt > DateTime.UtcNow.AddMinutes(-2))
                return Conflict(new { message = "Checkout initialization is already in progress. Retry shortly." });
            if (!string.IsNullOrWhiteSpace(pendingPayment?.CheckoutUrl) && pendingPayment.Amount == course.PriceAmount &&
                string.Equals(pendingPayment.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                if (transaction != null) await transaction.CommitAsync(ct);
                return Ok(new { checkoutUrl = pendingPayment.CheckoutUrl, txRef = pendingPayment.ProviderTxRef });
            }
            if (pendingPayment != null) pendingPayment.Status = "Failed";

            var txRef = $"WTF-{Guid.NewGuid():N}".Substring(0, 20);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                LearnerId = learnerId,
                CourseId = course.Id,
                Amount = course.PriceAmount,
                Currency = currency,
                Provider = "Chapa",
                ProviderTxRef = txRef,
                Status = "Initializing"
            };
            _context.Add(payment);
            await _context.SaveChangesAsync(ct);
            if (transaction != null) await transaction.CommitAsync(ct);
            if (transaction != null) await transaction.DisposeAsync();

            var result = await _chapa.InitializeAsync(new ChapaInitializeRequest
            {
                TxRef = txRef,
                Amount = course.PriceAmount,
                Currency = payment.Currency,
                RedirectUrl = $"{redirectUrl}{(redirectUrl.Contains('?') ? '&' : '?')}tx_ref={Uri.EscapeDataString(txRef)}",
                CallbackUrl = ValidHttpUrl(_configuration["CHAPA_CALLBACK_URL"]),
                CustomerEmail = learner.Email,
                CustomerFirstName = learner.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0],
                Title = course.Title
            }, ct);

            if (!result.Success || string.IsNullOrWhiteSpace(result.CheckoutUrl))
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync(ct);
                return StatusCode(502, new { message = result.Error ?? "Could not start the payment." });
            }

            payment.CheckoutUrl = result.CheckoutUrl;
            payment.Status = "Pending";
            await _context.SaveChangesAsync(ct);

            return Ok(new { checkoutUrl = result.CheckoutUrl, txRef });
        }

        private string? ValidateRedirectUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment)) return null;
            var origin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            var allowed = (_configuration["CORS_ALLOWED_ORIGINS"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Concat(_configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                .Select(item => item.TrimEnd('/'));
            return allowed.Contains(origin, StringComparer.OrdinalIgnoreCase) ? uri.ToString() : null;
        }

        private static string? ValidHttpUrl(string? value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo)
                ? uri.ToString()
                : null;

        private static bool MatchesPayment(ChapaVerifyResult verification, Payment payment) =>
            verification.Amount == payment.Amount &&
            string.Equals(verification.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Verifies a transaction with Chapa. On success the payment is marked
        /// Completed and the enrollment advances according to its configured mode.
        /// </summary>
        [HttpGet("verify/{txRef}")]
        public async Task<IActionResult> Verify(string txRef, CancellationToken ct)
        {
            var learnerId = await GetCurrentLearnerIdAsync(_context);
            if (!_chapa.IsConfigured)
            {
                return StatusCode(503, new { message = "Payments are not configured" });
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ProviderTxRef == txRef, ct);
            if (payment == null) return NotFound(new { message = "Unknown transaction reference." });
            if (payment.LearnerId != learnerId) return StatusCode(403, new { message = "This transaction belongs to another learner." });

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
            if (!MatchesPayment(verification, payment))
            {
                _logger.LogError("Chapa verification mismatch for {TxRef}: expected {Amount} {Currency}, received {ActualAmount} {ActualCurrency}.",
                    txRef, payment.Amount, payment.Currency, verification.Amount, verification.Currency);
                return Conflict(new { message = "Payment details did not match the enrollment charge. Contact support." });
            }

            return Ok(await MarkPaymentCompletedAsync(payment, enrollment, ct));
        }

        /// <summary>
        /// Chapa webhook callback. The HMAC-SHA256 signature of the raw body is
        /// verified with CHAPA_WEBHOOK_SECRET when configured. Processing is
        /// idempotent; transient verification/database failures return a retryable error.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
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
                return Unauthorized(new { received = false });
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
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new { received = false });
                }
                if (!MatchesPayment(verification, payment))
                {
                    _logger.LogError("Chapa webhook mismatch for {TxRef}: expected {Amount} {Currency}, received {ActualAmount} {ActualCurrency}.",
                        txRef, payment.Amount, payment.Currency, verification.Amount, verification.Currency);
                    return Conflict(new { received = false });
                }

                var enrollment = await _context.CourseEnrollments
                    .FirstOrDefaultAsync(e => e.CourseId == payment.CourseId && e.LearnerId == payment.LearnerId, ct);

                await MarkPaymentCompletedAsync(payment, enrollment, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing a Chapa webhook failed.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { received = false });
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
                var enrollmentMode = await _context.Courses.Where(course => course.Id == payment.CourseId)
                    .Select(course => course.EnrollmentMode).FirstAsync(ct);
                enrollment.Status = enrollmentMode == "ApprovalRequired" ? "PendingApproval" : "Active";
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
