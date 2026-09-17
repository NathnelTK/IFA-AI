using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class CourseShareInviteConfiguration : IEntityTypeConfiguration<CourseShareInvite>
    {
        public void Configure(EntityTypeBuilder<CourseShareInvite> builder)
        {
            builder.ToTable("CourseShareInvites");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.ShareCode).IsRequired().HasMaxLength(64);
            builder.Property(i => i.RecipientEmail).HasMaxLength(256);

            builder.HasIndex(i => i.ShareCode);

            builder.HasOne(i => i.Course)
                .WithMany(c => c.ShareInvites)
                .HasForeignKey(i => i.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Keep the sender history if a learner account is removed.
            builder.HasOne(i => i.SenderLearner)
                .WithMany()
                .HasForeignKey(i => i.SenderLearnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
