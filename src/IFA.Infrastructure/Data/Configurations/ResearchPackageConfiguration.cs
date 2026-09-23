using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class ResearchPackageConfiguration : IEntityTypeConfiguration<ResearchPackage>
    {
        public void Configure(EntityTypeBuilder<ResearchPackage> builder)
        {
            builder.ToTable("ResearchPackages");
            builder.HasKey(r => r.Id);

            builder.HasIndex(r => r.LearnerProfileId);

            builder.HasOne(r => r.LearnerProfile)
                .WithMany()
                .HasForeignKey(r => r.LearnerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // AcademicEvidence, PracticalResource, and VideoResource have
            // no navigation property pointing back to ResearchPackage —
            // that's fine, EF supports a one-directional relationship as
            // long as we tell it which FK to use explicitly here.
            builder.HasMany(r => r.AcademicSources)
                .WithOne()
                .HasForeignKey(a => a.ResearchPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.PracticalResources)
                .WithOne()
                .HasForeignKey(p => p.ResearchPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.VideoResources)
                .WithOne()
                .HasForeignKey(v => v.ResearchPackageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class AcademicEvidenceConfiguration : IEntityTypeConfiguration<AcademicEvidence>
    {
        public void Configure(EntityTypeBuilder<AcademicEvidence> builder)
        {
            builder.ToTable("AcademicEvidence");
            builder.HasKey(a => a.Id);
        }
    }

    public class PracticalResourceConfiguration : IEntityTypeConfiguration<PracticalResource>
    {
        public void Configure(EntityTypeBuilder<PracticalResource> builder)
        {
            builder.ToTable("PracticalResources");
            builder.HasKey(p => p.Id);
        }
    }

    public class VideoResourceConfiguration : IEntityTypeConfiguration<VideoResource>
    {
        public void Configure(EntityTypeBuilder<VideoResource> builder)
        {
            builder.ToTable("VideoResources");
            builder.HasKey(v => v.Id);
        }
    }
}