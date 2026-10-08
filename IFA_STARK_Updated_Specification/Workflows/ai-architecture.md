# AI Architecture

## Decision

IFA will use **Ollama** as the local AI runtime and **one free Ollama-compatible model** for the hackathon.

No fine-tuning is planned.

No separate trained model is required for onboarding, course generation, quizzes, tutor conversations or profile interpretation.

Instead, the same model is used through different controlled prompts, structured output schemas and application workflows.

## AI responsibilities

### Learner interaction
Understand goals, level, constraints and preferences.

### Course planning
Create a course blueprint from learner state and research.

### Content generation
Generate the current lesson/module from an approved module specification.

### Quiz generation
Generate questions and assessments from the current learning content.

### Tutor
Answer questions using current course/module context and learner state.

### Profile updates
Interpret explicit learner statements and produce a structured proposed profile update.

## Backend boundary

```text
User
 ↓
AI workflow
 ↓
Structured output
 ↓
Application validation
 ↓
API/domain rules
 ↓
PostgreSQL
```

The model never receives direct database credentials and never writes directly to the database.
