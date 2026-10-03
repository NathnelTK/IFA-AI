// IFA.Application/Common/Helpers/CourseGenerationHelpers.cs
// Pure Domain/Application logic - no EF Core, no HTTP. Safe to call from
// both IFA.API (manual testing endpoints) and IFA.Infrastructure (the
// orchestrator), without either layer depending on the other.
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;

namespace IFA.Application.Common.Helpers
{
    public static class CourseGenerationHelpers
    {
        public static Course BuildCourseFromProposal(
            CoursePipelineProposal proposal, LearnerProfile profile,
            Guid? persistedResearchId, Guid learnerId)
        {
            var hoursPerWeek = Math.Max(1, profile.AvailableStudyHoursPerWeek ?? 5);
            var weeks = Math.Max(1, (int)Math.Ceiling(proposal.TotalEstimatedHours / (double)hoursPerWeek));

            var course = new Course
            {
                Title = proposal.CourseTitle,
                Description = proposal.Description,
                Category = profile.SubjectTopic ?? "General",
                TargetAudience = profile.CurrentLevel ?? "General",
                EstimatedDuration = $"{weeks} week{(weeks == 1 ? "" : "s")}",
                CreatorLearnerId = learnerId,
                SourceLearnerProfileId = profile.Id,
                SourceResearchPackageId = persistedResearchId,
                Rating = 0,
                ReviewCount = "0",
                ShareCode = Guid.NewGuid().ToString("N")[..8]
            };

            foreach (var m in proposal.Modules)
            {
                course.Modules.Add(new Module
                {
                    ModuleNumber = m.ModuleNumber,
                    Title = m.Title,
                    Summary = m.Summary,
                    EstimatedHours = m.EstimatedHours,
                    KeyTopics = m.KeyTopics,
                    GenerationStatus = ModuleGenerationStatus.Blueprint
                });
            }

            return course;
        }

        public static void ApplyGeneratedContent(Module module, GeneratedModuleContent content)
        {
            for (var i = 0; i < content.Lessons.Count; i++)
            {
                var l = content.Lessons[i];
                module.Lessons.Add(new Lesson
                {
                    ModuleId = module.Id,
                    LessonNumber = i + 1,
                    Title = l.Title,
                    Summary = l.Summary,
                    ContentMarkdown = l.ContentMarkdown,
                    ReadingTimeMinutes = l.ReadingTimeMinutes
                });
            }

            var quiz = new Quiz { ModuleId = module.Id, Title = content.QuizTitle };
            foreach (var q in content.Questions)
            {
                quiz.Questions.Add(new Question
                {
                    Prompt = q.Prompt,
                    Options = q.Options,
                    CorrectOptionIndex = q.CorrectOptionIndex,
                    Explanation = q.Explanation,
                    TargetSkillName = q.TargetSkillName,
                    BloomTaxonomyLevel = q.BloomTaxonomyLevel
                });
            }
            module.ModuleQuiz = quiz;
        }
    }
}