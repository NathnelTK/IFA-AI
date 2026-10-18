using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class EducatorProjectConfiguration : IEntityTypeConfiguration<EducatorProject>
    {
        public void Configure(EntityTypeBuilder<EducatorProject> builder)
        {
            builder.ToTable("EducatorProjects");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).HasMaxLength(2000);
            builder.Property(p => p.Status).HasMaxLength(32);

            builder.HasIndex(p => p.EducatorLearnerId);
            builder.HasIndex(p => p.Status);

            builder.HasOne(p => p.EducatorLearner)
                .WithMany()
                .HasForeignKey(p => p.EducatorLearnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Course)
                .WithMany()
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(p => p.Blueprint)
                .WithOne(b => b.Project)
                .HasForeignKey<CourseBlueprint>(b => b.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
