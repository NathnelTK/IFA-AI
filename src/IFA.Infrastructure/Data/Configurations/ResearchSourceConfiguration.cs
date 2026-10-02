using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class ResearchSourceConfiguration : IEntityTypeConfiguration<ResearchSource>
    {
        public void Configure(EntityTypeBuilder<ResearchSource> builder)
        {
            builder.ToTable("ResearchSources");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Title).IsRequired().HasMaxLength(300);
            builder.Property(s => s.Url).IsRequired().HasMaxLength(1000);
            builder.Property(s => s.SourceType).IsRequired().HasMaxLength(50);
            builder.Property(s => s.Authors).HasMaxLength(300);

            builder.HasOne(s => s.ResearchPackage)
                .WithMany(r => r.Sources)
                .HasForeignKey(s => s.ResearchPackageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
