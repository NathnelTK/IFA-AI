using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations;

public class VideoResourceConfiguration : IEntityTypeConfiguration<VideoResource>
{
    public void Configure(EntityTypeBuilder<VideoResource> builder)
    {
        builder.HasKey(vr => vr.Id);

        builder.Property(vr => vr.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(vr => vr.Description)
            .HasMaxLength(2000);

        builder.Property(vr => vr.YouTubeVideoId)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(vr => vr.ChannelName)
            .HasMaxLength(200);

        builder.Property(vr => vr.ThumbnailUrl)
            .HasMaxLength(2000);

        builder.Property(vr => vr.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(vr => vr.ResearchPackage)
            .WithMany()
            .HasForeignKey(vr => vr.ResearchPackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}