namespace IFA.Application.Skills.Queries
{
    public class GetLearnerSkillsQuery
    {
        public Guid LearnerId { get; set; }
    }

    public class LearnerSkillDto
    {
        public required string Name { get; set; }
        public int Percentage { get; set; }
        public required string Color { get; set; }
        public required string Improvement { get; set; }
    }

    public class SkillCategoryDto
    {
        public required string Name { get; set; }
        public required List<LearnerSkillDto> Skills { get; set; }
    }

    public class GetLearnerSkillsQueryResult
    {
        public required List<SkillCategoryDto> Categories { get; set; }
        public int OverallImprovement { get; set; }
    }
}