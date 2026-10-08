// IFA.Application/Common/Interfaces/IUnderstandingAgentService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IUnderstandingAgentService
    {
        // Turn N of the conversation: given everything said so far plus
        // the learner's new message, returns the agent's next reply.
        // Does NOT touch LearnerProfile - purely conversational.
        Task<string> ContinueConversationAsync(ChatSession session, string learnerMessage, CancellationToken cancellationToken = default);

        // Called once, when the learner clicks "Generate my course".
        // Reads the full transcript and extracts a LearnerProfile.
        // Returns null if extraction fails even after retry - caller
        // must handle that (e.g. ask the learner to add more detail).
        Task<LearnerProfile?> ExtractProfileAsync(ChatSession session, CancellationToken cancellationToken = default);
    }
}