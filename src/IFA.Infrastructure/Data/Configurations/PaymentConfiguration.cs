using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount).HasPrecision(12, 2);
            builder.Property(p => p.Currency).HasMaxLength(8);
            builder.Property(p => p.Provider).HasMaxLength(32);
            builder.Property(p => p.ProviderTxRef).IsRequired().HasMaxLength(128);
            builder.Property(p => p.Status).HasMaxLength(16);
            builder.Property(p => p.CheckoutUrl).HasMaxLength(2048);
            builder.Property(p => p.ReturnUrl).HasMaxLength(2048);
            builder.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(128);
            builder.Property(p => p.ProviderTransactionId).HasMaxLength(256);
            builder.Property(p => p.VerificationPayloadHash).HasMaxLength(64);
            builder.Property(p => p.LeaseOwner).HasMaxLength(200);
            builder.Property(p => p.FailureCode).HasMaxLength(100);

            builder.HasIndex(p => p.ProviderTxRef).IsUnique();
            builder.HasIndex(p => new { p.CourseId, p.LearnerId });
            builder.HasIndex(p => p.IdempotencyKey).IsUnique();
            builder.HasIndex(p => new { p.Status, p.NextVerificationAt });
            builder.HasIndex(p => new { p.CourseId, p.LearnerId, p.Status })
                .HasDatabaseName("IX_Payments_OneOpenCheckout")
                .HasFilter("\"Status\" IN ('Pending', 'Initializing')")
                .IsUnique();

            builder.HasOne(p => p.Learner)
                .WithMany()
                .HasForeignKey(p => p.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<CourseOffer>()
                .WithMany()
                .HasForeignKey(p => p.OfferId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CourseRelease>()
                .WithMany()
                .HasForeignKey(p => p.ReleaseId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CourseCohort>()
                .WithMany()
                .HasForeignKey(p => p.CohortId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(p => p.Course)
                .WithMany()
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
