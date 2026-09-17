using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Data
{
    /// <summary>
    /// EF Core implementation of the application persistence contract.
    /// Entity mapping details live in the IEntityTypeConfiguration classes
    /// discovered from this assembly.
    /// </summary>
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Learner> Learners => Set<Learner>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Module> Modules => Set<Module>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<SkillMetric> SkillMetrics => Set<SkillMetric>();
        public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
        public DbSet<CourseShareInvite> CourseShareInvites => Set<CourseShareInvite>();

        IEnumerable<Learner> IApplicationDbContext.Learners => Learners;
        IEnumerable<Course> IApplicationDbContext.Courses => Courses;
        IEnumerable<Module> IApplicationDbContext.Modules => Modules;
        IEnumerable<Lesson> IApplicationDbContext.Lessons => Lessons;
        IEnumerable<Quiz> IApplicationDbContext.Quizzes => Quizzes;
        IEnumerable<Question> IApplicationDbContext.Questions => Questions;
        IEnumerable<SkillMetric> IApplicationDbContext.SkillMetrics => SkillMetrics;
        IEnumerable<CourseEnrollment> IApplicationDbContext.CourseEnrollments => CourseEnrollments;
        IEnumerable<CourseShareInvite> IApplicationDbContext.CourseShareInvites => CourseShareInvites;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
