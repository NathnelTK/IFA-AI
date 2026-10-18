using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class GenerationJobConfiguration : IEntityTypeConfiguration<GenerationJob>
    {
        public void Configure(EntityTypeBuilder<GenerationJob> builder)
        {
            builder.ToTable("GenerationJobs");
            builder.HasKey(j => j.Id);

            builder.Property(j => j.ModuleTitle).HasMaxLength(300);
            builder.Property(j => j.Status).HasMaxLength(16);
            builder.Property(j => j.Error).HasMaxLength(2000);

            builder.HasIndex(j => j.ProjectId);
            builder.HasIndex(j => new { j.CourseId, j.ModuleIndex });

            builder.HasOne(j => j.Project)
                .WithMany(p => p.GenerationJobs)
                .HasForeignKey(j => j.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
