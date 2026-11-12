using System.Security.Cryptography;
using System.Text;
using IFA.Infrastructure.Services;

namespace IFA.UnitTests;

public sealed class ChapaWebhookVerifierTests
{
    [Fact]
    public void MissingSecretFailsClosed()
    {
        Assert.False(ChapaWebhookVerifier.IsValid("{}", "anything", null));
    }

    [Fact]
    public void AcceptsOnlyMatchingHmac()
    {
        const string body = "{\"tx_ref\":\"test\"}";
        const string secret = "webhook-test-secret";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        Assert.True(ChapaWebhookVerifier.IsValid(body, signature, secret));
        Assert.False(ChapaWebhookVerifier.IsValid(body, "00", secret));
    }
}
