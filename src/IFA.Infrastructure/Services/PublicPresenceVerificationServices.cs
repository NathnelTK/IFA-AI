using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class DomainDnsVerifier : IDomainDnsVerifier
    {
        private readonly HttpClient _httpClient;
        private readonly CustomDomainOptions _options;

        public DomainDnsVerifier(HttpClient httpClient, IOptions<CustomDomainOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<bool> HasTxtRecordAsync(
            string host,
            string expectedValue,
            CancellationToken cancellationToken = default)
        {
            var answers = await QueryAsync($"_witforge.{host}", "TXT", cancellationToken);
            return answers.Any(value => value.Trim('"').Equals(expectedValue, StringComparison.Ordinal));
        }

        public async Task<bool> HasCnameRecordAsync(
            string host,
            string expectedTarget,
            CancellationToken cancellationToken = default)
        {
            var answers = await QueryAsync(host, "CNAME", cancellationToken);
            return answers.Any(value => value.TrimEnd('.').Equals(expectedTarget.TrimEnd('.'), StringComparison.OrdinalIgnoreCase));
        }

        private async Task<IReadOnlyList<string>> QueryAsync(
            string name,
            string type,
            CancellationToken cancellationToken)
        {
            var url = $"{_options.DnsOverHttpsUrl.TrimEnd('/')}?name={Uri.EscapeDataString(name)}&type={type}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("Answer", out var answer) || answer.ValueKind != JsonValueKind.Array)
            {
                return [];
            }
            return answer.EnumerateArray()
                .Where(item => item.TryGetProperty("data", out _))
                .Select(item => item.GetProperty("data").GetString() ?? string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }
    }

    public sealed class ExternalWebsiteVerifier : IExternalWebsiteVerifier
    {
        private readonly HttpClient _httpClient;

        public ExternalWebsiteVerifier(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> VerifyAsync(
            string baseUrl,
            string expectedValue,
            CancellationToken cancellationToken = default)
        {
            var current = new Uri($"{baseUrl.TrimEnd('/')}/.well-known/witforge-verification.txt");
            for (var redirect = 0; redirect <= 3; redirect++)
            {
                await EnsurePublicAsync(current, cancellationToken);
                using var response = await _httpClient.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location != null)
                {
                    current = response.Headers.Location.IsAbsoluteUri
                        ? response.Headers.Location
                        : new Uri(current, response.Headers.Location);
                    if (current.Scheme != Uri.UriSchemeHttps) return false;
                    continue;
                }
                if (!response.IsSuccessStatusCode
                    || response.Content.Headers.ContentLength is > 4096) return false;
                var value = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
                return value.Equals(expectedValue, StringComparison.Ordinal);
            }
            return false;
        }

        private static async Task EnsurePublicAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException("External website verification requires HTTPS.");
            var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
            if (addresses.Length == 0 || addresses.Any(IsPrivate))
                throw new InvalidOperationException("External website verification cannot connect to private addresses.");
        }

        internal static bool IsPrivate(IPAddress address)
        {
            if (IPAddress.IsLoopback(address)) return true;
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = address.GetAddressBytes();
                return bytes[0] == 10
                    || bytes[0] == 127
                    || (bytes[0] == 169 && bytes[1] == 254)
                    || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168)
                    || (bytes[0] == 100 && bytes[1] is >= 64 and <= 127);
            }
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || (address.GetAddressBytes()[0] & 0xfe) == 0xfc;
            }
            return true;
        }
    }

    public static class PublicNetworkConnect
    {
        public static async ValueTask<Stream> ConnectAsync(
            SocketsHttpConnectionContext context,
            CancellationToken cancellationToken)
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            var address = addresses.FirstOrDefault(candidate => !ExternalWebsiteVerifier.IsPrivate(candidate))
                ?? throw new InvalidOperationException("The destination resolved only to private or reserved addresses.");
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }

    public sealed class VercelCustomDomainProvisioner : ICustomDomainProvisioner
    {
        private readonly HttpClient _httpClient;
        private readonly CustomDomainOptions _options;

        public VercelCustomDomainProvisioner(HttpClient httpClient, IOptions<CustomDomainOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<bool> EnsureReadyAsync(string host, CancellationToken cancellationToken = default)
        {
            var project = Uri.EscapeDataString(_options.VercelProjectId);
            var team = string.IsNullOrWhiteSpace(_options.VercelTeamId)
                ? string.Empty
                : $"?teamId={Uri.EscapeDataString(_options.VercelTeamId)}";
            using var create = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.VercelApiUrl.TrimEnd('/')}/v10/projects/{project}/domains{team}");
            create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.VercelToken);
            create.Content = JsonContent.Create(new { name = host });
            using var created = await _httpClient.SendAsync(create, cancellationToken);
            if (!created.IsSuccessStatusCode && created.StatusCode != HttpStatusCode.Conflict)
                throw new HttpRequestException($"Vercel domain registration returned HTTP {(int)created.StatusCode}.");

            using var check = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_options.VercelApiUrl.TrimEnd('/')}/v9/projects/{project}/domains/{Uri.EscapeDataString(host)}{team}");
            check.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.VercelToken);
            using var checkedResponse = await _httpClient.SendAsync(check, cancellationToken);
            checkedResponse.EnsureSuccessStatusCode();
            var result = await checkedResponse.Content.ReadFromJsonAsync<VercelDomainResponse>(cancellationToken: cancellationToken);
            return result?.Verified == true;
        }

        private sealed record VercelDomainResponse(bool Verified);
    }
}
