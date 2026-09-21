// IFA.Infrastructure/Data/DevSeeder.cs
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Data
{
    /// <summary>
    /// Local-dev-only seed data with FIXED guids, so every teammate's
    /// Swagger session and every demo script reference the same rows.
    /// NOT the same thing as PR 8.5's production demo seed - that one
    /// needs a fully generated course + assessment history for the demo
    /// narrative. This is just "give me a Learner to point Swagger at."
    /// </summary>
    public static class DevSeeder
    {
        public static readonly Guid DemoLearnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
        {
            if (await db.Learners.AnyAsync(l => l.Id == DemoLearnerId))
            {
                logger.LogInformation("Dev seed learner already present, skipping.");
                return;
            }

            db.Learners.Add(new Learner
            {
                Id = DemoLearnerId,
                Name = "Nathnel Demo",
                Email = "demo@ifa.local",
                OverallProgress = 0
            });

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded dev learner {LearnerId}", DemoLearnerId);
        }
    }
}