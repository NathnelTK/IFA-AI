using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class LearnerStateConfiguration : IEntityTypeConfiguration<LearnerState>
    {
        public void Configure(EntityTypeBuilder<LearnerState> builder)
        {
            builder.ToTable("LearnerStates");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.ActiveStreakDays).HasDefaultValue(1);
            builder.Property(s => s.TotalHoursLearned).HasDefaultValue(0.0);
            builder.Property(s => s.CompletedLessonsCount).HasDefaultValue(0);
            builder.Property(s => s.CompletedQuizzesCount).HasDefaultValue(0);

            builder.HasOne(s => s.Learner)
                .WithOne(l => l.State)
                .HasForeignKey<LearnerState>(s => s.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
