using IFA.Application.Skills.Queries;
using IFA.Application.Common.Interfaces;

namespace IFA.Application.Skills.Services
{
    public class SkillProfileService
    {
        private readonly IApplicationDbContext _context;

        public SkillProfileService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetLearnerSkillsQueryResult> GetLearnerSkillsAsync(Guid learnerId)
        {
            // In a real implementation, this would query the database for actual skill data
            // For now, returning mock data matching the frontend implementation
            var result = new GetLearnerSkillsQueryResult
            {
                Categories = new List<SkillCategoryDto>
                {
                    new SkillCategoryDto
                    {
                        Name = "Backend Development",
                        Skills = new List<LearnerSkillDto>
                        {
                            new LearnerSkillDto { Name = "C#", Percentage = 84, Color = "#2A9D68", Improvement = "+12%" },
                            new LearnerSkillDto { Name = "Databases", Percentage = 61, Color = "#E07A5F", Improvement = "+8%" },
                            new LearnerSkillDto { Name = "APIs", Percentage = 55, Color = "#7C5CFC", Improvement = "+5%" },
                            new LearnerSkillDto { Name = "Authentication", Percentage = 45, Color = "#E11D48", Improvement = "+3%" },
                            new LearnerSkillDto { Name = "Testing", Percentage = 32, Color = "#EF4444", Improvement = "+15%" }
                        }
                    },
                    new SkillCategoryDto
                    {
                        Name = "Frontend Development",
                        Skills = new List<LearnerSkillDto>
                        {
                            new LearnerSkillDto { Name = "JavaScript", Percentage = 72, Color = "#F59E0B", Improvement = "+10%" },
                            new LearnerSkillDto { Name = "React", Percentage = 68, Color = "#3B82F6", Improvement = "+7%" },
                            new LearnerSkillDto { Name = "CSS/Tailwind", Percentage = 75, Color = "#10B981", Improvement = "+9%" }
                        }
                    }
                },
                OverallImprovement = 8
            };

            return await Task.FromResult(result);
        }

        public async Task<List<LearnerSkillDto>> GetWeakAreasAsync(Guid learnerId)
        {
            // Return skills below 50% as weak areas
            var allSkills = await GetLearnerSkillsAsync(learnerId);
            var weakSkills = allSkills.Categories
                .SelectMany(c => c.Skills)
                .Where(s => s.Percentage < 50)
                .ToList();

            return await Task.FromResult(weakSkills);
        }
    }
}