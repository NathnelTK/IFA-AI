# Architecture Decisions

## ADR-01 — Frontend status
The UI for the planned product phases is already implemented. Backend work should integrate with it instead of rebuilding it.

## ADR-02 — AI runtime
Use Ollama for the hackathon.

## ADR-03 — Model strategy
Use one free Ollama-compatible model. Do not spend hackathon time on fine-tuning.

## ADR-04 — AI roles
Use role-specific prompts/workflows around the same model for onboarding, course planning, content generation, quizzes, tutor interactions and profile interpretation.

## ADR-05 — Learner state
The backend/database owns persistent learner profile, progress, assessment and skill state.

## ADR-06 — AI profile updates
AI can propose structured profile updates, but the Application/API layer validates and persists them.

## ADR-07 — API development
Endpoints are implemented with their feature rather than all being created upfront. Contract changes go through PR review.

## ADR-08 — Research
ScholarXiv MCP is part of the research workflow and its output is normalized before downstream AI use.

## ADR-09 — Voice
Voxide is used for application voice interaction rather than only recording voice messages.

## ADR-10 — No fine-tuning
Fine-tuning is intentionally deferred because the hackathon timeline is better spent on the working learning loop.
