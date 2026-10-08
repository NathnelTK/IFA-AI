using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
    {
        public void Configure(EntityTypeBuilder<Assessment> builder)
        {
            builder.ToTable("Assessments");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.AttemptNumber).HasDefaultValue(1);

            builder.HasOne(a => a.Learner)
                .WithMany(l => l.Assessments)
                .HasForeignKey(a => a.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Quiz)
                .WithMany()
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(a => a.Responses)
                .WithOne(r => r.Assessment)
                .HasForeignKey(r => r.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
