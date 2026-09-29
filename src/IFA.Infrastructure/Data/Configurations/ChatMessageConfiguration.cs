

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder){
        builder.ToTable("ChatMessages");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new {m.ChatSessionId, m.SequenceNumber}).IsUnique();
        builder.HasOne(m => m.chatSession).WithMany(s =>s.Messages)
        .HasForeignKey(m => m.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}