using System.Net;
using IFA.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IFA.UnitTests;

public sealed class YouTubeResourceServiceTests
{
    [Fact]
    public async Task SendsApiKeyInHeaderInsteadOfLoggedQueryString()
    {
        const string apiKey = "sensitive-test-key";
        var handler = new CapturingHandler();
        var client = new HttpClient(handler);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["YOUTUBE_API_KEY"] = apiKey
        }).Build();
        var service = new YouTubeResourceService(client, configuration, NullLogger<YouTubeResourceService>.Instance);

        await service.SearchEducationalVideosAsync("safe logging", maxResults: 1);

        Assert.NotNull(handler.Request);
        Assert.DoesNotContain(apiKey, handler.Request!.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal(apiKey, handler.Request.Headers.GetValues("X-Goog-Api-Key").Single());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"items\":[]}")
            });
        }
    }
}
