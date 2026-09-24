# Deployment

Deploy the integrated system early enough to expose configuration problems.

Verify:

- frontend
- .NET API
- PostgreSQL/Supabase
- authentication
- Ollama strategy for the deployed environment
- ScholarXiv integration
- Voxide integration
- CORS
- environment variables
- error handling

Important: a local Ollama setup may not be suitable for the final hosted environment. The architecture should therefore keep the AI provider behind an application interface so the runtime can be changed without rewriting the application.
