# System Architecture

```text
SvelteKit Frontend
        |
        v
     .NET API
        |
  +-----+----------------------+
  |     |          |           |
  v     v          v           v
Auth  Learning   AI Orchestrator  Research/Integrations
        |              |             |
        v              v             +--> ScholarXiv MCP
    PostgreSQL      Ollama            +--> Web/Video Search
                       |
                       +--> Quiz / Tutor / Profile workflows

Voxide
   |
   v
Voice interaction → .NET API → AI Orchestrator
```

The frontend UI is already implemented for the planned product areas. Backend work should provide the real data and behavior behind those screens.
