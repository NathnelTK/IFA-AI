using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class LearnerSettingsConfiguration : IEntityTypeConfiguration<LearnerSettings>
    {
        public void Configure(EntityTypeBuilder<LearnerSettings> builder)
        {
            builder.ToTable("LearnerSettings");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Theme).HasMaxLength(20).HasDefaultValue("system");
            builder.Property(s => s.AccentColor).HasMaxLength(30).HasDefaultValue("pine");
            builder.Property(s => s.FontSize).HasMaxLength(20).HasDefaultValue("medium");

            builder.HasOne(s => s.Learner)
                .WithOne(l => l.Settings)
                .HasForeignKey<LearnerSettings>(s => s.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
