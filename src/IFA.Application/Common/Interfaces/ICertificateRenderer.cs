namespace IFA.Application.Common.Interfaces
{
    public interface ICertificateRenderer
    {
        Task<Stream> RenderAsync(
            string snapshotJson,
            string serialNumber,
            string verificationUrl,
            CancellationToken cancellationToken = default);
    }
}
