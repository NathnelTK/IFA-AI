using System.Text.Json;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.AI
{
    /// <summary>
    /// Deterministic, offline-safe provider used when no hosted LLM is reachable.
    /// Every payload is built as a real object and serialized, so the JSON always
    /// matches the schema the consuming parser expects. This keeps the AI pipeline
    /// (intake -> research -> generation) working with zero API keys.
    /// </summary>
    public class FallbackLlmProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        public Task<string> CompleteAsync(string systemPrompt, string userPrompt, LlmRole role = LlmRole.General, CancellationToken ct = default)
        {
            var topic = ExtractTopic(userPrompt);

            if (role == LlmRole.Model1_Intake)
            {
                var profile = new
                {
                    learningGoal = topic,
                    subject = topic,
                    currentLevel = "Beginner",
                    targetOutcome = $"Confidently build and ship a working {topic} project",
                    weeklyStudyHours = 6,
                    preferredLanguage = "en",
                    learningStyle = "Hands-on",
                    constraints = "Self-paced",
                    preferredYouTubeChannels = new[] { "freeCodeCamp", "Traversy Media" },
                    knownStrengths = new[] { "Basic programming", "Logic" },
                    knownWeaknesses = new[] { "Applying concepts in real projects", "Debugging" },
                    suggestedResearchTopics = new[] { $"Best practices for {topic}", $"Common pitfalls in {topic}" }
                };

                var json = JsonSerializer.Serialize(profile, JsonOptions);
                var response =
                    $"Great choice! Studying **{topic}** will give you high-leverage skills. " +
                    $"I've analyzed your goal and prepared your personalized learner profile.\n\n```json\n{json}\n```";
                return Task.FromResult(response);
            }

            if (role == LlmRole.Model2_Architect)
            {
                var blueprint = new
                {
                    courseTitle = $"Personalized {topic} Course",
                    targetGoal = $"Master {topic} from fundamentals to a working project",
                    totalEstimatedHours = 20,
                    modules = new object[]
                    {
                        new { moduleNumber = 1, title = $"Foundations of {topic}", summary = $"Essential concepts, vocabulary, and the mental model behind {topic}.", estimatedHours = 4, keyTopics = new[] { "Core concepts", "Terminology", "Environment setup" } },
                        new { moduleNumber = 2, title = $"Practical {topic} Implementation", summary = $"Hands-on application of {topic} through guided examples and exercises.", estimatedHours = 4, keyTopics = new[] { "Guided examples", "Practice exercises", "Debugging" } },
                        new { moduleNumber = 3, title = $"Intermediate {topic} Patterns", summary = "Common patterns and techniques that separate beginners from practitioners.", estimatedHours = 4, keyTopics = new[] { "Design patterns", "Refactoring", "Testing" } },
                        new { moduleNumber = 4, title = $"Advanced {topic} Techniques", summary = $"Performance, reliability, and advanced scenarios in {topic}.", estimatedHours = 4, keyTopics = new[] { "Performance", "Reliability", "Edge cases" } },
                        new { moduleNumber = 5, title = $"{topic} Capstone Project", summary = "Bring everything together by designing, building, and reviewing a capstone project.", estimatedHours = 4, keyTopics = new[] { "Project planning", "Implementation", "Review" } }
                    }
                };

                var json = JsonSerializer.Serialize(blueprint, JsonOptions);
                return Task.FromResult($"```json\n{json}\n```");
            }

            if (role == LlmRole.Model3_Builder)
            {
                // Model 3 is called with three different prompts. Each expects a
                // DIFFERENT JSON shape, so sniff the system prompt and emit the
                // matching schema (the old combined {lesson, quiz} object fails
                // validation in the per-call consumers).
                if (systemPrompt.Contains("Assessment Builder", StringComparison.OrdinalIgnoreCase))
                {
                    var quizResult = new
                    {
                        quiz = new
                        {
                            title = $"{topic} Module Mastery Quiz",
                            passingScorePercentage = 70,
                            questions = BuildFallbackQuestions(topic)
                        }
                    };
                    var quizJson = JsonSerializer.Serialize(quizResult, JsonOptions);
                    return Task.FromResult($"```json\n{quizJson}\n```");
                }

                if (systemPrompt.Contains("plan the lesson breakdown", StringComparison.OrdinalIgnoreCase))
                {
                    var plan = new
                    {
                        sections = new[]
                        {
                            $"Foundations of {topic}",
                            $"Applying {topic} in practice",
                            $"{topic}: worked examples and pitfalls"
                        }
                    };
                    var planJson = JsonSerializer.Serialize(plan, JsonOptions);
                    return Task.FromResult($"```json\n{planJson}\n```");
                }

                var lessonResult = new
                {
                    title = $"Deep Dive: Core Mechanics and Applied Practice in {topic}",
                    summary = $"A practical dive into the core mechanics, patterns, and common pitfalls of {topic}.",
                    contentMarkdown =
                        $"# Core Mechanics and Applied Practice in {topic}\n\n" +
                        "## Core idea\n" +
                        $"Every strong {topic} solution starts with clear boundaries: what problem are we solving, what inputs arrive, and what a correct output looks like. " +
                        "Writing those down first turns a vague task into a testable plan.\n\n" +
                        "## Worked example\n" +
                        "Consider a small scenario. Step 1: restate the problem in one sentence. Step 2: list the known inputs and the expected output. " +
                        "Step 3: choose the simplest approach that satisfies the requirements. Step 4: implement it in the smallest possible unit. " +
                        "Step 5: verify the result against the original statement and check edge cases such as empty input, very large input, and invalid input.\n\n" +
                        "Working this way keeps each decision explainable. If something breaks, you can point to the exact step that failed instead of guessing.\n\n" +
                        "## Best practices\n" +
                        "1. Prefer clarity over cleverness; readable solutions are easier to debug and extend.\n" +
                        "2. Validate inputs before doing work so failures happen early and close to the cause.\n" +
                        "3. Write one small check for each important behaviour before moving on.\n\n" +
                        "## Key takeaways\n" +
                        "- State the problem and expected output before implementing.\n" +
                        "- Verify results against edge cases, not just the happy path.\n\n" +
                        "## Practice\n" +
                        "Try the independent exercise below. Write down the known information, choose the concept or method that applies, and check your answer against the edge cases before moving on.",
                    readingTimeMinutes = 14,
                    youTubeVideoId = (string?)null,
                    youTubeVideoTitle = (string?)null,
                    scholarxivCitationDoi = (string?)null,
                    scholarxivPaperTitle = (string?)null,
                    keyTakeaways = new[]
                    {
                        "State the problem and expected output before implementing.",
                        "Verify results against edge cases, not just the happy path."
                    },
                    exercises = new object[]
                    {
                        new
                        {
                            instruction = "Apply the core idea of this module to a small problem and verify it against the edge cases you identified.",
                            hint = "Write down the known information, choose the relevant concept, then check the result.",
                            starterCode = ""
                        }
                    }
                };

                var lessonJson = JsonSerializer.Serialize(lessonResult, JsonOptions);
                return Task.FromResult($"```json\n{lessonJson}\n```");
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

        private static object[] BuildFallbackQuestions(string topic) => new object[]
        {
            new
            {
                prompt = $"What is the first step when approaching a new {topic} problem?",
                options = new[] { "Start coding immediately", "Restate the problem and the expected output", "Search for a finished solution to copy", "Skip planning and fix mistakes later" },
                correctOptionIndex = 1,
                explanation = "Restating the problem and expected output turns a vague task into a testable plan.",
                targetSkillName = $"{topic} Foundations",
                bloomTaxonomyLevel = "Understand"
            },
            new
            {
                prompt = "Why should inputs be validated before doing work?",
                options = new[] { "It makes the code longer", "It prevents failures from happening early", "Failures happen early and close to the cause", "Validation is only needed in production" },
                correctOptionIndex = 2,
                explanation = "Early validation surfaces problems close to their source, which makes them easier to diagnose.",
                targetSkillName = "Defensive Design",
                bloomTaxonomyLevel = "Apply"
            },
            new
            {
                prompt = $"Which of these is a good habit when implementing a {topic} solution?",
                options = new[] { "Prefer clever one-liners", "Write the whole system before testing anything", "Implement the smallest unit and verify it", "Avoid writing down assumptions" },
                correctOptionIndex = 2,
                explanation = "Implementing and verifying small units keeps each decision explainable and testable.",
                targetSkillName = "Implementation Practice",
                bloomTaxonomyLevel = "Apply"
            },
            new
            {
                prompt = "Why is verifying results against edge cases important?",
                options = new[] { "It removes the need for tests", "The happy path is usually wrong", "It catches failures that normal input hides", "It makes the code run faster" },
                correctOptionIndex = 2,
                explanation = "Edge cases (empty, large, or invalid input) often expose defects the happy path never reaches.",
                targetSkillName = "Verification",
                bloomTaxonomyLevel = "Analyze"
            },
            new
            {
                prompt = $"What does clear separation of concerns help you do in a {topic} project?",
                options = new[] { "Reduce the number of files", "Change or test one part without breaking others", "Eliminate all bugs automatically", "Skip documentation entirely" },
                correctOptionIndex = 1,
                explanation = "Clear boundaries let you change and test one part in isolation, which reduces accidental coupling.",
                targetSkillName = $"{topic} Architecture",
                bloomTaxonomyLevel = "Understand"
            }
        };

        /// <summary>
        /// Best-effort topic extraction. Prefers explicit "Module N: ..." or
        /// "Goal: ..." labels in the prompt, then the first meaningful noun.
        /// </summary>
        private static string ExtractTopic(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return "Software Engineering";

            // Intake prompts embed the learner's own words after this marker.
            const string userMessageMarker = "CURRENT USER MESSAGE:";
            var markerIndex = prompt.IndexOf(userMessageMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                var rest = prompt[(markerIndex + userMessageMarker.Length)..].Trim();
                var firstLine = FirstLine(rest);
                if (firstLine.Length > 1) return firstLine;
            }

            // Explicit labelled fields (course-architect and module prompts).
            foreach (var raw in prompt.Split('\n'))
            {
                var trimmed = raw.Trim();
                if (trimmed.Length == 0 || IsLabelOnly(trimmed)) continue;

                if (trimmed.StartsWith("Module ", StringComparison.OrdinalIgnoreCase))
                {
                    var colon = trimmed.IndexOf(':');
                    if (colon >= 0 && colon < trimmed.Length - 1)
                    {
                        var value = trimmed[(colon + 1)..].Trim();
                        if (value.Length > 1) return value;
                    }
                }

                foreach (var label in new[] { "Learning Goal:", "Goal:", "Subject:" })
                {
                    if (trimmed.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                    {
                        var value = trimmed[label.Length..].Trim();
                        if (value.Length > 1) return value;
                    }
                }
            }

            // Otherwise the first meaningful content line.
            foreach (var raw in prompt.Split('\n'))
            {
                var trimmed = raw.Trim();
                if (trimmed.Length > 3 && !IsLabelOnly(trimmed) && !trimmed.StartsWith("---"))
                {
                    return FirstLine(trimmed);
                }
            }

            foreach (var word in prompt.Split(' ', '\n', '\r', '\t'))
            {
                if (word.Length > 3 && char.IsUpper(word[0]))
                {
                    return word.Trim(',', '.', '!', '?', ':', '"');
                }
            }

            return "Software Engineering";
        }

        private static string FirstLine(string text)
        {
            var newline = text.IndexOfAny(new[] { '\r', '\n' });
            if (newline >= 0) text = text[..newline];
            return text.Trim().Trim('"').Trim();
        }

        /// <summary>True for structural labels such as "CONVERSATION HISTORY:" or "--- BEGIN SOURCES ---".</summary>
        private static bool IsLabelOnly(string line)
        {
            if (line.StartsWith("---")) return true;
            if (!line.EndsWith(":")) return false;
            var letters = line.TrimEnd(':').Trim();
            return letters.Length > 0 && letters == letters.ToUpperInvariant();
        }
    }
}
