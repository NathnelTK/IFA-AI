using System.IdentityModel.Tokens.Jwt;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace IFA.UnitTests;

public sealed class TokenServiceTests
{
    [Fact]
    public void UsesInjectedClockForLegacyTokenLifetime()
    {
        var now = new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);
        var clock = new FixedTimeProvider(now);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT_SECRET"] = "test-only-secret-that-is-at-least-thirty-two-characters",
            ["Jwt:Issuer"] = "Witforge.Auth",
            ["Jwt:Audience"] = "Witforge.LegacyClient"
        }).Build();
        var service = new TokenService(configuration, clock);

        var result = service.GenerateToken(new Learner
        {
            Id = Guid.NewGuid(),
            Name = "Clock Test",
            Email = "clock@example.test",
            Role = "Learner"
        });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal(now.UtcDateTime.AddDays(7), result.ExpiresAt);
        Assert.Equal(now.ToUnixTimeSeconds(), long.Parse(jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Iat).Value));
        Assert.Equal("Witforge.Auth", jwt.Issuer);
        Assert.Contains("Witforge.LegacyClient", jwt.Audiences);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
