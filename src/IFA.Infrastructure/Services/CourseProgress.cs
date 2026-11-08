using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Services;

public sealed class CourseProgressSnapshot
{
    internal HashSet<Guid> CompletedLessonIds { get; init; } = [];
    internal List<Assessment> Assessments { get; init; } = [];
    internal HashSet<Guid> CompletedModuleIds { get; init; } = [];
    public int CompletedLessons => CompletedLessonIds.Count;
    public int TotalLessons { get; init; }
    public int PassedRequiredQuizzes { get; init; }
    public int TotalRequiredQuizzes { get; init; }
    public int CompletedUnits => CompletedLessons + PassedRequiredQuizzes;
    public int TotalUnits => TotalLessons + TotalRequiredQuizzes;
    public bool IsCompleted => TotalUnits > 0 && CompletedUnits == TotalUnits;
    public int Percentage => IsCompleted ? 100 : TotalUnits == 0 ? 0 : Math.Min(99, (int)Math.Round(100.0 * CompletedUnits / TotalUnits));
    public int CompletedModules => CompletedModuleIds.Count;
    public int TotalModules { get; init; }
    public string Denominator => "All lessons plus all quizzes when requirePassingQuizzes is true; assignments are excluded.";
    public bool IsLessonCompleted(Guid id) => CompletedLessonIds.Contains(id);
    public bool IsModuleCompleted(Guid id) => CompletedModuleIds.Contains(id);
    public List<Assessment> AttemptsFor(Guid quizId) => Assessments.Where(a => a.QuizId == quizId)
        .OrderByDescending(a => a.AttemptNumber).ThenByDescending(a => a.SubmittedAt).ToList();
}

public static class CourseProgress
{
    public static async Task<CourseProgressSnapshot> GetAsync(
        IApplicationDbContext context, Course course, Guid? learnerId, CancellationToken ct = default)
    {
        var modules = await context.Modules.Where(m => m.CourseId == course.Id).Select(m => m.Id).ToListAsync(ct);
        var lessons = await context.Lessons.Where(l => l.Module!.CourseId == course.Id)
            .Select(l => new { l.Id, l.ModuleId }).ToListAsync(ct);
        var quizzes = await context.Quizzes.Where(q => q.Module!.CourseId == course.Id)
            .Select(q => new { q.Id, q.ModuleId }).ToListAsync(ct);
        var completed = learnerId.HasValue
            ? (await context.LessonProgress.Where(p => p.LearnerId == learnerId && p.IsCompleted && p.Lesson!.Module!.CourseId == course.Id)
                .Select(p => p.LessonId).ToListAsync(ct)).ToHashSet()
            : [];
        var assessments = learnerId.HasValue
            ? await context.Assessments.AsNoTracking().Where(a => a.LearnerId == learnerId && a.Quiz!.Module!.CourseId == course.Id).ToListAsync(ct)
            : [];
        var passedQuizIds = assessments.Where(a => a.IsPassed).Select(a => a.QuizId).ToHashSet();
        return new CourseProgressSnapshot
        {
            CompletedLessonIds = completed,
            Assessments = assessments,
            TotalLessons = lessons.Count,
            PassedRequiredQuizzes = course.RequirePassingQuizzes ? passedQuizIds.Count : 0,
            TotalRequiredQuizzes = course.RequirePassingQuizzes ? quizzes.Count : 0,
            TotalModules = modules.Count,
            CompletedModuleIds = modules.Where(id => lessons.Any(l => l.ModuleId == id) &&
                lessons.Where(l => l.ModuleId == id).All(l => completed.Contains(l.Id)) &&
                (!course.RequirePassingQuizzes || quizzes.Where(q => q.ModuleId == id).All(q => passedQuizIds.Contains(q.Id)))).ToHashSet()
        };
    }

    // Call after persisting the lesson/assessment, inside the same mutation transaction.
    public static async Task<CourseProgressSnapshot> UpdateAsync(
        IApplicationDbContext context, Course course, CourseEnrollment enrollment, CancellationToken ct = default, bool updateLastAccessed = true)
    {
        var progress = await GetAsync(context, course, enrollment.LearnerId, ct);
        enrollment.ProgressPercentage = progress.Percentage;
        enrollment.CompletedModulesCount = progress.CompletedModules;
        if (updateLastAccessed) enrollment.LastAccessedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return progress;
    }

    public static object LessonDto(Lesson lesson, bool hasContentAccess, CourseProgressSnapshot progress) => new
    {
        lesson.Id,
        lesson.ModuleId,
        lesson.LessonNumber,
        lesson.Title,
        Summary = hasContentAccess ? lesson.Summary : string.Empty,
        ContentMarkdown = hasContentAccess ? lesson.ContentMarkdown : string.Empty,
        lesson.ReadingTimeMinutes,
        YouTubeVideoId = hasContentAccess ? lesson.YouTubeVideoId : null,
        YouTubeVideoTitle = hasContentAccess ? lesson.YouTubeVideoTitle : null,
        ScholarxivCitationDoi = hasContentAccess ? lesson.ScholarxivCitationDoi : null,
        ScholarxivPaperTitle = hasContentAccess ? lesson.ScholarxivPaperTitle : null,
        lesson.CreatedAt,
        IsCompleted = progress.IsLessonCompleted(lesson.Id)
    };

    public static object QuizDto(Quiz quiz, Course course, bool isOwner, CourseProgressSnapshot progress)
    {
        var attempts = progress.AttemptsFor(quiz.Id);
        var latest = attempts.FirstOrDefault();
        return new
        {
            quiz.Id,
            quiz.ModuleId,
            quiz.Title,
            quiz.Kind,
            quiz.OrderIndex,
            quiz.PassingScorePercentage,
            MaxAttempts = course.MaxQuizAttempts,
            AttemptCount = attempts.Count,
            LastScorePercentage = latest?.ScorePercentage,
            BestScorePercentage = attempts.Count > 0 ? (int?)attempts.Max(a => a.ScorePercentage) : null,
            IsPassed = attempts.Any(a => a.IsPassed),
            LatestAttempt = latest == null ? null : new
            {
                AssessmentId = latest.Id,
                latest.ScorePercentage,
                latest.IsPassed,
                latest.AttemptNumber,
                latest.SubmittedAt
            },
            Questions = quiz.Questions.OrderBy(q => q.Id).Select(q => isOwner
                ? (object)new { q.Id, q.QuizId, q.Prompt, q.Options, q.CorrectOptionIndex, q.Explanation, q.TargetSkillName, q.BloomTaxonomyLevel }
                : new { q.Id, q.QuizId, q.Prompt, q.Options, q.TargetSkillName, q.BloomTaxonomyLevel })
        };
    }
}
