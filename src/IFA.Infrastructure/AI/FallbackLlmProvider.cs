using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.AI
{
    public class FallbackLlmProvider
    {
        public Task<string> CompleteAsync(string systemPrompt, string userPrompt, LlmRole role = LlmRole.General, CancellationToken ct = default)
        {
            var topic = ExtractTopic(userPrompt);

            if (role == LlmRole.Model1_Intake)
            {
                var profileJson = @"{
  ""learnerProfile"": {
    ""name"": ""Demo User"",
    ""email"": ""demo@ifa.com"",
    ""learningGoal"": ""Master " + topic + @" fundamentals and practical implementation"",
    ""subject"": """ + topic + @""",
    ""currentLevel"": ""Intermediate"",
    ""targetOutcome"": ""Exit Exam Ready"",
    ""weeklyStudyHours"": 6,
    ""preferredLearningStyle"": ""Hands-on with theory"",
    ""industryContext"": ""Software Engineering"",
    ""priorKnowledge"": [""Programming Fundamentals"", ""System Design Basics""],
    ""researchInterests"": [""Best Practices"", ""Performance Optimization"", ""Real-world Applications""]
  }
}";
                var response = $"Great choice! Studying **{topic}** will give you high-leverage skills. I've analyzed your goal and prepared your personalized learner profile.\n\n```json\n{profileJson.Trim()}\n```";
                return Task.FromResult(response);
            }

            if (role == LlmRole.Model2_Architect)
            {
                var blueprint = @"{
  ""courseBlueprint"": {
    ""title"": ""Complete " + topic + @" Mastery Path"",
    ""description"": ""A comprehensive learning journey covering fundamental concepts, practical applications, and advanced techniques in " + topic + @"."",
    ""estimatedDurationWeeks"": 8,
    ""difficultyLevel"": ""Intermediate to Advanced"",
    ""learningObjectives"": [
      ""Understand core " + topic + @" principles and concepts"",
      ""Apply " + topic + @" techniques to solve real-world problems"",
      ""Implement best practices for " + topic + @" development"",
      ""Analyze and optimize " + topic + @" performance"",
      ""Design scalable " + topic + @" solutions""
    ],
    ""modules"": [
      {
        ""title"": ""Foundations of " + topic + @""",
        ""description"": ""Essential concepts and principles"",
        ""estimatedDurationWeeks"": 2,
        ""learningObjectives"": [""Master fundamental concepts"", ""Understand core principles""]
      },
      {
        ""title"": ""Practical " + topic + @" Implementation"",
        ""description"": ""Hands-on development and coding"",
        ""estimatedDurationWeeks"": 2,
        ""learningObjectives"": [""Build working solutions"", ""Apply best practices""]
      },
      {
        ""title"": ""Advanced " + topic + @" Patterns"",
        ""description"": ""Complex scenarios and optimization"",
        ""estimatedDurationWeeks"": 2,
        ""learningObjectives"": [""Implement advanced patterns"", ""Optimize performance""]
      },
      {
        ""title"": """ + topic + @" in Production"",
        ""description"": ""Deployment, monitoring, and scaling"",
        ""estimatedDurationWeeks"": 2,
        ""learningObjectives"": [""Deploy to production"", ""Monitor and scale systems""]
      }
    ],
    ""prerequisites"": [""Programming Fundamentals"", ""Basic System Design""],
    ""assessmentStrategy"": ""Continuous assessment with practical projects and knowledge checks""
  }
}";
                return Task.FromResult($"```json\n{blueprint.Trim()}\n```");
            }

            if (role == LlmRole.Model3_Builder)
            {
                var moduleResult = @"{
  ""lesson"": {
    ""title"": ""In-Depth: Core Mechanics and Applied Architecture of " + topic + @""",
    ""summary"": ""A rigorous dive into real-world mechanics, patterns, and performance considerations for " + topic + @"."",
    ""contentMarkdown"": ""# Core Mechanics and Applied Architecture of " + topic + @"\n\n## Executive Summary\nWhen building systems with **" + topic + @"**, architectural consistency and clean separation of concerns prevent technical debt.\n\n## 1. Architectural Foundations\nEvery robust architecture enforces clear boundaries:\n- **Domain**: Pure business rules and invariant checks.\n- **Application**: Orchestration, commands, queries, and use cases.\n- **Infrastructure**: Network, database, and hardware I/O.\n\n## 2. Best Practices\n1. Minimize heap allocations in high-frequency loops.\n2. Validate inputs before executing domain state transitions.\n3. Keep methods focused on single responsibilities."",
    ""readingTimeMinutes"": 15,
    ""youTubeVideoId"": ""dQw4w9WgXcQ"",
    ""youTubeVideoTitle"": ""Complete Guide to " + topic + @" Production Architecture"",
    ""scholarxivCitationDoi"": ""10.48550/arXiv.2401.00123"",
    ""scholarxivPaperTitle"": ""Scalable Architectures and Verification in Modern " + topic + @""",
    ""keyTakeaways"": [
      ""Strict interface boundaries isolate domain logic from external dependencies."",
      ""Async execution models with cancellation tokens ensure system resilience."",
      ""Continuous assessment and telemetry reveal bottlenecks early.""
    ],
    ""exercises"": [
      {
        ""instruction"": ""Define an interface and implementation for processing requests."",
        ""hint"": ""Pass CancellationToken through to all async method calls."",
        ""starterCode"": ""public async Task ProcessAsync(CancellationToken ct) { }""
      }
    ]
  },
  ""quiz"": {
    ""title"": """ + topic + @" Formative Mastery Assessment"",
    ""passingScorePercentage"": 70,
    ""questions"": [
      {
        ""prompt"": ""What is the main benefit of decoupling domain logic from infrastructure?"",
        ""options"": [
          ""It eliminates the need for database storage"",
          ""It allows testing business rules independently"",
          ""It converts dynamic types into binaries"",
          ""It forces synchronous operations""
        ],
        ""correctOptionIndex"": 1,
        ""explanation"": ""Decoupling allows business logic to be tested independently of external systems."",
        ""targetSkillName"": ""System Architecture"",
        ""bloomTaxonomyLevel"": ""Analyze""
      }
    ]
  }
}";
                return Task.FromResult($"```json\n{moduleResult.Trim()}\n```");
            }

            if (role == LlmRole.Tutor)
            {
                var tutorText = $"I understand you're working with **{topic}**! Let me guide you through this step by step.\n\n" +
                    $"1. **Key Insight:** The most important principle in {topic} is maintaining clear separation between concerns and ensuring proper error handling.\n" +
                    $"2. **Practical Tip:** Break your implementation into small, testable units with clear contracts.\n" +
                    $"3. **Next Steps:** Would you like to review a concrete code example, test your knowledge with a practice question, or explore how this applies to your current project?";
                return Task.FromResult(tutorText);
            }

            return Task.FromResult($"Processed {topic} successfully.");
        }

        private static string ExtractTopic(string prompt)
        {
            // Simple topic extraction - in real implementation this would be more sophisticated
            var words = prompt.Split(' ');
            foreach (var word in words)
            {
                if (word.Length > 3 && char.IsUpper(word[0]))
                {
                    return word.Trim(',', '.', '!', '?');
                }
            }
            return "Software Engineering";
        }
    }
}