

using IFA.Domain.Entities;

public enum ChatSessionStatus { Active, Finalized }
public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LearnerId { get; set; }
    public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Active;
    // Set only once finalize succeeds - links the conversation to
    // the profile it eventually produced.
    public Guid? ResultingLearnerProfileId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Learner? Learner { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}