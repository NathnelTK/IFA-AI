using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class LearnerProfileConfiguration : IEntityTypeConfiguration<LearnerProfile>
    {
        public void Configure(EntityTypeBuilder<LearnerProfile> builder)
        {
            builder.ToTable("LearnerProfiles");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.LearningGoal).IsRequired().HasMaxLength(500);
            builder.Property(p => p.Subject).IsRequired().HasMaxLength(200);
            builder.Property(p => p.CurrentLevel).IsRequired().HasMaxLength(50);
            builder.Property(p => p.TargetOutcome).HasMaxLength(500);
            builder.Property(p => p.PreferredLanguage).HasMaxLength(20).HasDefaultValue("en");
            builder.Property(p => p.LearningStyle).HasMaxLength(50).HasDefaultValue("Hands-on");

            builder.HasOne(p => p.Learner)
                .WithOne(l => l.Profile)
                .HasForeignKey<LearnerProfile>(p => p.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
