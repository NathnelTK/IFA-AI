using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.ToTable("Questions");
            builder.HasKey(q => q.Id);

            builder.Property(q => q.Prompt).IsRequired().HasMaxLength(2000);
            builder.Property(q => q.Explanation).HasMaxLength(4000);
            builder.Property(q => q.TargetSkillName).HasMaxLength(160);
            builder.Property(q => q.BloomTaxonomyLevel).HasMaxLength(64);

            // Answer options are persisted as a PostgreSQL text[] column.
            builder.Property(q => q.Options).HasColumnType("text[]");

            builder.HasOne(q => q.Quiz)
                .WithMany(quiz => quiz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
