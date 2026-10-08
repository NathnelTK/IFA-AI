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

            builder.Property(r => r.Topic).IsRequired().HasMaxLength(300);
            builder.Property(r => r.Summary).IsRequired();

            builder.HasOne(r => r.Course)
                .WithMany(c => c.ResearchPackages)
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(r => r.Sources)
                .WithOne(s => s.ResearchPackage)
                .HasForeignKey(s => s.ResearchPackageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
