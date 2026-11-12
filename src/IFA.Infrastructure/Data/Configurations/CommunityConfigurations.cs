using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class WorkspaceFollowConfiguration : IEntityTypeConfiguration<WorkspaceFollow>
    {
        public void Configure(EntityTypeBuilder<WorkspaceFollow> builder)
        {
            builder.ToTable("WorkspaceFollows");
            builder.HasKey(follow => follow.Id);
            builder.HasIndex(follow => new { follow.WorkspaceId, follow.LearnerId }).IsUnique();
            builder.HasOne<EducatorWorkspace>().WithMany().HasForeignKey(follow => follow.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Learner>().WithMany().HasForeignKey(follow => follow.LearnerId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class CourseAnnouncementConfiguration : IEntityTypeConfiguration<CourseAnnouncement>
    {
        public void Configure(EntityTypeBuilder<CourseAnnouncement> builder)
        {
            builder.ToTable("CourseAnnouncements");
            builder.HasKey(announcement => announcement.Id);
            builder.Property(announcement => announcement.Title).IsRequired().HasMaxLength(200);
            builder.Property(announcement => announcement.BodyMarkdown).IsRequired().HasMaxLength(20_000);
            builder.Property(announcement => announcement.Audience).IsRequired().HasMaxLength(32);
            builder.HasIndex(announcement => new { announcement.CourseId, announcement.PublishedAt });
            builder.HasOne<Course>().WithMany().HasForeignKey(announcement => announcement.CourseId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CourseRelease>().WithMany().HasForeignKey(announcement => announcement.ReleaseId).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne<CourseCohort>().WithMany().HasForeignKey(announcement => announcement.CohortId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    public sealed class CourseReviewConfiguration : IEntityTypeConfiguration<CourseReview>
    {
        public void Configure(EntityTypeBuilder<CourseReview> builder)
        {
            builder.ToTable("CourseReviews", table => table.HasCheckConstraint("CK_CourseReviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
            builder.HasKey(review => review.Id);
            builder.Property(review => review.Body).IsRequired().HasMaxLength(5000);
            builder.Property(review => review.Status).IsRequired().HasMaxLength(32);
            builder.HasIndex(review => new { review.CourseId, review.LearnerId }).IsUnique();
            builder.HasIndex(review => new { review.CourseId, review.Status, review.CreatedAt });
            builder.HasOne<Course>().WithMany().HasForeignKey(review => review.CourseId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CourseRelease>().WithMany().HasForeignKey(review => review.ReleaseId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Learner>().WithMany().HasForeignKey(review => review.LearnerId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class CourseReviewReplyConfiguration : IEntityTypeConfiguration<CourseReviewReply>
    {
        public void Configure(EntityTypeBuilder<CourseReviewReply> builder)
        {
            builder.ToTable("CourseReviewReplies");
            builder.HasKey(reply => reply.Id);
            builder.Property(reply => reply.Body).IsRequired().HasMaxLength(5000);
            builder.Property(reply => reply.Status).IsRequired().HasMaxLength(32);
            builder.HasIndex(reply => new { reply.ReviewId, reply.AuthorAccountId }).IsUnique();
            builder.HasOne<CourseReview>().WithMany().HasForeignKey(reply => reply.ReviewId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class CourseReviewReportConfiguration : IEntityTypeConfiguration<CourseReviewReport>
    {
        public void Configure(EntityTypeBuilder<CourseReviewReport> builder)
        {
            builder.ToTable("CourseReviewReports");
            builder.HasKey(report => report.Id);
            builder.Property(report => report.Reason).IsRequired().HasMaxLength(64);
            builder.Property(report => report.Detail).HasMaxLength(2000);
            builder.Property(report => report.Status).IsRequired().HasMaxLength(32);
            builder.HasIndex(report => new { report.ReviewId, report.ReporterAccountId }).IsUnique();
            builder.HasIndex(report => new { report.Status, report.CreatedAt });
            builder.HasOne<CourseReview>().WithMany().HasForeignKey(report => report.ReviewId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ModerationCaseConfiguration : IEntityTypeConfiguration<ModerationCase>
    {
        public void Configure(EntityTypeBuilder<ModerationCase> builder)
        {
            builder.ToTable("ModerationCases");
            builder.HasKey(moderation => moderation.Id);
            builder.Property(moderation => moderation.ResourceType).IsRequired().HasMaxLength(64);
            builder.Property(moderation => moderation.Status).IsRequired().HasMaxLength(32);
            builder.Property(moderation => moderation.Priority).IsRequired().HasMaxLength(16);
            builder.Property(moderation => moderation.Resolution).HasMaxLength(2000);
            builder.HasIndex(moderation => new { moderation.ResourceType, moderation.ResourceId, moderation.Status });
            builder.HasIndex(moderation => new { moderation.Status, moderation.Priority, moderation.CreatedAt });
        }
    }
}
