using System;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public class TokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public Guid LearnerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Learner";
    }

    public interface ITokenService
    {
        TokenResult GenerateToken(Learner learner);
        Guid? ValidateToken(string token);
    }
}
