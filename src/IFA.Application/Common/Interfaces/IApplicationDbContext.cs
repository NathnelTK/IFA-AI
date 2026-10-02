using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        IQueryable<Learner> Learners { get; }
        IQueryable<Course> Courses { get; }
        IQueryable<Module> Modules { get; }
        IQueryable<Lesson> Lessons { get; }
        IQueryable<Quiz> Quizzes { get; }
        IQueryable<Question> Questions { get; }
        IQueryable<SkillMetric> SkillMetrics { get; }
        IQueryable<CourseEnrollment> CourseEnrollments { get; }
        IQueryable<CourseShareInvite> CourseShareInvites { get; }
        IQueryable<QuizAttempt> QuizAttempts { get; }
        IQueryable<QuizAnswer> QuizAnswers { get; }
        IQueryable<LessonProgress> LessonProgress { get; }
        IQueryable<LearnerProfile> LearnerProfiles { get; }
        IQueryable<LearnerState> LearnerStates { get; }
        IQueryable<LearnerSettings> LearnerSettings { get; }
        IQueryable<ResearchPackage> ResearchPackages { get; }
        IQueryable<ChatSession> ChatSessions { get; }
        IQueryable<ChatMessage> ChatMessages { get; }
        IQueryable<Notification> Notifications { get; }
        IQueryable<Assessment> Assessments { get; }
        IQueryable<LearningActivity> Activities { get; }

        void Add<TEntity>(TEntity entity) where TEntity : class;
        void Remove<TEntity>(TEntity entity) where TEntity : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}