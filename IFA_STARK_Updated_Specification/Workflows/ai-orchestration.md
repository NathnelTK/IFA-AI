# AI Orchestration

The orchestrator selects a workflow and supplies the appropriate context to Ollama.

```text
Frontend
   ↓
.NET API
   ↓
AI Orchestrator
   ├── onboarding prompt
   ├── course planner prompt
   ├── content generation prompt
   ├── quiz prompt
   ├── tutor prompt
   └── profile-update prompt
          ↓
       Ollama
          ↓
 Structured JSON / response
          ↓
 Application validation
```

Model configuration belongs in environment/configuration rather than controllers.

The orchestrator should keep prompts versioned and testable.
