using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IHostPolicyService
    {
        bool IsAllowed(string host, AuthSiteScope siteScope);
        Task<bool> IsAllowedAsync(
            string host,
            AuthSiteScope siteScope,
            CancellationToken cancellationToken = default);
        string Normalize(string host);
    }
}
