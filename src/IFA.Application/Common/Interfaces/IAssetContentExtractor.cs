namespace IFA.Application.Common.Interfaces
{
    public interface IAssetContentExtractor
    {
        Task<string> ExtractAsync(
            Stream content,
            string contentType,
            string fileName,
            CancellationToken cancellationToken = default);
    }

    public interface IImageOcrService
    {
        Task<string> ExtractTextAsync(
            Stream image,
            string contentType,
            CancellationToken cancellationToken = default);
    }
}
