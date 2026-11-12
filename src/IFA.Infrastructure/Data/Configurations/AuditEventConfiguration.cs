using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
    {
        public void Configure(EntityTypeBuilder<AuditEvent> builder)
        {
            builder.ToTable("AuditEvents");
            builder.HasKey(audit => audit.Id);

            builder.Property(audit => audit.Action).IsRequired().HasMaxLength(128);
            builder.Property(audit => audit.ResourceType).IsRequired().HasMaxLength(128);
            builder.Property(audit => audit.ResourceId).IsRequired().HasMaxLength(200);
            builder.Property(audit => audit.Outcome).IsRequired().HasMaxLength(32);
            builder.Property(audit => audit.ErrorCode).HasMaxLength(100);
            builder.Property(audit => audit.CorrelationId).HasMaxLength(100);
            builder.Property(audit => audit.CausationId).HasMaxLength(100);
            builder.Property(audit => audit.MetadataJson).IsRequired().HasColumnType("jsonb");

            builder.HasIndex(audit => audit.OccurredAt);
            builder.HasIndex(audit => new { audit.ResourceType, audit.ResourceId });
            builder.HasIndex(audit => audit.CorrelationId);
        }
    }
}
