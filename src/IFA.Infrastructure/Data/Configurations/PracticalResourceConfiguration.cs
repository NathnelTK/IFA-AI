using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations;

public class PracticalResourceConfiguration : IEntityTypeConfiguration<PracticalResource>
{
    public void Configure(EntityTypeBuilder<PracticalResource> builder)
    {
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pr => pr.Description)
            .HasMaxLength(2000);

        builder.Property(pr => pr.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(pr => pr.ResourceType)
            .HasMaxLength(100);

        builder.Property(pr => pr.Source)
            .HasMaxLength(200);

        builder.Property(pr => pr.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(pr => pr.ResearchPackage)
            .WithMany()
            .HasForeignKey(pr => pr.ResearchPackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}