using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Services
{
    public class ChapaInitializeResult
    {
        public bool Success { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? Error { get; set; }
    }

    public class ChapaVerifyResult
    {
        public bool Success { get; set; }
        /// <summary>Chapa status string for the transaction ("success", "pending", "failed", ...).</summary>
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Error { get; set; }
    }

    public class ChapaInitializeRequest
    {
        public string TxRef { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ETB";
        public string? RedirectUrl { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerFirstName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thin HTTP client over the Chapa payment aggregator API
    /// (initialize + verify). Configured via CHAPA_SECRET_KEY / CHAPA_BASE_URL.
    /// </summary>
    public interface IChapaPaymentService
    {
        bool IsConfigured { get; }
        Task<ChapaInitializeResult> InitializeAsync(ChapaInitializeRequest request, CancellationToken ct = default);
        Task<ChapaVerifyResult> VerifyAsync(string txRef, CancellationToken ct = default);
    }

    /// <summary>
    /// Verifies a Chapa webhook signature: HMAC-SHA256 of the raw request
    /// body using CHAPA_WEBHOOK_SECRET, hex-encoded in the
    /// x-chapa-signature header. Skips verification only when no secret is
    /// configured.
    /// </summary>
    public static class ChapaWebhookVerifier
    {
        public static bool IsValid(string rawBody, string? signatureHeader, string? secret)
        {
            if (string.IsNullOrWhiteSpace(secret)) return true; // not configured - skip
            if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
            return string.Equals(expected, signatureHeader.Trim().ToLowerInvariant(), StringComparison.Ordinal);
        }
    }

    public class ChapaPaymentService : IChapaPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _secretKey;
        private readonly string _baseUrl;
        private readonly ILogger<ChapaPaymentService> _logger;

        public ChapaPaymentService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ChapaPaymentService> logger)
        {
            _httpClient = httpClient;
            _secretKey = configuration["CHAPA_SECRET_KEY"];
            _baseUrl = (configuration["CHAPA_BASE_URL"] ?? "https://api.chapa.co/v1").TrimEnd('/');
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_secretKey) &&
            _secretKey != "change_me" &&
            !_secretKey.StartsWith("your_", StringComparison.OrdinalIgnoreCase);

        public async Task<ChapaInitializeResult> InitializeAsync(ChapaInitializeRequest request, CancellationToken ct = default)
        {
            if (!IsConfigured)
            {
                return new ChapaInitializeResult { Success = false, Error = "Payments are not configured" };
            }

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    tx_ref = request.TxRef,
                    amount = request.Amount,
                    currency = request.Currency,
                    redirect_url = request.RedirectUrl,
                    customer = new
                    {
                        email = request.CustomerEmail,
                        first_name = request.CustomerFirstName
                    },
                    customization = new { title = request.Title }
                });

                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/payment/initialize")
                {
                    Content = content
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

                using var response = await _httpClient.SendAsync(message, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Chapa initialize failed with {Status}: {Body}", (int)response.StatusCode, body);
                    return new ChapaInitializeResult { Success = false, Error = $"Chapa returned HTTP {(int)response.StatusCode}." };
                }

                using var doc = JsonDocument.Parse(body);
                var checkoutUrl =
                    TryGetString(doc.RootElement, "data", "checkout_url")
                    ?? TryGetString(doc.RootElement, "data", "url")
                    ?? TryGetString(doc.RootElement, "checkout_url");

                if (string.IsNullOrWhiteSpace(checkoutUrl))
                {
                    return new ChapaInitializeResult { Success = false, Error = "Chapa did not return a checkout URL." };
                }

                return new ChapaInitializeResult { Success = true, CheckoutUrl = checkoutUrl };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chapa initialize threw for tx {TxRef}.", request.TxRef);
                return new ChapaInitializeResult { Success = false, Error = "Could not reach the payment provider." };
            }
        }

        public async Task<ChapaVerifyResult> VerifyAsync(string txRef, CancellationToken ct = default)
        {
            if (!IsConfigured)
            {
                return new ChapaVerifyResult { Success = false, Error = "Payments are not configured" };
            }

            try
            {
                using var message = new HttpRequestMessage(
                    HttpMethod.Get, $"{_baseUrl}/transaction/verify/{Uri.EscapeDataString(txRef)}");
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);

                using var response = await _httpClient.SendAsync(message, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Chapa verify failed with {Status} for {TxRef}: {Body}", (int)response.StatusCode, txRef, body);
                    return new ChapaVerifyResult { Success = false, Error = $"Chapa returned HTTP {(int)response.StatusCode}." };
                }

                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("data", out var data))
                {
                    return new ChapaVerifyResult { Success = false, Error = "Unexpected Chapa response." };
                }

                var status = data.TryGetProperty("status", out var st) ? st.GetString() : null;
                decimal amount = 0;
                if (data.TryGetProperty("amount", out var amt))
                {
                    if (amt.ValueKind == JsonValueKind.Number && amt.TryGetDecimal(out var d)) amount = d;
                    else if (amt.ValueKind == JsonValueKind.String && decimal.TryParse(amt.GetString(), out var d2)) amount = d2;
                }
                var currency = data.TryGetProperty("currency", out var cur) ? cur.GetString() : null;

                return new ChapaVerifyResult
                {
                    Success = string.Equals(status, "success", StringComparison.OrdinalIgnoreCase),
                    Status = status,
                    Amount = amount,
                    Currency = currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chapa verify threw for tx {TxRef}.", txRef);
                return new ChapaVerifyResult { Success = false, Error = "Could not reach the payment provider." };
            }
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
