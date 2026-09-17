using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class SkillMetricConfiguration : IEntityTypeConfiguration<SkillMetric>
    {
        public void Configure(EntityTypeBuilder<SkillMetric> builder)
        {
            builder.ToTable("SkillMetrics");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.SkillName).IsRequired().HasMaxLength(160);
            builder.Property(s => s.HexColor).HasMaxLength(16);
            builder.Property(s => s.IconName).HasMaxLength(64);

            builder.HasIndex(s => new { s.LearnerId, s.SkillName }).IsUnique();

            builder.HasOne(s => s.Learner)
                .WithMany(l => l.Skills)
                .HasForeignKey(s => s.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
