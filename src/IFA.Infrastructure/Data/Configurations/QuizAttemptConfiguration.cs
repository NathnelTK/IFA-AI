using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
    {
        public void Configure(EntityTypeBuilder<QuizAttempt> builder)
        {
            builder.ToTable("QuizAttempts");
            builder.HasKey(a => a.Id);

            // Deliberately NOT unique on (QuizId, LearnerId) — unlike
            // CourseEnrollment and LessonProgress, a learner CAN retake
            // a quiz, so multiple attempt rows per learner are expected.
            builder.HasIndex(a => new { a.QuizId, a.LearnerId });

            builder.HasOne(a => a.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Learner)
                .WithMany()
                .HasForeignKey(a => a.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class QuizAnswerConfiguration : IEntityTypeConfiguration<QuizAnswer>
    {
        public void Configure(EntityTypeBuilder<QuizAnswer> builder)
        {
            builder.ToTable("QuizAnswers");
            builder.HasKey(a => a.Id);

            builder.HasIndex(a => new { a.QuizAttemptId, a.QuestionId }).IsUnique();

            builder.HasOne(a => a.QuizAttempt)
                .WithMany(qa => qa.Answers)
                .HasForeignKey(a => a.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}