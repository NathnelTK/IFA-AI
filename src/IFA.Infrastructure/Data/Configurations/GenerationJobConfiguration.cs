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
            builder.Property(j => j.Kind).HasMaxLength(32);
            builder.Property(j => j.RequestJson).HasColumnType("jsonb");
            builder.Property(j => j.Status).HasMaxLength(16);
            builder.Property(j => j.Error).HasMaxLength(2000);
            builder.Property(j => j.ErrorCode).HasMaxLength(100);
            builder.Property(j => j.LeaseOwner).HasMaxLength(200);
            builder.Property(j => j.CorrelationId).HasMaxLength(100);
            builder.Property(j => j.CausationId).HasMaxLength(100);

            builder.HasIndex(j => j.ProjectId);
            builder.HasIndex(j => j.AuthoringJobId);
            builder.HasIndex(j => new { j.CourseId, j.ModuleIndex });
            builder.HasIndex(j => new { j.Status, j.AvailableAt });
            builder.HasIndex(j => new { j.Status, j.LeaseExpiresAt });
            builder.HasIndex(j => new { j.CourseId, j.ModuleIndex })
                .IsUnique()
                .HasDatabaseName("IX_GenerationJobs_ActiveCourseModule")
                .HasFilter("\"Status\" IN ('Queued', 'Running')");

            builder.ToTable(table => table.HasCheckConstraint(
                "CK_GenerationJobs_Status",
                "\"Status\" IN ('Queued', 'Running', 'Done', 'Failed')"));

            builder.HasOne(j => j.Project)
                .WithMany(p => p.GenerationJobs)
                .HasForeignKey(j => j.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<AuthoringJob>()
                .WithMany()
                .HasForeignKey(j => j.AuthoringJobId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
