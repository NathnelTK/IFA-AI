using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IFA.Domain.Entities;
using IFA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFA.Infrastructure.Data
{
    /// <summary>
    /// Seeds realistic demo accounts, profiles, skill metrics and a public
    /// course catalog so every screen has meaningful data on first run.
    /// Idempotent: existing learners/courses are left untouched.
    /// </summary>
    public static class DemoDataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
        {
            await RecoverStaleGenerationAsync(db, logger);
            await SeedLearnersAsync(db, logger);
            await SeedCoursesAsync(db, logger);
            await SeedEnrollmentsAsync(db, logger);
            await SeedSocialAsync(db, logger);
        }

        /// <summary>
        /// A generation that crashed mid-flight leaves a module stuck in the
        /// Generating state, which would block every future attempt. Any such
        /// module with no generated lessons is safe to reset to Blueprint.
        /// </summary>
        private static async Task RecoverStaleGenerationAsync(ApplicationDbContext db, ILogger logger)
        {
            var stuck = await db.Modules
                .Where(m => m.GenerationStatus == ModuleGenerationStatus.Generating && !m.Lessons.Any())
                .ToListAsync();

            if (stuck.Count == 0) return;

            foreach (var module in stuck)
            {
                module.GenerationStatus = ModuleGenerationStatus.Blueprint;
            }

            await db.SaveChangesAsync();
            logger.LogWarning("Reset {Count} stale generating module(s) back to Blueprint.", stuck.Count);
        }

        // -------------------------------------------------------------------
        // Learners: credentials, profile, state, settings, skills
        // -------------------------------------------------------------------
        private static async Task SeedLearnersAsync(ApplicationDbContext db, ILogger logger)
        {
            foreach (var account in DemoAccounts.All)
            {
                var email = account.Email.ToLowerInvariant();
                var learner = await db.Learners.FirstOrDefaultAsync(l => l.Email == email);
                if (learner is null)
                {
                    learner = new Learner
                    {
                        Id = Guid.NewGuid(),
                        Name = account.Name,
                        Email = email,
                        PasswordHash = PasswordHasher.Hash(account.Password),
                        Role = account.Role,
                        AvatarUrl = account.AvatarUrl,
                        OverallProgress = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Learners.Add(learner);
                }
                else if (string.IsNullOrWhiteSpace(learner.PasswordHash))
                {
                    learner.PasswordHash = PasswordHasher.Hash(account.Password);
                }

                var existingProfile = await db.LearnerProfiles.FirstOrDefaultAsync(p => p.LearnerId == learner.Id);
                if (existingProfile is null)
                {
                    db.LearnerProfiles.Add(new LearnerProfile
                    {
                        Id = Guid.NewGuid(),
                        LearnerId = learner.Id,
                        LearningGoal = account.Goal,
                        Subject = account.Subject,
                        CurrentLevel = account.CurrentLevel,
                        TargetOutcome = account.TargetOutcome,
                        WeeklyStudyHours = account.WeeklyStudyHours,
                        PreferredLanguage = "en",
                        LearningStyle = account.LearningStyle,
                        Constraints = string.Empty,
                        PreferredYouTubeChannelsJson = JsonSerializer.Serialize(new[] { "freeCodeCamp", "Traversy Media" }),
                        KnownStrengthsJson = JsonSerializer.Serialize(account.Strengths),
                        KnownWeaknessesJson = JsonSerializer.Serialize(account.Weaknesses),
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    // Keep demo profiles canonical even if a previous run's
                    // fallback AI wrote placeholder values into them.
                    existingProfile.LearningGoal = account.Goal;
                    existingProfile.Subject = account.Subject;
                    existingProfile.CurrentLevel = account.CurrentLevel;
                    existingProfile.TargetOutcome = account.TargetOutcome;
                    existingProfile.WeeklyStudyHours = account.WeeklyStudyHours;
                    existingProfile.LearningStyle = account.LearningStyle;
                    existingProfile.KnownStrengthsJson = JsonSerializer.Serialize(account.Strengths);
                    existingProfile.KnownWeaknessesJson = JsonSerializer.Serialize(account.Weaknesses);
                    existingProfile.UpdatedAt = DateTime.UtcNow;
                }

                if (!await db.LearnerStates.AnyAsync(s => s.LearnerId == learner.Id))
                {
                    db.LearnerStates.Add(new LearnerState
                    {
                        Id = Guid.NewGuid(),
                        LearnerId = learner.Id,
                        ActiveStreakDays = 3,
                        TotalHoursLearned = account.WeeklyStudyHours * 2,
                        CompletedLessonsCount = 4,
                        CompletedQuizzesCount = 2,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                if (!await db.LearnerSettings.AnyAsync(s => s.LearnerId == learner.Id))
                {
                    db.LearnerSettings.Add(new LearnerSettings
                    {
                        Id = Guid.NewGuid(),
                        LearnerId = learner.Id,
                        PreferredYouTubeChannelsJson = JsonSerializer.Serialize(new[] { "freeCodeCamp" }),
                        PreferredTopicsJson = JsonSerializer.Serialize(new[] { account.Subject })
                    });
                }

                if (!await db.SkillMetrics.AnyAsync(s => s.LearnerId == learner.Id))
                {
                    var palette = new[] { "#2A9D68", "#E07A5F", "#7C5CFC", "#E11D48", "#EF4444" };
                    var icons = new[] { "Terminal", "Database", "Network", "KeyRound", "CheckCircle2" };
                    var skills = account.Strengths.Concat(account.Weaknesses).Take(5).ToArray();
                    for (var i = 0; i < skills.Length; i++)
                    {
                        var isWeak = account.Weaknesses.Contains(skills[i]);
                        db.SkillMetrics.Add(new SkillMetric
                        {
                            Id = Guid.NewGuid(),
                            LearnerId = learner.Id,
                            SkillName = skills[i],
                            CompetencyPercentage = isWeak ? 34 + i * 3 : 72 + i * 4,
                            HexColor = palette[i % palette.Length],
                            IconName = icons[i % icons.Length],
                            IsWeakArea = isWeak,
                            LastAssessedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Demo learners ensured ({Count}).", DemoAccounts.All.Length);
        }

        // -------------------------------------------------------------------
        // Public course catalog
        // -------------------------------------------------------------------
        private sealed record CourseSpec(
            string Title,
            string Description,
            string Category,
            string TargetAudience,
            string Badge,
            double Rating,
            string ReviewCount,
            string Duration,
            string Thumbnail,
            string Provider,
            string ShareCode,
            (string Title, string Summary)[] Modules);

        private static readonly CourseSpec[] Catalog =
        {
            new(
                "Python for Data Analysis",
                "Go from Python basics to cleaning, analyzing and visualizing real datasets with pandas and matplotlib.",
                "Data Science", "Beginner / Career switcher", "Popular", 4.8, "12.5k", "8 weeks",
                "/ifa.png", "IFA AI", "PYDATA01",
                new[]
                {
                    ("Python & the Data Mindset", "Syntax, data structures, and how to install and run Python for analysis."),
                    ("Working with pandas", "Series, DataFrames, filtering, grouping and aggregation."),
                    ("Cleaning Real Data", "Missing values, type conversion, duplicates and outliers."),
                    ("Visualizing Insights", "Charts with matplotlib and communicating findings."),
                    ("Capstone: Analyze a Dataset", "End-to-end analysis and a short written report.")
                }),

            new(
                "Full-Stack Web Development",
                "Build and deploy modern web apps with React on the frontend and Node/Express on the backend.",
                "Web Development", "Intermediate / Developer", "Bestseller", 4.7, "9.2k", "10 weeks",
                "/ifa.png", "IFA AI", "WEBFULL1",
                new[]
                {
                    ("Frontend Foundations", "HTML, CSS, modern JavaScript, and component thinking."),
                    ("React Components & State", "Props, state, hooks and composition."),
                    ("Backend APIs with Node", "Express routes, middleware and JSON contracts."),
                    ("Databases & Auth", "Persistence, sessions, and JWT-secured endpoints."),
                    ("Deploying a Full-Stack App", "Build, environment config, and a live deployment.")
                }),

            new(
                "Cybersecurity Essentials",
                "Understand threats, secure systems, and practice defensive security fundamentals.",
                "Security", "Beginner / IT professional", "Trending", 4.6, "6.3k", "6 weeks",
                "/ifa.png", "IFA AI", "CYSEC001",
                new[]
                {
                    ("Threat Landscape", "Common attack types, threat actors and the CIA triad."),
                    ("Securing Accounts", "Passwords, MFA, and identity best practices."),
                    ("Network Security Basics", "Firewalls, TLS, and safe network design."),
                    ("Web Application Security", "Injection, XSS, and OWASP Top 10 defenses."),
                    ("Incident Response", "Detecting, containing and learning from incidents.")
                }),

            new(
                "Machine Learning Foundations",
                "Learn the core ideas behind supervised and unsupervised learning with hands-on Python examples.",
                "AI/ML", "Intermediate / Analyst", "New", 4.7, "4.1k", "9 weeks",
                "/ifa.png", "IFA AI", "MLFOUND1",
                new[]
                {
                    ("What Is Machine Learning?", "Supervised, unsupervised and reinforcement learning."),
                    ("Data Preparation", "Features, labels, scaling and train/test splits."),
                    ("Regression & Classification", "Linear models, decision trees and evaluation metrics."),
                    ("Clustering & Dimensionality", "k-means, PCA and when to use them."),
                    ("Model Evaluation & Ethics", "Overfitting, bias, and responsible deployment.")
                }),

            new(
                "Cloud & DevOps Fundamentals",
                "Ship software reliably with cloud infrastructure, containers and CI/CD pipelines.",
                "Cloud", "Intermediate / Engineer", "Popular", 4.6, "7.8k", "7 weeks",
                "/ifa.png", "IFA AI", "CLOUDV01",
                new[]
                {
                    ("Cloud Concepts", "Regions, availability, IaaS/PaaS and cost awareness."),
                    ("Containers & Docker", "Images, containers, and reproducible environments."),
                    ("CI/CD Pipelines", "Automating build, test and deployment."),
                    ("Monitoring & Reliability", "Logs, metrics, alerts and SLOs."),
                    ("Infrastructure as Code", "Declarative provisioning and safe change.")
                }),

            new(
                "SQL & Database Design",
                "Design normalized schemas and write efficient SQL for real-world applications.",
                "Databases", "Beginner / Developer", "Bestseller", 4.8, "8.7k", "5 weeks",
                "/ifa.png", "IFA AI", "SQLDES01",
                new[]
                {
                    ("Relational Foundations", "Tables, keys, relationships and the relational model."),
                    ("Querying with SQL", "SELECT, JOINs, subqueries and aggregation."),
                    ("Designing Schemas", "Normalization, constraints and modeling trade-offs."),
                    ("Indexes & Performance", "How indexes work and reading query plans."),
                    ("Transactions & Integrity", "ACID, isolation levels and safe migrations.")
                }),

            new(
                "Medical Physiology Basics",
                "A clear, exam-focused introduction to how the major human body systems work together.",
                "Medicine & Health", "Beginner / Pre-med", null!, 4.7, "3.4k", "8 weeks",
                "/ifa.png", "IFA AI", "MEDPHY01",
                new[]
                {
                    ("Cells & Homeostasis", "Cell structure, transport and the internal environment."),
                    ("Cardiovascular System", "Heart, circulation, blood pressure and cardiac output."),
                    ("Respiratory System", "Gas exchange, ventilation and acid-base balance."),
                    ("Renal & Fluid Balance", "Filtration, reabsorption and electrolyte control."),
                    ("Nervous & Endocrine Control", "Signals, reflexes and hormonal regulation.")
                }),

            new(
                "Business Analytics with Spreadsheets",
                "Turn raw business data into decisions using spreadsheets, charts and simple forecasting.",
                "Business", "Beginner / Professional", "New", 4.5, "2.9k", "4 weeks",
                "/ifa.png", "IFA AI", "BIZAN001",
                new[]
                {
                    ("Spreadsheet Foundations", "Formulas, references and clean tabular data."),
                    ("Summarizing Data", "Pivot tables, grouping and key metrics."),
                    ("Visualizing Performance", "Charts and dashboards that communicate."),
                    ("Forecasting Basics", "Trends, seasonality and simple projections.")
                })
        };

        private static async Task SeedCoursesAsync(ApplicationDbContext db, ILogger logger)
        {
            foreach (var spec in Catalog)
            {
                if (await db.Courses.AnyAsync(c => c.Title == spec.Title)) continue;

                var course = new Course
                {
                    Id = Guid.NewGuid(),
                    Title = spec.Title,
                    Description = spec.Description,
                    Category = spec.Category,
                    TargetAudience = spec.TargetAudience,
                    ThumbnailUrl = spec.Thumbnail,
                    ProviderName = spec.Provider,
                    Badge = spec.Badge,
                    Rating = spec.Rating,
                    ReviewCount = spec.ReviewCount,
                    EstimatedDuration = spec.Duration,
                    IsPublic = true,
                    ShareCode = spec.ShareCode,
                    CreatedAt = DateTime.UtcNow
                };

                var number = 0;
                foreach (var (title, summary) in spec.Modules)
                {
                    number++;
                    var module = new Module
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        ModuleNumber = number,
                        Title = title,
                        Summary = summary,
                        EstimatedHours = 4,
                        GenerationStatus = ModuleGenerationStatus.Blueprint
                    };

                    // Materialize module 1 so enrolled learners see content
                    // immediately; later modules stay as blueprints to exercise
                    // the just-in-time "Generate module" flow.
                    if (number == 1)
                    {
                        MaterializeModule(course, module);
                    }

                    course.Modules.Add(module);
                }

                db.Courses.Add(course);
                logger.LogInformation("Seeded demo course {Title}.", spec.Title);
            }

            await db.SaveChangesAsync();
        }

        private static void MaterializeModule(Course course, Module module)
        {
            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                LessonNumber = 1,
                Title = $"Introduction to {module.Title}",
                Summary = module.Summary,
                ReadingTimeMinutes = 14,
                ContentMarkdown =
                    $"# Introduction to {module.Title}\n\n" +
                    $"This module is part of **{course.Title}**. {module.Summary}\n\n" +
                    "## Why it matters\n" +
                    "Strong foundations make later topics easier. Spending a little time to understand the core idea now saves a lot of confusion later. " +
                    "As you read, keep asking: what problem does this solve, and when would I reach for it?\n\n" +
                    "## Core idea\n" +
                    "Break the topic into small, testable pieces. State the goal, list the inputs and expected output, then work through one concrete example step by step. " +
                    "Check each step against the original statement before moving on, and pay attention to edge cases such as empty, very large, or invalid input.\n\n" +
                    "## Worked example\n" +
                    "1. Restate the task in one sentence.\n" +
                    "2. Identify the known information and the unknown.\n" +
                    "3. Choose the simplest method that applies.\n" +
                    "4. Apply it carefully, writing each step down.\n" +
                    "5. Verify the result and sanity-check the magnitude or behaviour.\n\n" +
                    "## Practice\n" +
                    "Complete the independent exercise below. Write down your reasoning, then compare it with the worked example to find any gaps.",
                CreatedAt = DateTime.UtcNow
            };

            var quiz = BuildQuiz(module);
            quiz.Kind = QuizKind.Exam;
            quiz.OrderIndex = 1;

            module.Lessons.Add(lesson);
            module.Quizzes.Add(quiz);
            module.GenerationStatus = ModuleGenerationStatus.Ready;
            module.GeneratedAt = DateTime.UtcNow;
        }

        private static Quiz BuildQuiz(Module module)
        {
            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                Title = $"{module.Title} Knowledge Check",
                PassingScorePercentage = 70
            };

            var questions = new (string Prompt, string[] Options, int Correct, string Explanation)[]
            {
                (
                    $"What is the main goal of the module \"{module.Title}\"?",
                    new[] { "To memorise unrelated facts", $"To build a working understanding of {module.Title}", "To skip the fundamentals", "To replace all practice with theory" },
                    1,
                    "A focused module builds working understanding you can apply, not isolated facts."
                ),
                (
                    "What is a good first step when solving a new problem?",
                    new[] { "Guess an answer quickly", "Restate the problem and the expected output", "Copy a finished solution", "Ignore the requirements" },
                    1,
                    "Restating the problem and expected output turns a vague task into something testable."
                ),
                (
                    "Why should you check edge cases?",
                    new[] { "They never occur in practice", "They reveal failures the happy path hides", "They make code slower", "They are only for exams" },
                    1,
                    "Edge cases such as empty or invalid input often expose defects the normal path never reaches."
                ),
                (
                    "Which habit improves long-term retention?",
                    new[] { "Reading once and moving on", "Practising with small exercises", "Avoiding feedback", "Skipping summaries" },
                    1,
                    "Active practice with feedback strengthens retention far more than passive reading."
                ),
                (
                    "What does clear separation of concerns help you do?",
                    new[] { "Remove the need for tests", "Change one part without breaking others", "Write less documentation", "Avoid all errors" },
                    1,
                    "Clear boundaries let you change and test one part in isolation, reducing accidental coupling."
                )
            };

            foreach (var (prompt, options, correct, explanation) in questions)
            {
                quiz.Questions.Add(new Question
                {
                    Id = Guid.NewGuid(),
                    QuizId = quiz.Id,
                    Prompt = prompt,
                    Options = options.ToList(),
                    CorrectOptionIndex = correct,
                    Explanation = explanation,
                    TargetSkillName = module.Title,
                    BloomTaxonomyLevel = "Understand"
                });
            }

            return quiz;
        }

        // -------------------------------------------------------------------
        // Enrollments with realistic progress
        // -------------------------------------------------------------------
        private static readonly (string Email, string CourseTitle, int Progress, int DaysAgo)[] Enrollments =
        {
            ("nathnel@ifa.local", "SQL & Database Design", 55, 1),
            ("nathnel@ifa.local", "Python for Data Analysis", 20, 3),
            ("ermiyas@ifa.local", "SQL & Database Design", 80, 1),
            ("ermiyas@ifa.local", "Cloud & DevOps Fundamentals", 35, 2),
            ("negede@ifa.local", "Python for Data Analysis", 45, 1),
            ("negede@ifa.local", "Full-Stack Web Development", 15, 4),
            ("hanna@ifa.local", "Medical Physiology Basics", 60, 2),
            ("hanna@ifa.local", "Cybersecurity Essentials", 10, 5)
        };

        private static async Task SeedEnrollmentsAsync(ApplicationDbContext db, ILogger logger)
        {
            foreach (var (email, courseTitle, progress, daysAgo) in Enrollments)
            {
                var learner = await db.Learners.FirstOrDefaultAsync(l => l.Email == email);
                var course = await db.Courses.Include(c => c.Modules).ThenInclude(m => m.Lessons)
                    .FirstOrDefaultAsync(c => c.Title == courseTitle);
                if (learner is null || course is null) continue;

                if (await db.CourseEnrollments.AnyAsync(e => e.CourseId == course.Id && e.LearnerId == learner.Id))
                    continue;

                var accessed = DateTime.UtcNow.AddDays(-daysAgo);
                db.CourseEnrollments.Add(new CourseEnrollment
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LearnerId = learner.Id,
                    ProgressPercentage = progress,
                    EnrolledAt = accessed.AddDays(-14),
                    LastAccessedAt = accessed
                });

                // Mark lessons in the ready first module complete to match progress.
                var firstModule = course.Modules.OrderBy(m => m.ModuleNumber).FirstOrDefault();
                if (firstModule is not null && progress >= 40)
                {
                    foreach (var lesson in firstModule.Lessons)
                    {
                        if (await db.Set<LessonProgress>().AnyAsync(p => p.LessonId == lesson.Id && p.LearnerId == learner.Id))
                            continue;
                        db.Set<LessonProgress>().Add(new LessonProgress
                        {
                            Id = Guid.NewGuid(),
                            LessonId = lesson.Id,
                            LearnerId = learner.Id,
                            IsCompleted = true,
                            CompletedAt = accessed
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Demo enrollments ensured.");
        }

        // -------------------------------------------------------------------
        // Activities & notifications
        // -------------------------------------------------------------------
        private static async Task SeedSocialAsync(ApplicationDbContext db, ILogger logger)
        {
            var demoLearner = await db.Learners.FirstOrDefaultAsync(l => l.Email == "nathnel@ifa.local");
            if (demoLearner is null) return;

            if (!await db.Activities.AnyAsync(a => a.LearnerId == demoLearner.Id))
            {
                db.Activities.AddRange(
                    new LearningActivity
                    {
                        Id = Guid.NewGuid(), LearnerId = demoLearner.Id, ActivityType = "quiz_passed",
                        Title = "Passed a quiz", Description = "SQL & Database Design — Querying with SQL",
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new LearningActivity
                    {
                        Id = Guid.NewGuid(), LearnerId = demoLearner.Id, ActivityType = "lesson_completed",
                        Title = "Completed a lesson", Description = "Python & the Data Mindset",
                        CreatedAt = DateTime.UtcNow.AddHours(-5)
                    },
                    new LearningActivity
                    {
                        Id = Guid.NewGuid(), LearnerId = demoLearner.Id, ActivityType = "research_completed",
                        Title = "Research completed", Description = "Grounded new module in ScholarXiv sources",
                        CreatedAt = DateTime.UtcNow.AddHours(-9)
                    },
                    new LearningActivity
                    {
                        Id = Guid.NewGuid(), LearnerId = demoLearner.Id, ActivityType = "course_started",
                        Title = "Started a course", Description = "Cloud & DevOps Fundamentals",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    });
            }

            if (!await db.Notifications.AnyAsync(n => n.LearnerId == demoLearner.Id))
            {
                db.Notifications.AddRange(
                    new Notification
                    {
                        Id = Guid.NewGuid(), LearnerId = demoLearner.Id, Type = "achievement",
                        Title = "3-day learning streak!", Message = "Keep it going — you're building momentum.",
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    },
                    new Notification
                    {
                        Id = Guid.NewGuid(), LearnerId = demoLearner.Id, Type = "recommendation",
                        Title = "New recommendation", Message = "Based on your SQL progress, try 'Machine Learning Foundations'.",
                        LinkUrl = "/recommendations", CreatedAt = DateTime.UtcNow.AddHours(-6)
                    },
                    new Notification
                    {
                        Id = Guid.NewGuid(), LearnerId = demoLearner.Id, Type = "course_update",
                        Title = "Module ready", Message = "A new module was generated for your course.",
                        CreatedAt = DateTime.UtcNow.AddDays(-2), IsRead = true
                    });
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Demo social data ensured.");
        }
    }
}
