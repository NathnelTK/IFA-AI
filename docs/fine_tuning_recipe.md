# IFA Fine-Tuning Recipe: Model 3 (IFA Course Builder)

> **Hackathon**: STARK Official Hackathon 2026  
> **Component**: Model 3 — Fine-Tuned Course Builder  
> **Platform**: Google AI Studio Tuned Models (Gemini 1.5 Flash / 2.0 Flash)

---

## 1. Role & Objective

Model 3 is the **specialized course constructor**.
- It does **not** conduct broad web research or conversational profiling (that is Model 1's role).
- It does **not** design the full multi-module curriculum (that is Model 2's role).
- Its sole purpose is to convert a **materialized Module Specification** into a rich, structured, learner-ready IFA module containing:
  - Engaging explanations with real-world analogies
  - Practical code snippets
  - Embedded YouTube video metadata
  - Scholarxiv citations
  - Formative diagnostic quizzes with distractor explanations

---

## 2. Training Dataset Specification (JSONL)

Each training example is a JSON object with `messages` or `input_text` / `output_text`:

```json
{
  "system_instruction": "You are the IFA Course Builder. Convert the provided Module Specification into standardized IFA Course JSON adhering to the IFA Course Design System.",
  "input": {
    "module_number": 1,
    "topic": "REST APIs with ASP.NET Core",
    "target_audience": "Junior Software Engineer",
    "learner_profile": {
      "level": "Beginner",
      "preferred_creators": ["Nick Chapsas"]
    },
    "learning_objectives": ["Understand HTTP methods", "Build minimal API"],
    "research_findings": ["Microservice architectures favor decoupled REST boundaries"],
    "resources": [],
    "preferred_videos": []
  },
  "output": {
    "module_title": "C# 10 & RESTful API Foundations",
    "lessons": [
      {
        "lesson_number": 1,
        "title": "HTTP Request Lifecycle & Minimal APIs",
        "reading_time_minutes": 10,
        "content_markdown": "# HTTP Request Lifecycle\n\nWhen a client issues a request...",
        "practical_exercises": ["Implement app.MapGet('/health')"],
        "key_takeaways": ["GET requests must be idempotent", "Status 200 signals success"]
      }
    ],
    "quiz": {
      "title": "REST API Diagnostic",
      "passing_score_percentage": 70,
      "questions": [
        {
          "prompt": "Which HTTP verb should be used to update an entire resource idempotently?",
          "options": ["GET", "POST", "PUT", "DELETE"],
          "correct_option_index": 2,
          "explanation": "PUT replaces the resource state and is idempotent according to RFC 7231."
        }
      ]
    }
  }
}
```

---

## 3. Dataset Generation Script

Run the generator script to synthesize 50-100 high-quality training pairs:

```bash
python scripts/dataset-generator/generate_curriculum_data.py --output data/ifa_curriculum_tuning.jsonl --samples 50
```

---

## 4. Google AI Studio Fine-Tuning Steps

1. Navigate to [Google AI Studio](https://aistudio.google.com/).
2. Select **Tune Model** -> Model: `gemini-1.5-flash-001` (or latest available free tuning base).
3. Upload `data/ifa_curriculum_tuning.jsonl`.
4. Hyperparameters:
   - **Epochs**: 4
   - **Batch Size**: 4
   - **Learning Rate Multiplier**: 1.0
5. Once tuning completes, copy the tuned model resource name:
   `tunedModels/ifa-course-builder-xxx`
6. Set in `.env`:
   ```bash
   AI_BUILDER_MODEL_NAME=tunedModels/ifa-course-builder-xxx
   ```

---

## 5. Resilience & Fallback Plan

If the tuned model endpoint is warming up or unavailable:
The `FineTunedCourseBuilderClient.cs` automatically activates few-shot prompted Gemini Flash with strict JSON schema validation, ensuring zero downtime during judges' evaluations.
