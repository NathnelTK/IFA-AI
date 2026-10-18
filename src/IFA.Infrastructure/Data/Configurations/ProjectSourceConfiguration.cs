using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class ProjectSourceConfiguration : IEntityTypeConfiguration<ProjectSource>
    {
        public void Configure(EntityTypeBuilder<ProjectSource> builder)
        {
            builder.ToTable("ProjectSources");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Url).IsRequired().HasMaxLength(2048);
            builder.Property(s => s.Type).HasMaxLength(32);
            builder.Property(s => s.Title).HasMaxLength(300);
            builder.Property(s => s.Description).HasMaxLength(2000);
            // Extracted article/PDF text — intentionally unbounded (Postgres text).
            builder.Property(s => s.ExtractedContent);
            builder.Property(s => s.Status).HasMaxLength(16);
            builder.Property(s => s.FailureReason).HasMaxLength(1000);
            builder.Property(s => s.AddedBy).HasMaxLength(8);

            builder.HasIndex(s => s.ProjectId);

            builder.HasOne(s => s.Project)
                .WithMany(p => p.Sources)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
