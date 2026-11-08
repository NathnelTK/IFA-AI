using System;
using System.Collections.Generic;
using IFA.Application.Common.Models;

namespace IFA.Application.Common.Services
{
    public static class CourseTemplateCatalog
    {
        public static IReadOnlyList<CourseTemplateDto> GetAll()
        {
            var templates = new List<CourseTemplateDto>
            {
                new CourseTemplateDto
                {
                    Id = "bootcamp",
                    Title = "Programming Bootcamp",
                    Description = "Intensive, practical hands-on curriculum with coding labs, architecture exercises, unit tests, and a production capstone project.",
                    Category = "Software Engineering",
                    TargetAudience = "Aspiring Software Engineers & Developers",
                    EstimatedDuration = "12 weeks",
                    Level = "Beginner to Advanced",
                    AccentColor = "#1B3D2F",
                    ThemeMode = "pine",
                    Icon = "Terminal",
                    KeyFeatures = new List<string> { "Hands-on coding labs", "Code reviews & test suites", "Architecture blueprints", "Capstone project" },
                    SampleAssignmentTitle = "Capstone: Full-Stack Microservices Architecture",
                    SampleAssignmentInstructions = "Design and build a complete RESTful microservice API with database persistence, JWT authentication, and automated integration tests.",
                    Modules = new List<CourseTemplateModuleDto>
                    {
                        new CourseTemplateModuleDto
                        {
                            Title = "Core Foundations & Environment Setup",
                            Summary = "Development tools, Git workflows, runtime fundamentals, and core programming concepts.",
                            EstimatedHours = 8,
                            QuizTitle = "Foundations Knowledge Check",
                            QuizQuestions = new List<string> { "What is the difference between value types and reference types?", "How do Git feature branches facilitate team development?" },
                            Lessons = new List<CourseTemplateLessonDto>
                            {
                                new CourseTemplateLessonDto
                                {
                                    Title = "Developer Workspace & Tooling Setup",
                                    Summary = "Configuring runtime environments, linters, debuggers, and version control.",
                                    ContentMarkdown = "# Developer Workspace & Tooling Setup\n\nWelcome to the Bootcamp! In this foundational lesson, you will configure a production-ready development environment.\n\n## Objectives\n- Install and verify the latest SDK and runtime\n- Initialize a standardized Git repository with pre-commit hooks\n- Configure formatting and static analysis rules\n\n```bash\n# Verify your CLI installation\ndotnet --version\nnode -v\n```\n\n## Key Takeaways\nConsistent development tooling eliminates cross-platform configuration friction."
                                },
                                new CourseTemplateLessonDto
                                {
                                    Title = "Data Structures & Algorithmic Complexity",
                                    Summary = "Evaluating time and space complexity with Big-O notation in real applications.",
                                    ContentMarkdown = "# Data Structures & Algorithmic Complexity\n\nWriting performant code requires understanding how memory and operations scale with input size.\n\n## Big-O Analysis\n- **O(1)**: Constant time lookups (e.g. HashMaps/Dictionaries)\n- **O(log N)**: Binary search trees\n- **O(N)**: Single pass linear iteration\n- **O(N log N)**: Optimal comparison sorting\n\nAlways select data structures tailored to your access patterns."
                                }
                            }
                        },
                        new CourseTemplateModuleDto
                        {
                            Title = "Backend Architecture & Database Design",
                            Summary = "Relational database schema modeling, indexing strategies, and clean API design.",
                            EstimatedHours = 12,
                            QuizTitle = "Backend & Data Tier Evaluation",
                            QuizQuestions = new List<string> { "Explain database normalization up to 3NF.", "What are the core principles of RESTful APIs?" },
                            Lessons = new List<CourseTemplateLessonDto>
                            {
                                new CourseTemplateLessonDto
                                {
                                    Title = "Relational Modeling & Normalization",
                                    Summary = "Designing scalable database schemas with foreign key constraints and indexes.",
                                    ContentMarkdown = "# Relational Modeling & Normalization\n\nData integrity is the bedrock of reliable systems. In this lesson, we break down normalized entity relationships."
                                },
                                new CourseTemplateLessonDto
                                {
                                    Title = "Designing RESTful Web APIs",
                                    Summary = "HTTP status codes, idempotency, resource-oriented routing, and error contracts.",
                                    ContentMarkdown = "# Designing RESTful Web APIs\n\nA disciplined REST API adheres to strict HTTP semantics and predictable payloads."
                                }
                            }
                        }
                    }
                },
                new CourseTemplateDto
                {
                    Id = "academic",
                    Title = "Academic Subject",
                    Description = "Rigorous theoretical foundations, peer-reviewed paper citations, comprehensive reading lists, and mid-term/final examinations.",
                    Category = "Computer Science & Academia",
                    TargetAudience = "University Students & Researchers",
                    EstimatedDuration = "16 weeks",
                    Level = "Intermediate to Advanced",
                    AccentColor = "#1E293B",
                    ThemeMode = "slate",
                    Icon = "GraduationCap",
                    KeyFeatures = new List<string> { "Peer-reviewed citations (OpenAlex, arXiv)", "Theoretical proofs", "Socratic seminars", "Comprehensive exams" },
                    SampleAssignmentTitle = "Literature Review & Research Paper",
                    SampleAssignmentInstructions = "Write a 2,000-word critical literature review synthesizing at least 8 peer-reviewed papers on recent algorithmic advances.",
                    Modules = new List<CourseTemplateModuleDto>
                    {
                        new CourseTemplateModuleDto
                        {
                            Title = "Theoretical Foundations & Literature Survey",
                            Summary = "Historical context, foundational theorems, and state-of-the-art literature.",
                            EstimatedHours = 6,
                            QuizTitle = "Theoretical Principles Exam",
                            QuizQuestions = new List<string> { "State and explain Church-Turing thesis implications.", "Analyze the computational complexity classes P vs NP." },
                            Lessons = new List<CourseTemplateLessonDto>
                            {
                                new CourseTemplateLessonDto
                                {
                                    Title = "Foundational Paradigms & Formal Proofs",
                                    Summary = "Mathematical modeling and formal proof techniques.",
                                    ContentMarkdown = "# Foundational Paradigms & Formal Proofs\n\nThis academic module investigates rigorous mathematical specifications and formal correctness proofs."
                                }
                            }
                        }
                    }
                },
                new CourseTemplateDto
                {
                    Id = "exam-prep",
                    Title = "Exam Preparation",
                    Description = "High-yield diagnostic assessments, time-boxed question sets, detailed rationales, and targeted weakness remediation.",
                    Category = "Certification & Exit Exams",
                    TargetAudience = "Candidates Preparing for National Exit Exams & Certifications",
                    EstimatedDuration = "4 weeks",
                    Level = "All Levels",
                    AccentColor = "#065F46",
                    ThemeMode = "emerald",
                    Icon = "ClipboardCheck",
                    KeyFeatures = new List<string> { "Diagnostic baseline test", "High-yield summary cheat sheets", "Time-boxed practice exams", "Weakness radar analysis" },
                    SampleAssignmentTitle = "Mock Certification Exam Attempt",
                    SampleAssignmentInstructions = "Complete the 50-question mock exam within 90 minutes and submit your self-assessment notes identifying targeted review domains.",
                    Modules = new List<CourseTemplateModuleDto>
                    {
                        new CourseTemplateModuleDto
                        {
                            Title = "High-Yield Review & Core Competencies",
                            Summary = "Identify the topics in your exam syllabus and plan targeted practice.",
                            EstimatedHours = 5,
                            QuizTitle = "High-Yield Practice Examination",
                            QuizQuestions = new List<string> { "Select the correct output for concurrent transactional isolation.", "Which security vulnerability is mitigated by parameterized queries?" },
                            Lessons = new List<CourseTemplateLessonDto>
                            {
                                new CourseTemplateLessonDto
                                {
                                    Title = "Critical Concepts & Fast-Recall Strategies",
                                    Summary = "Memorization techniques, decision trees, and exam traps to avoid.",
                                    ContentMarkdown = "# Critical Concepts & Fast-Recall Strategies\n\nMaximize your exam score by mastering high-probability questions and rapid elimination strategies."
                                }
                            }
                        }
                    }
                },
                new CourseTemplateDto
                {
                    Id = "masterclass",
                    Title = "Video-Based Masterclass",
                    Description = "Cinematic video lectures, instructor demonstrations, companion workbooks, and downloadable cheat sheets.",
                    Category = "Professional Development",
                    TargetAudience = "Professionals seeking expert tradecraft & executive insights",
                    EstimatedDuration = "6 weeks",
                    Level = "All Levels",
                    AccentColor = "#312E81",
                    ThemeMode = "indigo",
                    Icon = "Video",
                    KeyFeatures = new List<string> { "Curated video lectures", "Interactive workbooks", "Downloadable templates", "Real-world case studies" },
                    SampleAssignmentTitle = "Executive Strategic Plan",
                    SampleAssignmentInstructions = "Produce an executive presentation and implementation roadmap applying the masterclass principles to your organization.",
                    Modules = new List<CourseTemplateModuleDto>
                    {
                        new CourseTemplateModuleDto
                        {
                            Title = "Executive Frameworks & High-Impact Case Studies",
                            Summary = "Analyzing proven market leaders and architectural blueprints.",
                            EstimatedHours = 4,
                            QuizTitle = "Strategy & Implementation Review",
                            QuizQuestions = new List<string> { "What is the primary driver of organizational velocity?", "How do leading teams structure asynchronous communication?" },
                            Lessons = new List<CourseTemplateLessonDto>
                            {
                                new CourseTemplateLessonDto
                                {
                                    Title = "The Masterclass Framework Revealed",
                                    Summary = "Deconstructing the core method step by step with accompanying video walkthrough.",
                                    ContentMarkdown = "# The Masterclass Framework Revealed\n\nIn this keynote video session, we analyze the core framework used by high-performance organizations worldwide."
                                }
                            }
                        }
                    }
                },
                new CourseTemplateDto
                {
                    Id = "project-based",
                    Title = "Project-Based Course",
                    Description = "Learning by building: milestone-driven sprints, architectural specifications, rubric-graded deliverables, and peer code reviews.",
                    Category = "Engineering & Design",
                    TargetAudience = "Learners who learn best by creating tangible artifacts",
                    EstimatedDuration = "8 weeks",
                    Level = "Intermediate",
                    AccentColor = "#0F766E",
                    ThemeMode = "teal",
                    Icon = "Layers",
                    KeyFeatures = new List<string> { "Milestone-driven project sprints", "Interactive rubric assessments", "Portfolio artifact deliverables", "Peer feedback loops" },
                    SampleAssignmentTitle = "Sprint 1 Deliverable: Core System Prototype",
                    SampleAssignmentInstructions = "Deploy a functional MVP to staging, submit your repository link, and include a 2-minute Loom walkthrough.",
                    Modules = new List<CourseTemplateModuleDto>
                    {
                        new CourseTemplateModuleDto
                        {
                            Title = "Sprint 0: Architecture & Prototyping",
                            Summary = "Requirement analysis, wireframing, tech stack selection, and skeleton repository.",
                            EstimatedHours = 6,
                            QuizTitle = "Sprint Architecture Review",
                            QuizQuestions = new List<string> { "Why is schema validation essential at API boundaries?", "What are the trade-offs of single-repo vs multi-repo architecture?" },
                            Lessons = new List<CourseTemplateLessonDto>
                            {
                                new CourseTemplateLessonDto
                                {
                                    Title = "Defining the Product Scope & Wireframes",
                                    Summary = "Creating realistic project requirements and entity diagrams.",
                                    ContentMarkdown = "# Defining the Product Scope & Wireframes\n\nBefore writing code, establish clear technical specifications and user journey maps."
                                }
                            }
                        }
                    }
                },
                new CourseTemplateDto
                {
                    Id = "cohort",
                    Title = "Live Cohort Course",
                    Description = "Structured cohort learning with scheduled weekly live classes, group discussions, deadline accountability, and community Q&A.",
                    Category = "Cohort Learning & Community",
                    TargetAudience = "Students seeking instructor interaction and cohort accountability",
                    EstimatedDuration = "6 weeks",
                    Level = "All Levels",
                    AccentColor = "#7C2D12",
                    ThemeMode = "amber",
                    Icon = "Users",
                    KeyFeatures = new List<string> { "Weekly live interactive sessions", "Cohort discussion forums", "Peer review circles", "Shared milestones & accountability" },
                    SampleAssignmentTitle = "Cohort Collaboration Challenge",
                    SampleAssignmentInstructions = "Participate in this week's cohort workshop and post your team solution to the course discussion board.",
                    Modules = new List<CourseTemplateModuleDto>
                    {
                        new CourseTemplateModuleDto
                        {
                            Title = "Week 1: Cohort Kickoff & Group Immersion",
                            Summary = "Meet your cohort, schedule live sessions, and master the initial sprint.",
                            EstimatedHours = 4,
                            QuizTitle = "Orientation & Week 1 Check-In",
                            QuizQuestions = new List<string> { "What is our team norm for code review turnaround?", "When do weekly live sessions take place?" },
                            Lessons = new List<CourseTemplateLessonDto>
                            {
                                new CourseTemplateLessonDto
                                {
                                    Title = "Welcome to the Cohort: Schedule & Community Norms",
                                    Summary = "How live sessions, discussion boards, and assignments work together.",
                                    ContentMarkdown = "# Welcome to the Cohort\n\nLive learning thrives on active participation! Check the Live Sessions tab for your weekly meeting links and join the Discussion forum."
                                }
                            }
                        }
                    }
                },
                new CourseTemplateDto
                {
                    Id = "microlearning",
                    Title = "Short Microlearning Course",
                    Description = "Bite-sized 5-minute modules focused on a single outcome, quick checks for understanding, and immediate application.",
                    Category = "Productivity & Rapid Upskilling",
                    TargetAudience = "Busy professionals wanting rapid, laser-focused skills",
                    EstimatedDuration = "1-2 weeks",
                    Level = "Beginner to Intermediate",
                    AccentColor = "#4338CA",
                    ThemeMode = "indigo",
                    Icon = "Zap",
                    KeyFeatures = new List<string> { "5-minute focused lessons", "Instant practical takeaways", "Quick 2-question checks", "Actionable checklists" },
                    SampleAssignmentTitle = "Rapid Implementation Exercise",
                    SampleAssignmentInstructions = "Apply the 5-minute lesson technique to your daily workflow and note your productivity improvement.",
                    Modules = new List<CourseTemplateModuleDto>
                    {
                        new CourseTemplateModuleDto
                        {
                            Title = "Micro-Skill 1: Rapid Mastery",
                            Summary = "A single, focused capability explained and demonstrated in under 5 minutes.",
                            EstimatedHours = 1,
                            QuizTitle = "Instant Concept Check",
                            QuizQuestions = new List<string> { "What is the single most important rule of clean functions?" },
                            Lessons = new List<CourseTemplateLessonDto>
                            {
                                new CourseTemplateLessonDto
                                {
                                    Title = "The 5-Minute Rule for Clean Code",
                                    Summary = "One transformative tip you can apply today.",
                                    ContentMarkdown = "# The 5-Minute Rule for Clean Code\n\nKeep functions small, do one thing, and name variables with clear intent.\n\n## Summary Checklist\n- [ ] Single Responsibility Principle\n- [ ] Predictable return types\n- [ ] Meaningful naming"
                                }
                            }
                        }
                    }
                }
            };
            foreach (var template in templates)
            {
                template.LearningObjectives = template.Id switch
                {
                    "bootcamp" => new() { "Configure a development environment and use version control.", "Design and build a tested API backed by a relational database." },
                    "academic" => new() { "Explain the subject's foundational concepts.", "Evaluate evidence and present a supported academic argument." },
                    "exam-prep" => new() { "Identify gaps using practice assessments.", "Apply a structured review plan to the selected exam syllabus." },
                    "masterclass" => new() { "Explain the instructor's method.", "Apply the method to a documented case study." },
                    "project-based" => new() { "Define a feasible project scope.", "Build and present a working project with documented design decisions." },
                    "cohort" => new() { "Apply the course concepts during guided sessions.", "Collaborate on a shared challenge and reflect on feedback." },
                    _ => new() { "Apply one focused technique to a practical task." }
                };
                template.AssessmentSuggestions = template.Id switch
                {
                    "bootcamp" => new() { "Short knowledge check after each technical module", "Tested capstone application with a review rubric" },
                    "academic" => new() { "Source-based essay or problem set", "Cumulative examination mapped to course outcomes" },
                    "exam-prep" => new() { "Timed diagnostic test", "Full mock examination with targeted remediation" },
                    "masterclass" => new() { "Technique analysis quiz", "Recorded or documented case-study application" },
                    "project-based" => new() { "Milestone review at each build phase", "Final demonstration evaluated with a project rubric" },
                    "cohort" => new() { "Live-session reflection", "Collaborative challenge and final presentation" },
                    _ => new() { "Focused knowledge check", "One practical application task" }
                };
            }
            return templates;
        }
    }
}
