using IFA.Domain.Entities;

namespace IFA.Application.Common.Services;

public static class CoursePublication
{
    public static List<string> Validate(Course course)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(course.Title)) errors.Add("Enter a course title.");
        if (string.IsNullOrWhiteSpace(course.Description)) errors.Add("Add a course description.");
        if (string.IsNullOrWhiteSpace(course.Category)) errors.Add("Choose a category.");
        if (string.IsNullOrWhiteSpace(course.Language) || string.IsNullOrWhiteSpace(course.Level))
            errors.Add("Set the language and level.");
        if (!course.LearningObjectives.Any(o => !string.IsNullOrWhiteSpace(o)))
            errors.Add("Add at least one learning objective.");
        if (course.Modules.Count == 0) errors.Add("Add at least one module.");
        foreach (var module in course.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Title) || module.GenerationStatus != ModuleGenerationStatus.Ready)
                errors.Add($"Finish module '{module.Title}' before publishing.");
            if (module.Lessons.Count == 0 || module.Lessons.Any(l => string.IsNullOrWhiteSpace(l.Title) || string.IsNullOrWhiteSpace(l.ContentMarkdown)))
                errors.Add($"Module '{module.Title}' needs titled lessons with saved content.");
            foreach (var quiz in module.Quizzes)
            {
                if (string.IsNullOrWhiteSpace(quiz.Title) || quiz.Questions.Count == 0 || quiz.PassingScorePercentage is < 1 or > 100 ||
                    quiz.Questions.Any(q => string.IsNullOrWhiteSpace(q.Prompt) || q.Options.Count is < 2 or > 8 ||
                        q.Options.Any(string.IsNullOrWhiteSpace) || q.CorrectOptionIndex < 0 || q.CorrectOptionIndex >= q.Options.Count))
                    errors.Add($"Review the title, questions and answer key for '{quiz.Title}'.");
            }
        }
        foreach (var assignment in course.Assignments)
        {
            if (string.IsNullOrWhiteSpace(assignment.Title) || string.IsNullOrWhiteSpace(assignment.InstructionsMarkdown) ||
                assignment.DueDaysAfterEnrollment < 0 || assignment.MaxScore <= 0)
                errors.Add($"Review the title, instructions, deadline and score for assignment '{assignment.Title}'.");
        }
        if (course.PassingScorePercentage is < 1 or > 100 || course.MaxQuizAttempts is < 1 or > 100)
            errors.Add("Use a passing score of 1-100 and an attempt limit of 1-100.");
        if (course.EnrollmentLimit is <= 0 || (course.EnrollmentOpensAt.HasValue && course.EnrollmentClosesAt <= course.EnrollmentOpensAt))
            errors.Add("Check the enrollment limit and enrollment dates.");
        return errors;
    }
}
