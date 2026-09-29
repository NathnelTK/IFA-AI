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

            builder.Property(p => p.Goal).IsRequired();

            // A learner can have multiple profiles over time (goals change),
            // so this is NOT unique on LearnerId alone — unlike
            // CourseEnrollment, there's no "only once" rule here.
            builder.HasIndex(p => p.LearnerId);

            builder.HasOne(p => p.Learner)
                .WithMany()
                .HasForeignKey(p => p.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}