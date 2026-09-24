# Quiz Workflow

Quizzes are generated from the current course/module content.

The same Ollama model can be used with a quiz-specific structured prompt.

```text
Module content
 ↓
Quiz generation
 ↓
Questions + options + answer key + explanations
 ↓
Backend validation
 ↓
Persist quiz
 ↓
Learner attempt
 ↓
Score + skill signals
```

Quiz generation is an AI workflow, not a requirement for a separate quiz model.
