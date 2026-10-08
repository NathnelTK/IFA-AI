using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Data
{
    public static class EntranceExamCourseSeeder
    {
        public static readonly Guid CourseId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private static readonly (string Title, string Summary)[] WeeklyModules =
        {
            ("Week 1: Diagnostic and core mathematics", "Take a timed baseline across mathematics, physics, chemistry, biology, and English. Review algebra, ratios, units, and scientific notation."),
            ("Week 2: Functions, equations, and graphs", "Practice functions, equations, inequalities, sequences, graph interpretation, and translating word problems into mathematical models."),
            ("Week 3: Trigonometry, vectors, and geometry", "Review trigonometric ratios and identities, coordinate geometry, vector components, and multi-step geometry problems."),
            ("Week 4: Mechanics and motion", "Study kinematics, Newton's laws, momentum, work, energy, and power. Draw diagrams and check units before calculating."),
            ("Week 5: Waves, electricity, and magnetism", "Practice wave relationships, sound and light, circuits, Ohm's law, electrical power, and basic magnetic effects."),
            ("Week 6: Chemistry foundations and quantitative problems", "Review atomic structure, periodic trends, bonding, formulae, balancing equations, mole calculations, and concentration."),
            ("Week 7: Reactions, equilibrium, acids, and organic chemistry", "Practice reaction types, rates, equilibrium, acids and bases, oxidation-reduction, and introductory organic functional groups."),
            ("Week 8: Cells, genetics, and human biology", "Review cell structure, transport, enzymes, mitosis and meiosis, inheritance, DNA, and major human body systems."),
            ("Week 9: Ecology, evolution, and plant biology", "Practice ecosystems, energy flow, population change, natural selection, classification, plant structure, and photosynthesis."),
            ("Week 10: English comprehension and language skills", "Build reading accuracy, vocabulary from context, grammar, sentence correction, and concise written responses under time limits."),
            ("Week 11: Mixed subject practice and error repair", "Complete timed mixed sets. Categorize every missed question by concept, calculation, reading, or time-management error, then retry it."),
            ("Week 12: Full mock exams and final review", "Simulate full exam conditions, review the error log, revisit high-impact weak areas, and use a calm exam-day pacing plan.")
        };

        public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, bool enrollDemoLearner = false)
        {
            var course = await db.Courses
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(c => c.Id == CourseId);

            if (course is null)
            {
                course = new Course
                {
                    Id = CourseId,
                    Title = "Ethiopian Grade 12 Natural Science Entrance Exam — 12-Week Plan",
                    Description = "A 12-week, five-subject preparation plan for Mathematics, Physics, Chemistry, Biology, and English. Each weekly module is expanded into a lesson and practice quiz using Groq, grounded in current ScholarXiv search results. This independent study plan is not an official Ministry of Education syllabus or exam paper.",
                    Category = "Entrance Exam",
                    TargetAudience = "Ethiopian Grade 12 / Natural Science",
                    ThumbnailUrl = "/ifa.png",
                    ProviderName = "IFA AI",
                    Badge = "12-week plan",
                    Rating = 0,
                    ReviewCount = "0",
                    EstimatedDuration = "12 weeks",
                    IsPublic = true,
                    ShareCode = "ETH12STM",
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var (title, summary) in WeeklyModules)
                {
                    course.Modules.Add(new Module
                    {
                        CourseId = course.Id,
                        ModuleNumber = course.Modules.Count + 1,
                        Title = title,
                        Summary = summary,
                        EstimatedHours = 20,
                        GenerationStatus = ModuleGenerationStatus.Blueprint
                    });
                }

                db.Courses.Add(course);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded public Ethiopian Grade 12 entrance-exam course {CourseId}", course.Id);
            }

            if (enrollDemoLearner && await db.Learners.AnyAsync(l => l.Id == DevSeeder.DemoLearnerId))
            {
                var hasEnrollment = await db.CourseEnrollments
                    .AnyAsync(e => e.CourseId == course.Id && e.LearnerId == DevSeeder.DemoLearnerId);

                if (!hasEnrollment)
                {
                    db.CourseEnrollments.Add(new CourseEnrollment
                    {
                        CourseId = course.Id,
                        LearnerId = DevSeeder.DemoLearnerId
                    });
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
