using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class CourseEnrollmentConfiguration : IEntityTypeConfiguration<CourseEnrollment>
    {
        public void Configure(EntityTypeBuilder<CourseEnrollment> builder)
        {
            builder.ToTable("CourseEnrollments");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.ProgressPercentage).HasDefaultValue(0);
            builder.Property(e => e.CompletedModulesCount).HasDefaultValue(0);

            // A learner can enroll in a given course only once.
            builder.HasIndex(e => new { e.CourseId, e.LearnerId }).IsUnique();

            builder.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Learner)
                .WithMany(l => l.Enrollments)
                .HasForeignKey(e => e.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
