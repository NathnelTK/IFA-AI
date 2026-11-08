using System.ComponentModel.DataAnnotations;
using IFA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Controllers;

public partial class EducatorController
{
    public class ReorderModulesRequest
    {
        [Required, MaxLength(50)]
        public List<Guid> ModuleIds { get; set; } = new();
    }

    [HttpPut("courses/{courseId:guid}/modules/reorder")]
    public async Task<IActionResult> ReorderModules(Guid courseId, [FromBody] ReorderModulesRequest request, CancellationToken ct)
    {
        var guard = EducatorGuard();
        if (guard != null) return guard;
        var (course, error) = await LoadEditableCourseAsync(courseId, ct);
        if (error != null) return error;
        if (request.ModuleIds.Count != course!.Modules.Count || request.ModuleIds.Distinct().Count() != request.ModuleIds.Count ||
            request.ModuleIds.Any(id => !course.Modules.Any(m => m.Id == id)))
            return BadRequest(new { message = "Send every module ID exactly once, in the desired order." });

        var modules = request.ModuleIds.Select(id => course.Modules.First(m => m.Id == id)).ToList();
        await _context.ExecuteInTransactionAsync(async () =>
        {
            // Move out of the occupied range before assigning the final unique positions.
            for (var i = 0; i < modules.Count; i++) modules[i].ModuleNumber = -i - 1;
            await _context.SaveChangesAsync(ct);
            for (var i = 0; i < modules.Count; i++) modules[i].ModuleNumber = i + 1;
            await SyncProjectAsync(course, ct);
            await _context.SaveChangesAsync(ct);
        }, ct);
        return Ok(new { moduleIds = request.ModuleIds });
    }

    [HttpPost("courses/{courseId:guid}/modules/{moduleId:guid}/duplicate")]
    public async Task<IActionResult> DuplicateModule(Guid courseId, Guid moduleId, CancellationToken ct)
    {
        var guard = EducatorGuard();
        if (guard != null) return guard;
        var (course, error) = await LoadEditableCourseAsync(courseId, ct);
        if (error != null) return error;
        var original = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
        if (original == null) return NotFound(new { message = "Module not found." });
        if (course.Modules.Count >= 50) return BadRequest(new { message = "A course can contain at most 50 modules." });
        var copy = new Module
        {
            CourseId = courseId,
            ModuleNumber = course.Modules.Max(m => m.ModuleNumber) + 1,
            Title = CopyTitle(original.Title),
            Summary = original.Summary,
            EstimatedHours = original.EstimatedHours,
            GenerationStatus = original.GenerationStatus,
            GeneratedAt = original.GeneratedAt
        };
        foreach (var lesson in original.Lessons.OrderBy(l => l.LessonNumber))
            copy.Lessons.Add(CopyLesson(lesson, copy.Id, copy.Lessons.Count + 1, lesson.Title));
        foreach (var quiz in original.Quizzes.OrderBy(q => q.OrderIndex))
        {
            var quizCopy = new Quiz { ModuleId = copy.Id, Title = quiz.Title, PassingScorePercentage = quiz.PassingScorePercentage, Kind = quiz.Kind, OrderIndex = quiz.OrderIndex };
            foreach (var question in quiz.Questions)
                quizCopy.Questions.Add(new Question
                {
                    QuizId = quizCopy.Id, Prompt = question.Prompt, Options = question.Options.ToList(),
                    CorrectOptionIndex = question.CorrectOptionIndex, Explanation = question.Explanation,
                    TargetSkillName = question.TargetSkillName, BloomTaxonomyLevel = question.BloomTaxonomyLevel
                });
            copy.Quizzes.Add(quizCopy);
        }
        _context.Add(copy);
        await SyncProjectAsync(course, ct);
        await _context.SaveChangesAsync(ct);
        return Ok(new { moduleId = copy.Id, moduleNumber = copy.ModuleNumber, title = copy.Title });
    }

    [HttpPost("courses/{courseId:guid}/modules/{moduleId:guid}/lessons/{lessonId:guid}/duplicate")]
    public async Task<IActionResult> DuplicateLesson(Guid courseId, Guid moduleId, Guid lessonId, CancellationToken ct)
    {
        var guard = EducatorGuard();
        if (guard != null) return guard;
        var (course, error) = await LoadEditableCourseAsync(courseId, ct);
        if (error != null) return error;
        var module = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
        var original = module?.Lessons.FirstOrDefault(l => l.Id == lessonId);
        if (original == null) return NotFound(new { message = "Lesson not found in this module." });
        if (module!.Lessons.Count >= 100) return BadRequest(new { message = "A module can contain at most 100 lessons." });
        var copy = CopyLesson(original, moduleId, module.Lessons.Max(l => l.LessonNumber) + 1, CopyTitle(original.Title));
        _context.Add(copy);
        await SyncProjectAsync(course, ct);
        await _context.SaveChangesAsync(ct);
        return Ok(new { lessonId = copy.Id, lessonNumber = copy.LessonNumber, title = copy.Title });
    }

    private static string CopyTitle(string title) => $"{title[..Math.Min(title.Length, 193)]} (copy)";

    private static Lesson CopyLesson(Lesson original, Guid moduleId, int number, string title) => new()
    {
        ModuleId = moduleId, LessonNumber = number, Title = title, Summary = original.Summary,
        ContentMarkdown = original.ContentMarkdown, ReadingTimeMinutes = original.ReadingTimeMinutes,
        YouTubeVideoId = original.YouTubeVideoId, YouTubeVideoTitle = original.YouTubeVideoTitle,
        ScholarxivCitationDoi = original.ScholarxivCitationDoi, ScholarxivPaperTitle = original.ScholarxivPaperTitle
    };

    public class SaveQuizzesRequest
    {
        [Required, MaxLength(20)]
        public List<QuizEditDto> Quizzes { get; set; } = new();
    }

    public class QuizEditDto
    {
        public Guid? Id { get; set; }
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;
        [Range(1, 100)]
        public int? PassingScorePercentage { get; set; }
        public QuizKind Kind { get; set; } = QuizKind.Exam;
        [Required, MinLength(1), MaxLength(100)]
        public List<QuestionEditDto> Questions { get; set; } = new();
    }

    public class QuestionEditDto
    {
        public Guid? Id { get; set; }
        [Required, StringLength(2000)]
        public string Prompt { get; set; } = string.Empty;
        [Required, MinLength(2), MaxLength(8)]
        public List<string> Options { get; set; } = new();
        public int CorrectOptionIndex { get; set; }
        [StringLength(4000)]
        public string Explanation { get; set; } = string.Empty;
        [StringLength(160)]
        public string TargetSkillName { get; set; } = string.Empty;
    }

    [HttpPut("courses/{courseId:guid}/modules/{moduleId:guid}/quizzes")]
    public async Task<IActionResult> SaveQuizzes(Guid courseId, Guid moduleId, [FromBody] SaveQuizzesRequest request, CancellationToken ct)
    {
        var guard = EducatorGuard();
        if (guard != null) return guard;
        var (course, error) = await LoadEditableCourseAsync(courseId, ct);
        if (error != null) return error;
        var module = course!.Modules.FirstOrDefault(m => m.Id == moduleId);
        if (module == null) return NotFound(new { message = "Module not found." });
        if (request.Quizzes.Any(q => q == null || !Enum.IsDefined(q.Kind) || q.Questions == null ||
            q.Questions.Any(item => item == null || item.Options == null || item.Options.Any(o => string.IsNullOrWhiteSpace(o) || o.Length > 1000) ||
                item.CorrectOptionIndex < 0 || item.CorrectOptionIndex >= item.Options.Count)))
            return BadRequest(new { message = "Provide supported quizzes, nonempty answer choices, and a valid correct answer for every question." });

        var ids = request.Quizzes.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count || ids.Any(id => !module.Quizzes.Any(q => q.Id == id)))
            return BadRequest(new { message = "Quiz IDs must be unique and belong to this module." });
        foreach (var dto in request.Quizzes)
        {
            var questionIds = dto.Questions.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToList();
            var quiz = module.Quizzes.FirstOrDefault(q => q.Id == dto.Id);
            if (questionIds.Distinct().Count() != questionIds.Count || questionIds.Any(id => quiz == null || !quiz.Questions.Any(q => q.Id == id)))
                return BadRequest(new { message = "Question IDs must be unique and belong to their quiz." });
        }
        var existingIds = module.Quizzes.Select(q => q.Id).ToList();
        if (await _context.QuizAttempts.AnyAsync(a => existingIds.Contains(a.QuizId), ct) ||
            await _context.Assessments.AnyAsync(a => existingIds.Contains(a.QuizId), ct))
            return Conflict(new { message = "These assessments already have learner attempts. Duplicate the module to create a revised assessment without changing historical grades." });

        foreach (var removed in module.Quizzes.Where(q => !ids.Contains(q.Id)).ToList())
        {
            _context.Remove(removed);
            module.Quizzes.Remove(removed);
        }
        for (var i = 0; i < request.Quizzes.Count; i++)
        {
            var dto = request.Quizzes[i];
            var quiz = module.Quizzes.FirstOrDefault(q => q.Id == dto.Id);
            if (quiz == null)
            {
                quiz = new Quiz { ModuleId = moduleId };
                module.Quizzes.Add(quiz);
                _context.Add(quiz);
            }
            quiz.Title = dto.Title.Trim();
            quiz.Kind = dto.Kind;
            quiz.OrderIndex = i;
            quiz.PassingScorePercentage = dto.PassingScorePercentage ?? course.PassingScorePercentage;
            var questionIds = dto.Questions.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToHashSet();
            foreach (var removed in quiz.Questions.Where(q => !questionIds.Contains(q.Id)).ToList())
            {
                _context.Remove(removed);
                quiz.Questions.Remove(removed);
            }
            foreach (var item in dto.Questions)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == item.Id);
                if (question == null)
                {
                    question = new Question { QuizId = quiz.Id };
                    quiz.Questions.Add(question);
                    _context.Add(question);
                }
                question.Prompt = item.Prompt.Trim();
                question.Options = item.Options.Select(o => o.Trim()).ToList();
                question.CorrectOptionIndex = item.CorrectOptionIndex;
                question.Explanation = item.Explanation?.Trim() ?? string.Empty;
                question.TargetSkillName = item.TargetSkillName?.Trim() ?? string.Empty;
            }
        }
        await SyncProjectAsync(course, ct);
        await _context.SaveChangesAsync(ct);
        return Ok(new { quizzes = module.Quizzes.OrderBy(q => q.OrderIndex).Select(q => new
        {
            q.Id, q.Title, q.Kind, q.PassingScorePercentage, q.OrderIndex,
            questions = q.Questions.Select(item => new { item.Id, item.Prompt, item.Options, item.CorrectOptionIndex, item.Explanation, item.TargetSkillName })
        }) });
    }
}
