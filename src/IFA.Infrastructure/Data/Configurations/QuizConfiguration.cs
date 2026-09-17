using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
    {
        public void Configure(EntityTypeBuilder<Quiz> builder)
        {
            builder.ToTable("Quizzes");
            builder.HasKey(q => q.Id);

            builder.Property(q => q.Title).IsRequired().HasMaxLength(200);
            builder.Property(q => q.PassingScorePercentage).HasDefaultValue(70);

            // Exactly one module test (quiz) per module.
            builder.HasOne(q => q.Module)
                .WithOne(m => m.ModuleQuiz)
                .HasForeignKey<Quiz>(q => q.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
