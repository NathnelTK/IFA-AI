# Non-Functional Requirements

- API contracts should be stable and versionable.
- External AI/tool failures must be handled gracefully.
- Secrets must never be committed.
- Learner data must be protected by authorization.
- Long AI operations should not block requests indefinitely.
- The system should support replacing Ollama with another provider later without rewriting domain logic.
- Backend code should remain consistent with the Onion Architecture.
