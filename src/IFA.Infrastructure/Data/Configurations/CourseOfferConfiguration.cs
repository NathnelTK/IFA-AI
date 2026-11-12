using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class CourseOfferConfiguration : IEntityTypeConfiguration<CourseOffer>
    {
        public void Configure(EntityTypeBuilder<CourseOffer> builder)
        {
            builder.ToTable("CourseOffers");
            builder.HasKey(offer => offer.Id);
            builder.Property(offer => offer.Name).IsRequired().HasMaxLength(200);
            builder.Property(offer => offer.PurchaseType).IsRequired().HasMaxLength(32);
            builder.Property(offer => offer.PriceAmount).HasPrecision(12, 2);
            builder.Property(offer => offer.Currency).IsRequired().HasMaxLength(8);
            builder.Property(offer => offer.TermsJson).HasColumnType("jsonb");
            builder.HasIndex(offer => new { offer.CourseId, offer.Version }).IsUnique();
            builder.HasIndex(offer => new { offer.CourseId, offer.IsActive, offer.AvailableFrom, offer.AvailableUntil });
            builder.HasOne<Course>().WithMany().HasForeignKey(offer => offer.CourseId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CourseRelease>().WithMany().HasForeignKey(offer => offer.ReleaseId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CourseCohort>().WithMany().HasForeignKey(offer => offer.CohortId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    public sealed class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
    {
        public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
        {
            builder.ToTable("PaymentWebhookEvents");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Provider).IsRequired().HasMaxLength(32);
            builder.Property(item => item.EventKey).IsRequired().HasMaxLength(256);
            builder.Property(item => item.PayloadHash).IsRequired().HasMaxLength(64);
            builder.Property(item => item.Status).IsRequired().HasMaxLength(32);
            builder.HasIndex(item => new { item.Provider, item.EventKey }).IsUnique();
            builder.HasIndex(item => item.ReceivedAt);
        }
    }
}
