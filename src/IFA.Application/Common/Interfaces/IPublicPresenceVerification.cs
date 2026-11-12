namespace IFA.Application.Common.Interfaces
{
    public interface IDomainDnsVerifier
    {
        Task<bool> HasTxtRecordAsync(string host, string expectedValue, CancellationToken cancellationToken = default);
        Task<bool> HasCnameRecordAsync(string host, string expectedTarget, CancellationToken cancellationToken = default);
    }

    public interface IExternalWebsiteVerifier
    {
        Task<bool> VerifyAsync(string baseUrl, string expectedValue, CancellationToken cancellationToken = default);
    }

    public interface ICustomDomainProvisioner
    {
        Task<bool> EnsureReadyAsync(string host, CancellationToken cancellationToken = default);
    }
}
