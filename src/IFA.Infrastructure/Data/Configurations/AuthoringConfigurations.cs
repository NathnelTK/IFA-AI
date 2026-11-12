using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class AuthoringProjectConfiguration : IEntityTypeConfiguration<AuthoringProject>
    {
        public void Configure(EntityTypeBuilder<AuthoringProject> builder)
        {
            builder.ToTable("AuthoringProjects");
            builder.HasKey(project => project.Id);
            builder.Property(project => project.Mode).HasConversion<string>().HasMaxLength(16);
            builder.Property(project => project.State).HasConversion<string>().HasMaxLength(32);
            builder.Property(project => project.Title).IsRequired().HasMaxLength(300);
            builder.Property(project => project.FailureCode).HasMaxLength(100);
            builder.Property(project => project.FailureDetail).HasMaxLength(1000);
            builder.Property(project => project.Version).IsConcurrencyToken();
            builder.HasIndex(project => project.CourseId).IsUnique();
            builder.HasIndex(project => project.LegacyEducatorProjectId).IsUnique();
            builder.HasIndex(project => new { project.WorkspaceId, project.State, project.UpdatedAt });
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(project => project.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Course>()
                .WithMany()
                .HasForeignKey(project => project.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class CourseGenerationRequestConfiguration : IEntityTypeConfiguration<CourseGenerationRequest>
    {
        public void Configure(EntityTypeBuilder<CourseGenerationRequest> builder)
        {
            builder.ToTable("CourseGenerationRequests");
            builder.HasKey(request => request.Id);
            builder.Property(request => request.Prompt).IsRequired().HasMaxLength(8000);
            builder.Property(request => request.Category).IsRequired().HasMaxLength(100);
            builder.Property(request => request.TargetAudience).IsRequired().HasMaxLength(300);
            builder.Property(request => request.LearningObjectivesJson).HasColumnType("jsonb");
            builder.Property(request => request.ConstraintsJson).HasColumnType("jsonb");
            builder.Property(request => request.TemplateId).HasMaxLength(100);
            builder.HasIndex(request => request.AuthoringProjectId).IsUnique();
            builder.HasOne<AuthoringProject>()
                .WithMany()
                .HasForeignKey(request => request.AuthoringProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class AuthoringSourceConfiguration : IEntityTypeConfiguration<AuthoringSource>
    {
        public void Configure(EntityTypeBuilder<AuthoringSource> builder)
        {
            builder.ToTable("AuthoringSources");
            builder.HasKey(source => source.Id);
            builder.Property(source => source.Kind).HasConversion<string>().HasMaxLength(16);
            builder.Property(source => source.Locator).IsRequired().HasMaxLength(2048);
            builder.Property(source => source.Status).IsRequired().HasMaxLength(32);
            builder.Property(source => source.MetadataJson).HasColumnType("jsonb");
            builder.Property(source => source.ExtractedTextReference).HasMaxLength(2048);
            builder.Property(source => source.ErrorCode).HasMaxLength(100);
            builder.HasIndex(source => source.LegacyProjectSourceId).IsUnique();
            builder.HasIndex(source => new { source.AuthoringProjectId, source.Status });
            builder.HasOne<AuthoringProject>()
                .WithMany()
                .HasForeignKey(source => source.AuthoringProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(source => source.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class AuthoringJobConfiguration : IEntityTypeConfiguration<AuthoringJob>
    {
        public void Configure(EntityTypeBuilder<AuthoringJob> builder)
        {
            builder.ToTable("AuthoringJobs");
            builder.HasKey(job => job.Id);
            builder.Property(job => job.Kind).IsRequired().HasMaxLength(64);
            builder.Property(job => job.State).HasConversion<string>().HasMaxLength(16);
            builder.Property(job => job.PayloadJson).HasColumnType("jsonb");
            builder.Property(job => job.ResultJson).HasColumnType("jsonb");
            builder.Property(job => job.LeaseOwner).HasMaxLength(200);
            builder.Property(job => job.ErrorCode).HasMaxLength(100);
            builder.Property(job => job.ErrorDetail).HasMaxLength(1000);
            builder.Property(job => job.CorrelationId).HasMaxLength(100);
            builder.Property(job => job.CausationId).HasMaxLength(100);
            builder.HasIndex(job => new { job.State, job.AvailableAt });
            builder.HasIndex(job => new { job.AuthoringProjectId, job.State });
            builder.HasIndex(job => job.LeaseExpiresAt);
            builder.HasOne<AuthoringProject>()
                .WithMany()
                .HasForeignKey(job => job.AuthoringProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<AuthoringJob>()
                .WithMany()
                .HasForeignKey(job => job.ParentJobId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class LegacyProjectAliasConfiguration : IEntityTypeConfiguration<LegacyProjectAlias>
    {
        public void Configure(EntityTypeBuilder<LegacyProjectAlias> builder)
        {
            builder.ToTable("LegacyProjectAliases");
            builder.HasKey(alias => alias.LegacyProjectId);
            builder.HasIndex(alias => alias.AuthoringProjectId);
            builder.HasOne<AuthoringProject>()
                .WithMany()
                .HasForeignKey(alias => alias.AuthoringProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class CourseTemplateVersionConfiguration : IEntityTypeConfiguration<CourseTemplateVersion>
    {
        public void Configure(EntityTypeBuilder<CourseTemplateVersion> builder)
        {
            builder.ToTable("CourseTemplateVersions");
            builder.HasKey(template => template.Id);
            builder.Property(template => template.TemplateKey).IsRequired().HasMaxLength(100);
            builder.Property(template => template.Name).IsRequired().HasMaxLength(200);
            builder.Property(template => template.Category).IsRequired().HasMaxLength(100);
            builder.Property(template => template.DefinitionJson).HasColumnType("jsonb");
            builder.HasIndex(template => new { template.TemplateKey, template.Version }).IsUnique();
        }
    }
}
