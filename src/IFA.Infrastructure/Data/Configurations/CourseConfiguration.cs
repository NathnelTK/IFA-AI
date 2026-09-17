using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.ToTable("Courses");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Description).HasMaxLength(2000);
            builder.Property(c => c.Category).HasMaxLength(120);
            builder.Property(c => c.TargetAudience).HasMaxLength(160);
            builder.Property(c => c.ThumbnailUrl).HasMaxLength(512);
            builder.Property(c => c.ProviderName).HasMaxLength(160);
            builder.Property(c => c.ProviderLogo).HasMaxLength(512);
            builder.Property(c => c.Badge).HasMaxLength(64);
            builder.Property(c => c.ReviewCount).HasMaxLength(32);
            builder.Property(c => c.EstimatedDuration).HasMaxLength(64);
            builder.Property(c => c.ShareCode).HasMaxLength(64);

            builder.HasIndex(c => c.ShareCode);
            builder.HasIndex(c => c.IsPublic);

            builder.HasOne(c => c.CreatorLearner)
                .WithMany(l => l.AuthoredCourses)
                .HasForeignKey(c => c.CreatorLearnerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
