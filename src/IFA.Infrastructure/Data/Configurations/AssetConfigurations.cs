using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
    {
        public void Configure(EntityTypeBuilder<Asset> builder)
        {
            builder.ToTable("Assets");
            builder.HasKey(asset => asset.Id);
            builder.Property(asset => asset.Purpose).IsRequired().HasMaxLength(64);
            builder.Property(asset => asset.OriginalFileName).IsRequired().HasMaxLength(256);
            builder.Property(asset => asset.ContentType).IsRequired().HasMaxLength(128);
            builder.Property(asset => asset.ExpectedSha256).IsRequired().HasMaxLength(64);
            builder.Property(asset => asset.VerifiedSha256).HasMaxLength(64);
            builder.Property(asset => asset.QuarantineStorageKey).IsRequired().HasMaxLength(1024);
            builder.Property(asset => asset.CleanStorageKey).HasMaxLength(1024);
            builder.Property(asset => asset.PublicStorageKey).HasMaxLength(1024);
            builder.Property(asset => asset.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(asset => asset.Visibility).HasConversion<string>().HasMaxLength(32);
            builder.Property(asset => asset.RejectionCode).HasMaxLength(100);
            builder.HasIndex(asset => asset.QuarantineStorageKey).IsUnique();
            builder.HasIndex(asset => asset.CleanStorageKey).IsUnique().HasFilter("\"CleanStorageKey\" IS NOT NULL");
            builder.HasIndex(asset => asset.PublicStorageKey).IsUnique().HasFilter("\"PublicStorageKey\" IS NOT NULL");
            builder.HasIndex(asset => new { asset.WorkspaceId, asset.Status, asset.CreatedAt });
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(asset => asset.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(asset => asset.OwnerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
    {
        public void Configure(EntityTypeBuilder<UploadSession> builder)
        {
            builder.ToTable("UploadSessions");
            builder.HasKey(session => session.Id);
            builder.Property(session => session.State).IsRequired().HasMaxLength(32);
            builder.Property(session => session.MultipartUploadId).HasMaxLength(1000);
            builder.Property(session => session.UploadedPartsJson).HasColumnType("jsonb");
            builder.HasIndex(session => new { session.AssetId, session.State });
            builder.HasIndex(session => session.ExpiresAt);
            builder.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(session => session.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class AssetDerivativeConfiguration : IEntityTypeConfiguration<AssetDerivative>
    {
        public void Configure(EntityTypeBuilder<AssetDerivative> builder)
        {
            builder.ToTable("AssetDerivatives");
            builder.HasKey(derivative => derivative.Id);
            builder.Property(derivative => derivative.Kind).IsRequired().HasMaxLength(64);
            builder.Property(derivative => derivative.StorageKey).IsRequired().HasMaxLength(1024);
            builder.Property(derivative => derivative.ContentType).IsRequired().HasMaxLength(128);
            builder.Property(derivative => derivative.MetadataJson).HasColumnType("jsonb");
            builder.HasIndex(derivative => derivative.StorageKey).IsUnique();
            builder.HasIndex(derivative => new { derivative.AssetId, derivative.Kind }).IsUnique();
            builder.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(derivative => derivative.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class AssetReferenceConfiguration : IEntityTypeConfiguration<AssetReference>
    {
        public void Configure(EntityTypeBuilder<AssetReference> builder)
        {
            builder.ToTable("AssetReferences");
            builder.HasKey(reference => reference.Id);
            builder.Property(reference => reference.EntityType).IsRequired().HasMaxLength(64);
            builder.Property(reference => reference.Purpose).IsRequired().HasMaxLength(64);
            builder.HasIndex(reference => new { reference.EntityType, reference.EntityId, reference.Purpose }).IsUnique();
            builder.HasIndex(reference => reference.AssetId);
            builder.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(reference => reference.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class AssetProcessingJobConfiguration : IEntityTypeConfiguration<AssetProcessingJob>
    {
        public void Configure(EntityTypeBuilder<AssetProcessingJob> builder)
        {
            builder.ToTable("AssetProcessingJobs");
            builder.HasKey(job => job.Id);
            builder.Property(job => job.Kind).IsRequired().HasMaxLength(64);
            builder.Property(job => job.State).IsRequired().HasMaxLength(16);
            builder.Property(job => job.LeaseOwner).HasMaxLength(200);
            builder.Property(job => job.ErrorCode).HasMaxLength(100);
            builder.Property(job => job.ErrorDetail).HasMaxLength(1000);
            builder.Property(job => job.CorrelationId).HasMaxLength(100);
            builder.HasIndex(job => new { job.State, job.AvailableAt });
            builder.HasIndex(job => new { job.AssetId, job.Kind });
            builder.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(job => job.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
