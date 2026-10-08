

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("ChatSessions");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.LearnerId);

        builder.HasOne(s => s.Learner).WithMany().HasForeignKey(s => s.LearnerId).OnDelete(DeleteBehavior.Cascade);
    }
}