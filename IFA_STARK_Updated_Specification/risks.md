# Risks

## Ollama deployment
A local model may be difficult to host in the final deployment environment.

Mitigation: keep AI behind an application interface so another provider can be substituted if necessary.

## AI response reliability
Free local models may return malformed structured output.

Mitigation: schemas, validation, retries and constrained prompts.

## External tool availability
ScholarXiv or Voxide may have integration limitations.

Mitigation: isolate integrations and provide graceful failure handling.

## Time
Trying to implement too many independent AI models or fine-tune a model may consume the hackathon timeline.

Mitigation: one model, multiple workflows, focus on the core learner loop.
