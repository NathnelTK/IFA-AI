using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IFA.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string SecretKey => _configuration["JWT_SECRET"] 
            ?? _configuration["Jwt:Key"] 
            ?? "IFA_SUPER_SECRET_SECURITY_KEY_FOR_HACKATHON_DEMO_2026_LONG_ENOUGH";

        private string Issuer => _configuration["Jwt:Issuer"] ?? "IFA.API";
        private string Audience => _configuration["Jwt:Audience"] ?? "IFA.Client";

        public TokenResult GenerateToken(Learner learner)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);

            var expiresAt = DateTime.UtcNow.AddDays(7);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, learner.Id.ToString()),
                    new Claim(ClaimTypes.Name, learner.Name ?? "Learner"),
                    new Claim(ClaimTypes.Email, learner.Email ?? ""),
                    new Claim(ClaimTypes.Role, learner.Role)
                }),
                Expires = expiresAt,
                Issuer = Issuer,
                Audience = Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new TokenResult
            {
                AccessToken = tokenString,
                RefreshToken = Guid.NewGuid().ToString("N"),
                ExpiresAt = expiresAt,
                LearnerId = learner.Id,
                Name = learner.Name,
                Email = learner.Email,
                Role = learner.Role
            };
        }

        public Guid? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
                if (claim != null && Guid.TryParse(claim.Value, out var learnerId))
                {
                    return learnerId;
                }
            }
            catch
            {
                // Invalid token
            }

            return null;
        }
    }
}
