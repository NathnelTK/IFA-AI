using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class CourseCompletionRecordConfiguration : IEntityTypeConfiguration<CourseCompletionRecord>
    {
        public void Configure(EntityTypeBuilder<CourseCompletionRecord> builder)
        {
            builder.ToTable("CourseCompletionRecords");
            builder.HasKey(record => record.Id);
            builder.Property(record => record.SnapshotJson).HasColumnType("jsonb");
            builder.Property(record => record.EvidenceHash).IsRequired().HasMaxLength(64);
            builder.HasIndex(record => record.EnrollmentId).IsUnique();
            builder.HasIndex(record => new { record.ReleaseId, record.LearnerId });
            builder.HasOne<CourseEnrollment>().WithMany().HasForeignKey(record => record.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CourseRelease>().WithMany().HasForeignKey(record => record.ReleaseId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class CourseCertificateConfiguration : IEntityTypeConfiguration<CourseCertificate>
    {
        public void Configure(EntityTypeBuilder<CourseCertificate> builder)
        {
            builder.ToTable("CourseCertificates");
            builder.HasKey(certificate => certificate.Id);
            builder.Property(certificate => certificate.SerialNumber).IsRequired().HasMaxLength(64);
            builder.Property(certificate => certificate.VerificationCode).IsRequired().HasMaxLength(64);
            builder.Property(certificate => certificate.Status).IsRequired().HasMaxLength(32);
            builder.Property(certificate => certificate.PdfStorageKey).HasMaxLength(1024);
            builder.Property(certificate => certificate.RenderingErrorCode).HasMaxLength(100);
            builder.Property(certificate => certificate.RevocationReason).HasMaxLength(1000);
            builder.HasIndex(certificate => certificate.CompletionRecordId).IsUnique();
            builder.HasIndex(certificate => certificate.SerialNumber).IsUnique();
            builder.HasIndex(certificate => certificate.VerificationCode).IsUnique();
            builder.HasOne<CourseCompletionRecord>().WithMany().HasForeignKey(certificate => certificate.CompletionRecordId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class CertificateRenderJobConfiguration : IEntityTypeConfiguration<CertificateRenderJob>
    {
        public void Configure(EntityTypeBuilder<CertificateRenderJob> builder)
        {
            builder.ToTable("CertificateRenderJobs");
            builder.HasKey(job => job.Id);
            builder.Property(job => job.State).IsRequired().HasMaxLength(16);
            builder.Property(job => job.LeaseOwner).HasMaxLength(200);
            builder.Property(job => job.ErrorCode).HasMaxLength(100);
            builder.Property(job => job.ErrorDetail).HasMaxLength(1000);
            builder.HasIndex(job => new { job.State, job.AvailableAt });
            builder.HasIndex(job => job.CertificateId).IsUnique();
            builder.HasOne<CourseCertificate>().WithMany().HasForeignKey(job => job.CertificateId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
