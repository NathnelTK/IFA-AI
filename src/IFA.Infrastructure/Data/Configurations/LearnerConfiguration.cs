using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public class LearnerConfiguration : IEntityTypeConfiguration<Learner>
    {
        public void Configure(EntityTypeBuilder<Learner> builder)
        {
            builder.ToTable("Learners");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Name).IsRequired().HasMaxLength(160);
            builder.Property(l => l.Email).IsRequired().HasMaxLength(256);
            builder.Property(l => l.PasswordHash).HasMaxLength(512);
            builder.Property(l => l.Role).HasMaxLength(64).HasDefaultValue("Learner");
            builder.Property(l => l.AvatarUrl).HasMaxLength(512);
            builder.Property(l => l.OverallProgress).HasDefaultValue(0);

            builder.HasIndex(l => l.Email).IsUnique();
        }
    }
}
