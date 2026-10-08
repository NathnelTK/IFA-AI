using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    public class ImageResource
    {
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Source { get; set; } = "Wikimedia";
        /// <summary>"Image" for a representative photo/illustration, "Graph" for a diagram/chart.</summary>
        public string Kind { get; set; } = "Image";
    }

    /// <summary>
    /// Finds real, embeddable images and diagrams for a topic so the course
    /// builder can illustrate lessons without hallucinating image URLs.
    /// </summary>
    public interface IImageResourceService
    {
        Task<List<ImageResource>> SearchImagesAsync(string query, int maxResults = 3, CancellationToken cancellationToken = default);
    }
}
