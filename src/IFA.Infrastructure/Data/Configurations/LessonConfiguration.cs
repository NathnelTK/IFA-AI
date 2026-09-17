using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
    {
        public void Configure(EntityTypeBuilder<Lesson> builder)
        {
            builder.ToTable("Lessons");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Title).IsRequired().HasMaxLength(200);
            builder.Property(l => l.Summary).HasMaxLength(2000);
            builder.Property(l => l.ContentMarkdown).HasMaxLength(20000);
            builder.Property(l => l.YouTubeVideoId).HasMaxLength(32);
            builder.Property(l => l.YouTubeVideoTitle).HasMaxLength(400);
            builder.Property(l => l.ScholarxivCitationDoi).HasMaxLength(200);
            builder.Property(l => l.ScholarxivPaperTitle).HasMaxLength(600);

            builder.HasIndex(l => new { l.ModuleId, l.LessonNumber }).IsUnique();

            builder.HasOne(l => l.Module)
                .WithMany(m => m.Lessons)
                .HasForeignKey(l => l.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
