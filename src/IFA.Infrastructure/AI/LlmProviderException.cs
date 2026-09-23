using System;

namespace IFA.Infrastructure.AI
{
    public class LlmProviderException : Exception
    {
        public LlmProviderException(
            string providerName,
            string message,
            bool isRateLimited = false,
            Exception? innerException = null)
            : base(message, innerException)
        {
            ProviderName = providerName;
            IsRateLimited = isRateLimited;
        }

        public string ProviderName { get; }

        public bool IsRateLimited { get; }
    }
}
