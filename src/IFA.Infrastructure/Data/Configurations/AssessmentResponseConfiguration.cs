using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class AssessmentResponseConfiguration : IEntityTypeConfiguration<AssessmentResponse>
    {
        public void Configure(EntityTypeBuilder<AssessmentResponse> builder)
        {
            builder.ToTable("AssessmentResponses");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Feedback).HasMaxLength(1000);

            builder.HasOne(r => r.Assessment)
                .WithMany(a => a.Responses)
                .HasForeignKey(r => r.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Question)
                .WithMany()
                .HasForeignKey(r => r.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
