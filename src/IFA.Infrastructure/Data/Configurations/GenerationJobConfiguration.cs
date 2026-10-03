using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GenerationJobConfiguration : IEntityTypeConfiguration<GenerationJob>
{
    public void Configure(EntityTypeBuilder<GenerationJob> builder)
    {
        builder.ToTable("GenerationJobs");
        builder.HasKey(j => j.Id);
        // Used by both the status-polling endpoint and crash recovery.
        builder.HasIndex(j => new { j.LearnerId, j.Status });

        builder.HasOne(j => j.Learner).WithMany()
            .HasForeignKey(j => j.LearnerId).OnDelete(DeleteBehavior.Cascade);
    }
}