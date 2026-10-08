

public enum ChatRole { User, Assistant }
public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatSessionId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatSession? chatSession { get; set; }
}