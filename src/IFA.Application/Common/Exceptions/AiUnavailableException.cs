using System;

namespace IFA.Application.Common.Exceptions
{
    public sealed class AiUnavailableException : InvalidOperationException
    {
        public AiUnavailableException(string message) : base(message)
        {
        }
    }
}
