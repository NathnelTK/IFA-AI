# Business Rules

1. The backend is the source of truth for learner state.
2. AI output cannot directly modify database records.
3. Profile changes initiated through AI must be represented as structured API requests and validated before saving.
4. Course generation must use the learner profile and research package.
5. Future modules should remain lightweight until needed where practical.
6. Assessment results must be persisted before they affect adaptive learning state.
7. Public course visibility requires an explicit publish state.
8. Private learner information must not be exposed through public progress comparison.
9. External service failures must not corrupt learner state.
10. API contracts should be changed through reviewed PRs.
