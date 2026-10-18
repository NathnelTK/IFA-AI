using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class SourceConflictConfiguration : IEntityTypeConfiguration<SourceConflict>
    {
        public void Configure(EntityTypeBuilder<SourceConflict> builder)
        {
            builder.ToTable("SourceConflicts");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Kind).HasMaxLength(16);
            // Comma-separated list of the participating ProjectSource ids.
            builder.Property(c => c.SourceIds).HasMaxLength(2000);
            builder.Property(c => c.Explanation).HasMaxLength(4000);
            builder.Property(c => c.SuggestedResolutions);
            builder.Property(c => c.Resolution).HasMaxLength(16);
            builder.Property(c => c.ManualNote).HasMaxLength(2000);
            builder.Property(c => c.ResolvedBy).HasMaxLength(8);

            builder.HasIndex(c => c.ProjectId);

            builder.HasOne(c => c.Project)
                .WithMany(p => p.Conflicts)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
