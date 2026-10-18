using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class CourseBlueprintConfiguration : IEntityTypeConfiguration<CourseBlueprint>
    {
        public void Configure(EntityTypeBuilder<CourseBlueprint> builder)
        {
            builder.ToTable("CourseBlueprints");
            builder.HasKey(b => b.Id);

            // Full module/chapter/source map — intentionally unbounded.
            builder.Property(b => b.Json);

            builder.HasIndex(b => b.ProjectId).IsUnique();
        }
    }
}
