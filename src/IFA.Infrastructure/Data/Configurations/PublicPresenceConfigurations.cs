using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class EducatorPublicProfileConfiguration : IEntityTypeConfiguration<EducatorPublicProfile>
    {
        public void Configure(EntityTypeBuilder<EducatorPublicProfile> builder)
        {
            builder.ToTable("EducatorPublicProfiles");
            builder.HasKey(profile => profile.Id);
            builder.Property(profile => profile.Slug).IsRequired().HasMaxLength(100);
            builder.Property(profile => profile.DisplayName).IsRequired().HasMaxLength(160);
            builder.Property(profile => profile.Headline).HasMaxLength(300);
            builder.Property(profile => profile.Bio).HasMaxLength(5000);
            builder.Property(profile => profile.ExpertiseJson).HasColumnType("jsonb");
            builder.Property(profile => profile.LinksJson).HasColumnType("jsonb");
            builder.Property(profile => profile.ThemeJson).HasColumnType("jsonb");
            builder.HasIndex(profile => profile.WorkspaceId).IsUnique();
            builder.HasIndex(profile => profile.Slug).IsUnique();
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(profile => profile.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(profile => profile.PhotoAssetId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public sealed class OrganizationSiteConfiguration : IEntityTypeConfiguration<OrganizationSite>
    {
        public void Configure(EntityTypeBuilder<OrganizationSite> builder)
        {
            builder.ToTable("OrganizationSites");
            builder.HasKey(site => site.Id);
            builder.Property(site => site.Slug).IsRequired().HasMaxLength(100);
            builder.Property(site => site.Name).IsRequired().HasMaxLength(160);
            builder.Property(site => site.ThemeJson).HasColumnType("jsonb");
            builder.Property(site => site.SeoJson).HasColumnType("jsonb");
            builder.HasIndex(site => site.WorkspaceId).IsUnique();
            builder.HasIndex(site => site.Slug).IsUnique();
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(site => site.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(site => site.LogoAssetId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public sealed class OrganizationSitePageConfiguration : IEntityTypeConfiguration<OrganizationSitePage>
    {
        public void Configure(EntityTypeBuilder<OrganizationSitePage> builder)
        {
            builder.ToTable("OrganizationSitePages");
            builder.HasKey(page => page.Id);
            builder.Property(page => page.PageType).HasConversion<string>().HasMaxLength(32);
            builder.Property(page => page.Title).IsRequired().HasMaxLength(160);
            builder.Property(page => page.SchemaJson).HasColumnType("jsonb");
            builder.HasIndex(page => new { page.OrganizationSiteId, page.PageType }).IsUnique();
            builder.HasIndex(page => new { page.OrganizationSiteId, page.IsVisible, page.NavigationOrder });
            builder.HasOne<OrganizationSite>()
                .WithMany()
                .HasForeignKey(page => page.OrganizationSiteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class CustomDomainConfiguration : IEntityTypeConfiguration<CustomDomain>
    {
        public void Configure(EntityTypeBuilder<CustomDomain> builder)
        {
            builder.ToTable("CustomDomains");
            builder.HasKey(domain => domain.Id);
            builder.Property(domain => domain.Host).IsRequired().HasMaxLength(253);
            builder.Property(domain => domain.Status).IsRequired().HasMaxLength(32);
            builder.Property(domain => domain.VerificationToken).IsRequired().HasMaxLength(200);
            builder.Property(domain => domain.ExpectedCnameTarget).IsRequired().HasMaxLength(253);
            builder.Property(domain => domain.FailureCode).HasMaxLength(100);
            builder.Property(domain => domain.LeaseOwner).HasMaxLength(200);
            builder.HasIndex(domain => domain.Host).IsUnique();
            builder.HasIndex(domain => new { domain.Status, domain.NextCheckAt });
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(domain => domain.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class VerifiedExternalWebsiteConfiguration : IEntityTypeConfiguration<VerifiedExternalWebsite>
    {
        public void Configure(EntityTypeBuilder<VerifiedExternalWebsite> builder)
        {
            builder.ToTable("VerifiedExternalWebsites");
            builder.HasKey(site => site.Id);
            builder.Property(site => site.Url).IsRequired().HasMaxLength(2048);
            builder.Property(site => site.Host).IsRequired().HasMaxLength(253);
            builder.Property(site => site.Status).IsRequired().HasMaxLength(32);
            builder.Property(site => site.VerificationToken).IsRequired().HasMaxLength(200);
            builder.Property(site => site.FailureCode).HasMaxLength(100);
            builder.Property(site => site.LeaseOwner).HasMaxLength(200);
            builder.HasIndex(site => new { site.WorkspaceId, site.Host }).IsUnique();
            builder.HasIndex(site => new { site.Status, site.NextCheckAt });
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(site => site.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
