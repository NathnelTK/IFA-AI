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
            builder.Property(q => q.Kind).HasConversion<int>();

            // A module has several assessments (two mini-quizzes + one exam), so
            // this is a one-to-many relationship keyed on ModuleId.
            builder.HasOne(q => q.Module)
                .WithMany(m => m.Quizzes)
                .HasForeignKey(q => q.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
