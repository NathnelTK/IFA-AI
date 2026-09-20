# STARK Hackathon Changelog — IFA

> **Project:** IFA — AI-powered adaptive learning companion
> **Team:** Team XOR — Nathnel Teklemariam (Lead), Ermiyas Eshetu, Negede Tekleyes
> **Hackathon:** STARK Official Hackathon 2026
> **Format:** STARK Changelog Standard v1

This file records every meaningful architectural and product milestone in a
machine-checkable format. Entries are appended in chronological order.

## Entry format

```markdown
## [YYYY-MM-DD] TYPE: Title
- **Author:** Team XOR
- **Phase:** Phase 1 / PR 1.2
- **Type:** ARCHITECTURE
- **Summary:** What changed and why.
- **Files:** `path/to/one.cs`, `path/to/two.svelte`
- **Verification:** How the change was validated.
- **Problem / Solution:** Optional note about a problem encountered and its fix.
```

Allowed types: `FEATURE`, `FIX`, `REFACTOR`, `PERFORMANCE`, `DOCS`, `CHORE`,
`ARCHITECTURE`, `SECURITY`.

Append a milestone with `npm run changelog:add -- --title "..."` and validate the
whole file with `npm run changelog:verify`.

---

## [2026-09-17] CHORE: STARK changelog automation and containerization foundation
- **Author:** Team XOR
- **Phase:** Phase 1 / PR 1.3
- **Type:** CHORE
- **Summary:** Added the hackathon traceability tooling and the local multi-container development topology. `scripts/update-changelog.js` appends and validates STARK-formatted milestones, a root `package.json` exposes `changelog:add` / `changelog:verify`, and `docker-compose.yml` brings up PostgreSQL, the API and the SvelteKit web app for a one-command local environment.
- **Files:** `docs/stark-changelog.md`, `scripts/update-changelog.js`, `package.json`, `docker-compose.yml`, `docker/Dockerfile.backend`, `docker/Dockerfile.frontend`, `.dockerignore`, `.gitignore`
- **Verification:** `npm run changelog:verify` reports "Format is STARK-compliant". Docker is not installed in the author's environment, so `docker-compose.yml` was authored against the documented image/build contract and the frontend/backend builds it depends on were verified locally instead.
- **Problem / Solution:** The repository had no root ignore rules, so `node_modules` and .NET `bin`/`obj` output would have been committed. A root `.gitignore` now excludes dependencies and build output while explicitly keeping source `.ts`/`.js`/`.svelte`/`.cs` files tracked.

## [2026-09-17] ARCHITECTURE: ASP.NET Core Clean Architecture backend foundation
- **Author:** Team XOR
- **Phase:** Phase 1 / PR 1.2
- **Type:** ARCHITECTURE
- **Summary:** Completed the .NET 8 Clean Architecture backend. Domain entities stay dependency-free, the Application layer owns the persistence and integration contracts, and Infrastructure implements them with EF Core + PostgreSQL. The API now exposes Swagger, a SvelteKit-scoped CORS policy, a `/api/health` endpoint and dependency-injection wiring instead of the default weather template.
- **Files:** `IFA.slnx`, `src/IFA.Infrastructure/Data/ApplicationDbContext.cs`, `src/IFA.Infrastructure/Data/Configurations/*.cs`, `src/IFA.Infrastructure/DependencyInjection.cs`, `src/IFA.Infrastructure/Data/Migrations/*`, `src/IFA.API/Program.cs`, `src/IFA.API/appsettings.json`
- **Verification:** `dotnet build IFA.slnx` succeeds with 0 warnings and 0 errors; `dotnet ef dbcontext info` resolves the Npgsql provider and model; the `InitialCreate` migration generates the full schema.
- **Problem / Solution:** `List<string>` answer options needed an explicit PostgreSQL column type. They are mapped to `text[]` so the model validates cleanly under Npgsql.

## [2026-09-17] FEATURE: SvelteKit 2 frontend shell and IFA design system
- **Author:** Team XOR
- **Phase:** Phase 1 / PR 1.1
- **Type:** FEATURE
- **Summary:** Established the SvelteKit 2 + Svelte 5 client with Tailwind design tokens matching the IFA dashboard mockup (`ifa.png`): a fixed sidebar, header, responsive dashboard grid, and the initial dashboard components for the hero scoper, continue-learning card, public courses, skill donut, recommendations, activity feed and AI tutor dock.
- **Files:** `client/package.json`, `client/tailwind.config.js`, `client/svelte.config.js`, `client/src/routes/+layout.svelte`, `client/src/routes/+page.svelte`, `client/src/lib/components/*`, `client/src/lib/stores/dashboardStore.ts`
- **Verification:** `npm run check` (svelte-check) reports 0 errors and 0 warnings.
- **Problem / Solution:** The container build requires a Node adapter, so `@sveltejs/adapter-node` replaced `adapter-auto` to produce a runnable `build/` output for Docker.

## [2026-09-20] ARCHITECTURE: Three-Model AI Pipeline & Adaptive Engine Foundation
- **Author:** Team XOR
- **Phase:** Phase 2 & 3
- **Type:** ARCHITECTURE
- **Summary:** Completed the AI Foundation and Three-Model course generation pipeline with JIT materialization and Adaptive Learning Engine. Intentionally stubbed integration clients with detailed implementation tasks for the team.
- **Verification:** dotnet build IFA.slnx succeeds with 0 errors and 0 warnings; npm run check succeeds with 0 errors and 0 warnings.
