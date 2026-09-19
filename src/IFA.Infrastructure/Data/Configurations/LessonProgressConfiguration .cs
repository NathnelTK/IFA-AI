using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
    {
        public void Configure(EntityTypeBuilder<LessonProgress> builder)
        {
            builder.ToTable("LessonProgress");
            builder.HasKey(p => p.Id);

            // Mirrors your CourseEnrollment pattern exactly: this is the
            // constraint that actually fixes the bug we found — one
            // learner can only have ONE progress row per lesson, ever.
            builder.HasIndex(p => new { p.LessonId, p.LearnerId }).IsUnique();

            builder.HasOne(p => p.Lesson)
                .WithMany()
                .HasForeignKey(p => p.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Learner)
                .WithMany()
                .HasForeignKey(p => p.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}