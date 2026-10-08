using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using IFA.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;

namespace IFA.Infrastructure.Services
{
    public class AiTutorService : IAiTutorService
    {
        private readonly ILlmGateway _llmGateway;
        private readonly IApplicationDbContext _context;

        public AiTutorService(ILlmGateway llmGateway, IApplicationDbContext context)
        {
            _llmGateway = llmGateway;
            _context = context;
        }

        public async Task<TutorResponse> ChatAsync(Guid learnerId, TutorMessageRequest request, CancellationToken ct = default)
        {
            string courseContext = "";
            string lessonContext = "";
            string skillsContext = "";

            if (request.LessonId.HasValue && request.LessonId != Guid.Empty)
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Module)
                    .ThenInclude(m => m!.Course)
                    .FirstOrDefaultAsync(l => l.Id == request.LessonId.Value, ct);

                if (lesson != null)
                {
                    courseContext = $"Course: {lesson.Module?.Course?.Title}\nModule: {lesson.Module?.Title}\n";
                    lessonContext = $"Current Lesson: {lesson.Title}\nLesson Summary: {lesson.Summary}\nLesson Content Excerpt:\n{lesson.ContentMarkdown?.Take(1000).ToArray()}";
                }
            }
            else if (request.CourseId.HasValue && request.CourseId != Guid.Empty)
            {
                // No specific lesson open — scope the tutor to the chosen course
                // so its subject matches what the learner is studying.
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == request.CourseId.Value, ct);

                if (course != null)
                {
                    courseContext = $"Course: {course.Title}\nCategory: {course.Category}\nDescription: {course.Description}\n";
                }
            }

            var weakSkills = await _context.SkillMetrics
                .Where(s => s.LearnerId == learnerId && s.IsWeakArea)
                .Select(s => s.SkillName)
                .ToListAsync(ct);

            if (weakSkills.Any())
            {
                skillsContext = $"Learner Weak Skills to watch for: {string.Join(", ", weakSkills)}";
            }

            var historyText = string.Join("\n", (request.History ?? new List<IntakeMessageDto>())
                .Select(h => $"{h.Role.ToUpper()}: {h.Content}"));

            var userPrompt = $"CONTEXT:\n{courseContext}\n{lessonContext}\n{skillsContext}\n\nCONVERSATION HISTORY:\n{historyText}\n\nLEARNER QUESTION:\n{request.Message}";

            // When the learner has picked a specific course, tutor strictly within
            // that course's subject instead of the fixed exam-prep persona.
            var systemPrompt = string.IsNullOrWhiteSpace(courseContext)
                ? PromptRegistry.TutorSystemPrompt
                : "You are IFA's AI tutor for a specific course the learner is enrolled in. " +
                  "Teach according to the COURSE CONTEXT below — match that course's subject, level and terminology. " +
                  "Be concise, well-structured, and give worked examples where useful.";

            var reply = await _llmGateway.CompleteAsync(systemPrompt, userPrompt, LlmRole.Tutor, ct);

            // Extract code snippet if present
            string? codeSnippet = null;
            string? language = null;
            var codeMatch = Regex.Match(reply, @"```([a-zA-Z0-9#+]*)\s*([\s\S]*?)```");
            if (codeMatch.Success)
            {
                language = codeMatch.Groups[1].Value.Trim();
                codeSnippet = codeMatch.Groups[2].Value.Trim();
            }

            return new TutorResponse
            {
                ReplyMarkdown = reply,
                CodeSnippet = codeSnippet,
                Language = string.IsNullOrWhiteSpace(language) ? "csharp" : language,
                SuggestedFollowUps = new List<string>
                {
                    "Can you give me a practical exercise for this?",
                    "How does this relate to the upcoming quiz?",
                    "Why is this preferred over alternatives?"
                }
            };
        }
    }
}
