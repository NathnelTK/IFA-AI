using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class ProductActivityEventConfiguration : IEntityTypeConfiguration<ProductActivityEvent>
    {
        public void Configure(EntityTypeBuilder<ProductActivityEvent> builder)
        {
            builder.ToTable("ProductActivityEvents");
            builder.HasKey(activity => activity.Id);
            builder.Property(activity => activity.EventType).IsRequired().HasMaxLength(128);
            builder.Property(activity => activity.CorrelationId).HasMaxLength(100);
            builder.Property(activity => activity.MetadataJson).HasColumnType("jsonb");
            builder.HasIndex(activity => new { activity.WorkspaceId, activity.EventType, activity.OccurredAt });
            builder.HasIndex(activity => new { activity.CourseId, activity.EventType, activity.OccurredAt });
            builder.HasIndex(activity => new { activity.ActorAccountId, activity.OccurredAt });
        }
    }
}
