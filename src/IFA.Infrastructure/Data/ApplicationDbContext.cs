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

        // New sets, backing the entities added this round:
        public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
        public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
        public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
        public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
        public DbSet<ResearchPackage> ResearchPackages => Set<ResearchPackage>();

        // IApplicationDbContext now declares IQueryable<T>, and DbSet<T>
        // already IS an IQueryable<T> natively — so these explicit
        // implementations no longer downcast anything. They just forward
        // the same object the interface asked for, unmodified.
        IQueryable<Learner> IApplicationDbContext.Learners => Learners;
        IQueryable<Course> IApplicationDbContext.Courses => Courses;
        IQueryable<Module> IApplicationDbContext.Modules => Modules;
        IQueryable<Lesson> IApplicationDbContext.Lessons => Lessons;
        IQueryable<Quiz> IApplicationDbContext.Quizzes => Quizzes;
        IQueryable<Question> IApplicationDbContext.Questions => Questions;
        IQueryable<SkillMetric> IApplicationDbContext.SkillMetrics => SkillMetrics;
        IQueryable<CourseEnrollment> IApplicationDbContext.CourseEnrollments => CourseEnrollments;
        IQueryable<CourseShareInvite> IApplicationDbContext.CourseShareInvites => CourseShareInvites;
        IQueryable<LessonProgress> IApplicationDbContext.LessonProgress => LessonProgress;
        IQueryable<QuizAttempt> IApplicationDbContext.QuizAttempts => QuizAttempts;
        IQueryable<QuizAnswer> IApplicationDbContext.QuizAnswers => QuizAnswers;
        IQueryable<LearnerProfile> IApplicationDbContext.LearnerProfiles => LearnerProfiles;
        IQueryable<ResearchPackage> IApplicationDbContext.ResearchPackages => ResearchPackages;
        void IApplicationDbContext.Add<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Add(entity);
        void IApplicationDbContext.Remove<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Remove(entity);
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}