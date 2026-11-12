using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure.Services
{
    public sealed class HostPolicyService : IHostPolicyService
    {
        private readonly AuthOptions _options;
        private readonly IHostEnvironment _environment;
        private readonly ApplicationDbContext _db;

        public HostPolicyService(
            IOptions<AuthOptions> options,
            IHostEnvironment environment,
            ApplicationDbContext db)
        {
            _options = options.Value;
            _environment = environment;
            _db = db;
        }

        public async Task<bool> IsAllowedAsync(
            string host,
            AuthSiteScope siteScope,
            CancellationToken cancellationToken = default)
        {
            if (IsAllowed(host, siteScope)) return true;
            if (siteScope != AuthSiteScope.Public) return false;
            var normalized = Normalize(host);
            return await _db.CustomDomains.AnyAsync(domain =>
                domain.Host == normalized
                && domain.Status == "Verified"
                && domain.DisabledAt == null,
                cancellationToken);
        }

        public bool IsAllowed(string host, AuthSiteScope siteScope)
        {
            var normalized = Normalize(host);
            if (_environment.IsDevelopment()
                && (normalized == "localhost" || normalized == "127.0.0.1" || normalized == "[::1]"))
            {
                return true;
            }

            var allowed = siteScope switch
            {
                AuthSiteScope.Auth => _options.CentralHosts,
                AuthSiteScope.App => _options.AppHosts,
                AuthSiteScope.Public => _options.PublicHosts,
                _ => []
            };
            return allowed.Select(Normalize).Contains(normalized, StringComparer.OrdinalIgnoreCase);
        }

        public string Normalize(string host)
        {
            var value = host.Trim();
            if (Uri.TryCreate($"https://{value}", UriKind.Absolute, out var uri))
            {
                value = uri.Host;
            }
            value = value.TrimEnd('.').ToLowerInvariant();
            if (value.Length is 0 or > 253 || value.Contains('/') || value.Contains('\\'))
            {
                throw new InvalidOperationException("The request host is invalid.");
            }
            return value;
        }
    }
}
