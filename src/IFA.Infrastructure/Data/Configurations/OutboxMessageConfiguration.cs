using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(message => message.Id);

            builder.Property(message => message.Type).IsRequired().HasMaxLength(128);
            builder.Property(message => message.PayloadJson).IsRequired().HasColumnType("jsonb");
            builder.Property(message => message.AggregateType).HasMaxLength(128);
            builder.Property(message => message.DeduplicationKey).IsRequired().HasMaxLength(200);
            builder.Property(message => message.LeaseOwner).HasMaxLength(200);
            builder.Property(message => message.LastErrorCode).HasMaxLength(100);
            builder.Property(message => message.LastError).HasMaxLength(1000);
            builder.Property(message => message.CorrelationId).HasMaxLength(100);
            builder.Property(message => message.CausationId).HasMaxLength(100);

            builder.HasIndex(message => message.DeduplicationKey).IsUnique();
            builder.HasIndex(message => new { message.ProcessedAt, message.DeadLetteredAt, message.AvailableAt });
            builder.HasIndex(message => message.LeaseExpiresAt);
        }
    }
}
