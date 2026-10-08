using IFA.Application.Skills.Queries;
using IFA.Application.Common.Interfaces;

namespace IFA.Application.Skills.Services
{
    public class SkillProfileService
    {
        private static readonly string[] Palette =
        {
            "#2A9D68", "#E07A5F", "#7C5CFC", "#E11D48", "#EF4444", "#F59E0B", "#3B82F6", "#10B981"
        };

        private readonly IApplicationDbContext _context;

        public SkillProfileService(IApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Builds a learner's skill profile from their OWN data:
        /// seeded/assessed <see cref="IFA.Domain.Entities.SkillMetric"/> rows plus
        /// real quiz performance (QuizAnswer -> Question.TargetSkillName). No two
        /// learners share a profile unless their activity is actually identical.
        /// </summary>
        public Task<GetLearnerSkillsQueryResult> GetLearnerSkillsAsync(Guid learnerId)
        {
            var metrics = _context.SkillMetrics
                .Where(m => m.LearnerId == learnerId)
                .ToList();

            // Real performance signal: every answered question, its skill, and
            // when the attempt happened.
            var answers = _context.QuizAnswers
                .Where(a => a.QuizAttempt!.LearnerId == learnerId)
                .Select(a => new
                {
                    Skill = a.Question!.TargetSkillName,
                    a.IsCorrect,
                    a.QuizAttemptId,
                    Date = a.QuizAttempt!.CompletedAt ?? a.QuizAttempt.StartedAt
                })
                .ToList();

            var answerGroups = answers
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Skill) ? "General" : a.Skill)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Union of skills the learner has been assessed on or has quiz data for.
            var skillNames = metrics.Select(m => m.SkillName)
                .Concat(answerGroups.Keys)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var skills = new List<LearnerSkillDto>();
            for (var i = 0; i < skillNames.Count; i++)
            {
                var name = skillNames[i];
                var metric = metrics.FirstOrDefault(m => string.Equals(m.SkillName, name, StringComparison.OrdinalIgnoreCase));
                answerGroups.TryGetValue(name, out var group);

                var percentage = metric?.CompetencyPercentage ?? 0;
                var improvement = 0;

                if (group is { Count: > 0 })
                {
                    // Actual accuracy on questions tagged with this skill.
                    percentage = (int)Math.Round(group.Count(a => a.IsCorrect) * 100.0 / group.Count);

                    // Trend across attempts (latest vs earliest) when we have both.
                    var perAttempt = group
                        .GroupBy(a => a.QuizAttemptId)
                        .Select(g => new
                        {
                            Date = g.Max(x => x.Date),
                            Accuracy = g.Count(x => x.IsCorrect) * 100.0 / g.Count()
                        })
                        .OrderBy(x => x.Date)
                        .ToList();

                    if (perAttempt.Count >= 2)
                    {
                        improvement = (int)Math.Round(perAttempt[^1].Accuracy - perAttempt[0].Accuracy);
                    }
                }

                var color = metric?.HexColor;
                if (string.IsNullOrWhiteSpace(color))
                {
                    color = Palette[i % Palette.Length];
                }

                skills.Add(new LearnerSkillDto
                {
                    Name = name,
                    Percentage = Math.Clamp(percentage, 0, 100),
                    Color = color!,
                    Improvement = improvement >= 0 ? $"+{improvement}%" : $"{improvement}%"
                });
            }

            // Split into two meaningful, non-fabricated categories.
            var strengths = skills.Where(s => s.Percentage >= 50).OrderByDescending(s => s.Percentage).ToList();
            var focusAreas = skills.Where(s => s.Percentage < 50).OrderBy(s => s.Percentage).ToList();

            var categories = new List<SkillCategoryDto>();
            if (strengths.Count > 0)
            {
                categories.Add(new SkillCategoryDto { Name = "Strengths", Skills = strengths });
            }
            if (focusAreas.Count > 0)
            {
                categories.Add(new SkillCategoryDto { Name = "Focus Areas", Skills = focusAreas });
            }

            var overallImprovement = skills.Count == 0
                ? 0
                : (int)Math.Round(skills
                    .Select(s => int.TryParse(s.Improvement.TrimEnd('%', '+'), out var v) ? v : 0)
                    .DefaultIfEmpty(0)
                    .Average());

            return Task.FromResult(new GetLearnerSkillsQueryResult
            {
                Categories = categories,
                OverallImprovement = overallImprovement
            });
        }

        public async Task<List<LearnerSkillDto>> GetWeakAreasAsync(Guid learnerId)
        {
            var allSkills = await GetLearnerSkillsAsync(learnerId);
            return allSkills.Categories
                .SelectMany(c => c.Skills)
                .Where(s => s.Percentage < 50)
                .OrderBy(s => s.Percentage)
                .ToList();
        }
    }
}
