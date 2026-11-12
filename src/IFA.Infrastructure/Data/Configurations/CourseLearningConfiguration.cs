using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations;

public class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.ToTable("LiveSessions");
        builder.Property(s => s.Title).HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.TimeZone).HasMaxLength(64);
        builder.Property(s => s.MeetingUrl).HasMaxLength(2048);
        builder.Property(s => s.Status).HasMaxLength(32);
        builder.HasOne(s => s.Course).WithMany(c => c.LiveSessions).HasForeignKey(s => s.CourseId);
        builder.HasOne<CourseRelease>().WithMany().HasForeignKey(s => s.ReleaseId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<CourseCohort>().WithMany().HasForeignKey(s => s.CohortId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CourseDiscussionConfiguration : IEntityTypeConfiguration<CourseDiscussion>
{
    public void Configure(EntityTypeBuilder<CourseDiscussion> builder)
    {
        builder.ToTable("CourseDiscussions");
        builder.Property(d => d.Title).HasMaxLength(200);
        builder.HasOne(d => d.Course).WithMany(c => c.Discussions).HasForeignKey(d => d.CourseId);
        builder.HasOne(d => d.Lesson).WithMany().HasForeignKey(d => d.LessonId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(d => d.Learner).WithMany().HasForeignKey(d => d.LearnerId);
        builder.HasOne(d => d.Parent).WithMany(d => d.Replies).HasForeignKey(d => d.ParentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");
        builder.Property(a => a.Title).HasMaxLength(200);
        builder.Property(a => a.AllowedFileTypes).HasMaxLength(256);
        builder.HasOne(a => a.Course).WithMany(c => c.Assignments).HasForeignKey(a => a.CourseId);
        builder.HasOne(a => a.Module).WithMany().HasForeignKey(a => a.ModuleId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.Lesson).WithMany().HasForeignKey(a => a.LessonId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("AssignmentSubmissions");
        builder.Property(s => s.FileUrl).HasMaxLength(2048);
        builder.Property(s => s.FileName).HasMaxLength(256);
        builder.Property(s => s.Status).HasMaxLength(32);
        builder.HasOne(s => s.Assignment).WithMany(a => a.Submissions).HasForeignKey(s => s.AssignmentId);
        builder.HasOne(s => s.Learner).WithMany().HasForeignKey(s => s.LearnerId);
        builder.HasOne<Asset>().WithMany().HasForeignKey(s => s.AssetId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<ReleaseAssignment>().WithMany().HasForeignKey(s => s.ReleaseAssignmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(s => new { s.AssignmentId, s.LearnerId }).IsUnique();
    }
}
