using System.Net.Http.Headers;
using System.Net.Http.Json;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class TransactionalEmailService : ITransactionalEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly EmailOptions _options;

        public TransactionalEmailService(HttpClient httpClient, IOptions<EmailOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task SendAsync(TransactionalEmail email, CancellationToken cancellationToken = default)
        {
            if (_options.DeliveryMode.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (!_options.DeliveryMode.Equals("Resend", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The transactional email delivery mode is not supported.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Headers.TryAddWithoutValidation("Idempotency-Key", email.IdempotencyKey);
            request.Content = JsonContent.Create(new
            {
                from = _options.From,
                to = new[] { email.To },
                subject = email.Subject,
                text = email.TextBody,
                html = email.HtmlBody
            });
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Transactional email provider returned HTTP {(int)response.StatusCode}.",
                    inner: null,
                    response.StatusCode);
            }
        }
    }
}
