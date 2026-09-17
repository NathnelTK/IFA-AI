using System.Threading;
using System.Threading.Tasks;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        System.Collections.Generic.IEnumerable<Learner> Learners { get; }
        System.Collections.Generic.IEnumerable<Course> Courses { get; }
        System.Collections.Generic.IEnumerable<Module> Modules { get; }
        System.Collections.Generic.IEnumerable<Lesson> Lessons { get; }
        System.Collections.Generic.IEnumerable<Quiz> Quizzes { get; }
        System.Collections.Generic.IEnumerable<Question> Questions { get; }
        System.Collections.Generic.IEnumerable<SkillMetric> SkillMetrics { get; }
        System.Collections.Generic.IEnumerable<CourseEnrollment> CourseEnrollments { get; }
        System.Collections.Generic.IEnumerable<CourseShareInvite> CourseShareInvites { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
