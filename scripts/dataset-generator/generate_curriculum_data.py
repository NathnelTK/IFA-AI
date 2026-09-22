#!/usr/bin/env python3
"""
=============================================================================
ASSIGNED TO AI & ML TEAM (Ermiyas / Team XOR AI Lead)
=============================================================================
RESPONSIBILITY:
Synthesize training pairs (Input: ModuleSpecificationDto -> Output: IFA Course JSON)
for fine-tuning Gemini Flash in Google AI Studio.

STEP-BY-STEP INSTRUCTIONS:
1. Define 10-15 standard tech topics (C#, Python, SQL, REST APIs, Docker, Clean Arch).
2. For each topic, construct:
   - Input: module specification containing topic, audience, learning objectives,
     academic evidence summaries, and creator preferences.
   - Output: structured JSON matching IFA's Course schema (Lesson title, markdown,
     exercises, key takeaways, and 3-question diagnostic quiz).
3. Export into JSONL format compatible with Google AI Studio:
   {"messages": [{"role": "user", "content": "..."}, {"role": "model", "content": "..."}]}
4. Output the generated file to data/ifa_curriculum_tuning.jsonl.
=============================================================================
"""

import json
import argparse
import os

SEED_TOPICS = [
    {
        "topic": "ASP.NET Core Minimal APIs",
        "audience": "Junior Backend Developer",
        "creator": "Nick Chapsas",
        "lesson_title": "Getting Started with Minimal APIs in .NET 10",
        "quiz_prompt": "What is the primary benefit of Minimal APIs over Controller-based APIs?",
        "quiz_correct": "Minimal overhead, faster startup and streamlined syntax without boilerplate."
    },
    {
        "topic": "Clean Architecture Boundaries",
        "audience": "Software Engineering Student",
        "creator": "Amichai Mantinband",
        "lesson_title": "Separation of Concerns: Domain, Application, and Infrastructure",
        "quiz_prompt": "Why should the Domain layer have no reference to the Infrastructure layer?",
        "quiz_correct": "To maintain persistence ignorance and protect core business rules from external churn."
    }
]

def generate_sample(item):
    user_input = {
        "module_number": 1,
        "topic": item["topic"],
        "target_audience": item["audience"],
        "preferred_creators": [item["creator"]],
        "learning_objectives": [f"Understand {item['topic']} principles", f"Implement working code for {item['topic']}"]
    }
    
    model_output = {
        "module_title": f"Mastering {item['topic']}",
        "lessons": [
            {
                "lesson_number": 1,
                "title": item["lesson_title"],
                "reading_time_minutes": 10,
                "content_markdown": f"# {item['lesson_title']}\n\nIn this lesson, we break down core principles...",
                "practical_exercises": [f"Create a project demonstrating {item['topic']}"],
                "key_takeaways": [f"{item['topic']} is essential for scalable backend design"]
            }
        ],
        "quiz": {
            "title": f"{item['topic']} Diagnostic Quiz",
            "passing_score_percentage": 70,
            "questions": [
                {
                    "prompt": item["quiz_prompt"],
                    "options": [
                        item["quiz_correct"],
                        "It eliminates the need for database storage completely.",
                        "It only works with JavaScript frameworks.",
                        "It increases build times significantly."
                    ],
                    "correct_option_index": 0,
                    "explanation": item["quiz_correct"]
                }
            ]
        }
    }

    return {
        "messages": [
            {"role": "user", "content": json.dumps(user_input, indent=2)},
            {"role": "model", "content": json.dumps(model_output, indent=2)}
        ]
    }

def main():
    parser = argparse.ArgumentParser(description="Generate curriculum fine-tuning dataset for IFA")
    parser.add_argument("--output", default="data/ifa_curriculum_tuning.jsonl", help="Output JSONL path")
    parser.add_argument("--samples", type=int, default=10, help="Number of samples to generate")
    args = parser.parse_args()

    os.makedirs(os.path.dirname(args.output), exist_ok=True)

    with open(args.output, "w", encoding="utf-8") as f:
        for idx in range(args.samples):
            seed = SEED_TOPICS[idx % len(SEED_TOPICS)]
            sample = generate_sample(seed)
            f.write(json.dumps(sample) + "\n")

    print(f"Successfully generated {args.samples} fine-tuning samples to {args.output}")

if __name__ == "__main__":
    main()
