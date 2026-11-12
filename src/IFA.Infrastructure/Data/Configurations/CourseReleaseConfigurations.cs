using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class CourseReleaseConfiguration : IEntityTypeConfiguration<CourseRelease>
    {
        public void Configure(EntityTypeBuilder<CourseRelease> builder)
        {
            builder.ToTable("CourseReleases");
            builder.HasKey(release => release.Id);
            builder.Property(release => release.Title).IsRequired().HasMaxLength(200);
            builder.Property(release => release.Slug).IsRequired().HasMaxLength(120);
            builder.Property(release => release.SnapshotJson).HasColumnType("jsonb");
            builder.Property(release => release.ContentHash).IsRequired().HasMaxLength(64);
            builder.Property(release => release.Status).IsRequired().HasMaxLength(32);
            builder.HasIndex(release => new { release.CourseId, release.Version }).IsUnique();
            builder.HasIndex(release => release.ContentHash);
            builder.HasOne<Course>()
                .WithMany()
                .HasForeignKey(release => release.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CourseRelease>()
                .WithMany()
                .HasForeignKey(release => release.SupersedesReleaseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class ReleaseModuleConfiguration : IEntityTypeConfiguration<ReleaseModule>
    {
        public void Configure(EntityTypeBuilder<ReleaseModule> builder)
        {
            builder.ToTable("ReleaseModules");
            builder.HasKey(module => module.Id);
            builder.Property(module => module.Title).IsRequired().HasMaxLength(200);
            builder.Property(module => module.Summary).HasMaxLength(2000);
            builder.HasIndex(module => new { module.ReleaseId, module.ModuleNumber }).IsUnique();
            builder.HasIndex(module => new { module.ReleaseId, module.SourceModuleId }).IsUnique();
            builder.HasOne<CourseRelease>()
                .WithMany()
                .HasForeignKey(module => module.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ReleaseLessonConfiguration : IEntityTypeConfiguration<ReleaseLesson>
    {
        public void Configure(EntityTypeBuilder<ReleaseLesson> builder)
        {
            builder.ToTable("ReleaseLessons");
            builder.HasKey(lesson => lesson.Id);
            builder.Property(lesson => lesson.Title).IsRequired().HasMaxLength(200);
            builder.Property(lesson => lesson.Summary).HasMaxLength(2000);
            builder.Property(lesson => lesson.YouTubeVideoId).HasMaxLength(64);
            builder.Property(lesson => lesson.AssetReferencesJson).HasColumnType("jsonb");
            builder.HasIndex(lesson => new { lesson.ReleaseModuleId, lesson.LessonNumber }).IsUnique();
            builder.HasIndex(lesson => new { lesson.ReleaseModuleId, lesson.SourceLessonId }).IsUnique();
            builder.HasOne<ReleaseModule>()
                .WithMany()
                .HasForeignKey(lesson => lesson.ReleaseModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ReleaseQuizConfiguration : IEntityTypeConfiguration<ReleaseQuiz>
    {
        public void Configure(EntityTypeBuilder<ReleaseQuiz> builder)
        {
            builder.ToTable("ReleaseQuizzes");
            builder.HasKey(quiz => quiz.Id);
            builder.Property(quiz => quiz.Title).IsRequired().HasMaxLength(200);
            builder.Property(quiz => quiz.Kind).IsRequired().HasMaxLength(16);
            builder.Property(quiz => quiz.DefinitionJson).HasColumnType("jsonb");
            builder.HasIndex(quiz => new { quiz.ReleaseModuleId, quiz.OrderIndex }).IsUnique();
            builder.HasIndex(quiz => new { quiz.ReleaseModuleId, quiz.SourceQuizId }).IsUnique();
            builder.HasOne<ReleaseModule>()
                .WithMany()
                .HasForeignKey(quiz => quiz.ReleaseModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ReleaseAssignmentConfiguration : IEntityTypeConfiguration<ReleaseAssignment>
    {
        public void Configure(EntityTypeBuilder<ReleaseAssignment> builder)
        {
            builder.ToTable("ReleaseAssignments");
            builder.HasKey(assignment => assignment.Id);
            builder.Property(assignment => assignment.Title).IsRequired().HasMaxLength(200);
            builder.Property(assignment => assignment.AllowedFileTypes).HasMaxLength(256);
            builder.Property(assignment => assignment.RubricCriteriaJson).HasColumnType("jsonb");
            builder.HasIndex(assignment => new { assignment.ReleaseId, assignment.SourceAssignmentId }).IsUnique();
            builder.HasOne<CourseRelease>()
                .WithMany()
                .HasForeignKey(assignment => assignment.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<ReleaseModule>()
                .WithMany()
                .HasForeignKey(assignment => assignment.ReleaseModuleId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public sealed class CourseCohortConfiguration : IEntityTypeConfiguration<CourseCohort>
    {
        public void Configure(EntityTypeBuilder<CourseCohort> builder)
        {
            builder.ToTable("CourseCohorts");
            builder.HasKey(cohort => cohort.Id);
            builder.Property(cohort => cohort.Name).IsRequired().HasMaxLength(200);
            builder.Property(cohort => cohort.Status).IsRequired().HasMaxLength(32);
            builder.HasIndex(cohort => new { cohort.CourseId, cohort.Status });
            builder.HasIndex(cohort => cohort.ReleaseId);
            builder.HasOne<Course>()
                .WithMany()
                .HasForeignKey(cohort => cohort.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CourseRelease>()
                .WithMany()
                .HasForeignKey(cohort => cohort.ReleaseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class ReleaseItemProgressConfiguration : IEntityTypeConfiguration<ReleaseItemProgress>
    {
        public void Configure(EntityTypeBuilder<ReleaseItemProgress> builder)
        {
            builder.ToTable("ReleaseItemProgress");
            builder.HasKey(progress => progress.Id);
            builder.Property(progress => progress.ItemType).IsRequired().HasMaxLength(32);
            builder.Property(progress => progress.State).IsRequired().HasMaxLength(32);
            builder.Property(progress => progress.ProgressPercent).HasPrecision(5, 2);
            builder.Property(progress => progress.EvidenceJson).HasColumnType("jsonb");
            builder.HasIndex(progress => new { progress.EnrollmentId, progress.ItemType, progress.ItemId }).IsUnique();
            builder.HasIndex(progress => new { progress.ReleaseId, progress.LearnerId });
            builder.HasOne<CourseEnrollment>()
                .WithMany()
                .HasForeignKey(progress => progress.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<CourseRelease>()
                .WithMany()
                .HasForeignKey(progress => progress.ReleaseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class ProgressMigrationRecordConfiguration : IEntityTypeConfiguration<ProgressMigrationRecord>
    {
        public void Configure(EntityTypeBuilder<ProgressMigrationRecord> builder)
        {
            builder.ToTable("ProgressMigrationRecords");
            builder.HasKey(record => record.Id);
            builder.Property(record => record.SourceType).IsRequired().HasMaxLength(32);
            builder.Property(record => record.Outcome).IsRequired().HasMaxLength(32);
            builder.Property(record => record.DetailJson).HasColumnType("jsonb");
            builder.HasIndex(record => new { record.SourceType, record.SourceId }).IsUnique();
            builder.HasIndex(record => record.EnrollmentId);
        }
    }

    public sealed class ReleaseQuizAttemptConfiguration : IEntityTypeConfiguration<ReleaseQuizAttempt>
    {
        public void Configure(EntityTypeBuilder<ReleaseQuizAttempt> builder)
        {
            builder.ToTable("ReleaseQuizAttempts");
            builder.HasKey(attempt => attempt.Id);
            builder.Property(attempt => attempt.AnswersJson).HasColumnType("jsonb");
            builder.HasIndex(attempt => new { attempt.EnrollmentId, attempt.ReleaseQuizId, attempt.AttemptedAt });
            builder.HasOne<CourseEnrollment>()
                .WithMany()
                .HasForeignKey(attempt => attempt.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<ReleaseQuiz>()
                .WithMany()
                .HasForeignKey(attempt => attempt.ReleaseQuizId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
