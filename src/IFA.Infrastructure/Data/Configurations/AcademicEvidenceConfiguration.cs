using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations;

public class AcademicEvidenceConfiguration : IEntityTypeConfiguration<AcademicEvidence>
{
    public void Configure(EntityTypeBuilder<AcademicEvidence> builder)
    {
        builder.HasKey(ae => ae.Id);

        builder.Property(ae => ae.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ae => ae.Authors)
            .HasMaxLength(1000);

        builder.Property(ae => ae.Doi)
            .HasMaxLength(100);

        builder.Property(ae => ae.Abstract)
            .HasMaxLength(5000);

        builder.Property(ae => ae.Url)
            .HasMaxLength(2000);

        builder.Property(ae => ae.ExternalId)
            .HasMaxLength(100);

        builder.Property(ae => ae.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(ae => ae.ResearchPackage)
            .WithMany()
            .HasForeignKey(ae => ae.ResearchPackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}