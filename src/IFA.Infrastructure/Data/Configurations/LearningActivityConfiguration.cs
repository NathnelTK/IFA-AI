using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class LearningActivityConfiguration : IEntityTypeConfiguration<LearningActivity>
    {
        public void Configure(EntityTypeBuilder<LearningActivity> builder)
        {
            builder.ToTable("LearningActivities");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.ActivityType).IsRequired().HasMaxLength(50);
            builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
            builder.Property(a => a.Description).HasMaxLength(500);

            builder.HasOne(a => a.Learner)
                .WithMany(l => l.Activities)
                .HasForeignKey(a => a.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
