namespace IFA.Infrastructure.Data
{
    /// <summary>
    /// Canonical demo accounts. Defined in code (not a new DB column) so the
    /// seeder and the public sign-in screen always agree on credentials without
    /// requiring a schema migration on an already-created database.
    /// </summary>
    public static class DemoAccounts
    {
        public sealed record DemoAccount(
            string Name,
            string Email,
            string Password,
            string Role,
            string AvatarUrl,
            string Goal,
            string Subject,
            string CurrentLevel,
            string TargetOutcome,
            int WeeklyStudyHours,
            string LearningStyle,
            string[] Strengths,
            string[] Weaknesses);

        public const string DefaultPassword = "ifa12345";

        public static readonly DemoAccount[] All =
        {
            new(
                Name: "Nathnel Teklemariam",
                Email: "nathnel@ifa.local",
                Password: DefaultPassword,
                Role: "Learner",
                AvatarUrl: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=100&h=100&fit=crop&crop=faces",
                Goal: "Master C# backend development and pass the exit exam",
                Subject: "Software Engineering",
                CurrentLevel: "Intermediate",
                TargetOutcome: "Ship production REST APIs and pass the exit exam",
                WeeklyStudyHours: 8,
                LearningStyle: "Hands-on",
                Strengths: new[] { "C# syntax", "Control flow" },
                Weaknesses: new[] { "Authentication", "Testing" }),

            new(
                Name: "Ermiyas Eshetu",
                Email: "ermiyas@ifa.local",
                Password: DefaultPassword,
                Role: "Learner",
                AvatarUrl: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=100&h=100&fit=crop&crop=faces",
                Goal: "Become confident in databases and data modeling",
                Subject: "Databases",
                CurrentLevel: "Intermediate",
                TargetOutcome: "Design normalized schemas and write efficient SQL",
                WeeklyStudyHours: 6,
                LearningStyle: "Visual",
                Strengths: new[] { "SQL basics", "Relational thinking" },
                Weaknesses: new[] { "Query optimization", "Indexing" }),

            new(
                Name: "Negede Tekleyes",
                Email: "negede@ifa.local",
                Password: DefaultPassword,
                Role: "Learner",
                AvatarUrl: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=100&h=100&fit=crop&crop=faces",
                Goal: "Learn Python and build a first data project",
                Subject: "Python & Data",
                CurrentLevel: "Beginner",
                TargetOutcome: "Build and deploy a small data-analysis project",
                WeeklyStudyHours: 5,
                LearningStyle: "Hands-on",
                Strengths: new[] { "Logical reasoning" },
                Weaknesses: new[] { "Python syntax", "Pandas" }),

            new(
                Name: "Hanna Girma",
                Email: "hanna@ifa.local",
                Password: DefaultPassword,
                Role: "Learner",
                AvatarUrl: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=100&h=100&fit=crop&crop=faces",
                Goal: "Prepare for the Grade 12 natural science entrance exam",
                Subject: "Natural Science",
                CurrentLevel: "Intermediate",
                TargetOutcome: "Score competitively across maths, physics, chemistry and biology",
                WeeklyStudyHours: 12,
                LearningStyle: "Reading",
                Strengths: new[] { "Biology", "Chemistry basics" },
                Weaknesses: new[] { "Physics mechanics", "Calculus" })
        };
    }
}
