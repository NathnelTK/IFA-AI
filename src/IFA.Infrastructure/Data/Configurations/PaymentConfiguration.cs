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

            builder.HasIndex(p => p.ProviderTxRef).IsUnique();
            builder.HasIndex(p => new { p.CourseId, p.LearnerId });

            builder.HasOne(p => p.Learner)
                .WithMany()
                .HasForeignKey(p => p.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Course)
                .WithMany()
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
