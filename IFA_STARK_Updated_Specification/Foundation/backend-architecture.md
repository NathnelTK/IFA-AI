# Backend Architecture

Use Onion Architecture with clear separation between:

- Domain
- Application
- Infrastructure
- API
- Shared

The API layer exposes HTTP contracts. Application services/use cases contain orchestration and business workflows. Infrastructure handles PostgreSQL, Ollama, ScholarXiv, Voxide and other external services.

AI services must not directly write to PostgreSQL. AI outputs are validated by the application/API layer before persistent changes are made.
