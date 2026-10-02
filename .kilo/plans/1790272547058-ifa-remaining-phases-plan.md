# IFA — Plan to Finish Phases 2–8 (Full Spec)

## Goal
Complete the IFA STARK spec's remaining critical path for real: turn the mock UI + static backend into a working end-to-end AI adaptive learning loop, then finish social, account, search/notifications, and deployment.

Target flow:
```
Learner → Model 1 intake → LearnerProfile → Research → Model 2 blueprint
→ JIT current module → Model 3 lesson/quiz → Assessment → Skill gap → Adaptive next module
```

## Locked decisions
1. **Scope:** Full spec, Phases 2–8 (auth, notifications, search, publishing, CI/deploy included).
2. **AI runtime:** `ILlmGateway` abstraction. Ollama is the default provider; hosted Gemini/Groq is an optional, config-selected fallback. Role-based prompts on one model; **no fine-tuning** (ADR-03/10). Update README to stop advertising Gemini/Groq as primary.
3. **Auth:** Lightweight JWT on the existing `Learner` entity (add `PasswordHash`, `Role`, refresh tokens). Controllers derive `learnerId` from the token instead of URL params.
4. **External integrations:** Real adapters behind existing `IScholarxivService`, `IYouTubeResourceService`, `IVoxService`; config-driven deterministic offline fallback so the demo never hard-fails.

## Verified current state (evidence)
- **Frontend:** all UI pages/components exist, but **zero** API calls (`grep` for `fetch(`/`/api/`/`API_URL` in `client/src` = none). All data is hardcoded in `client/src/lib/stores/dashboardStore.ts`.
- **Backend services are mock:** `ScholarxivService` returns seeded papers; `SkillProfileService` returns static lists; `CourseSharingService` does not persist (its `Add`/`SaveChangesAsync` are commented out).
- **Only 2 controllers:** `SkillsController`, `CourseSharingController`. No AI/LLM implementation exists (`grep` for Ollama/Gemini/Groq/HttpClient/ILlm = none). `IYouTubeResourceService`, `IVoxService`, `ICourseGenerationService` are declared but unimplemented.
- **Data layer is correct:** every relationship IS configured in `src/IFA.Infrastructure/Data/Configurations/*.cs` and present in the `InitialCreate` migration/snapshot.
- **Teammate's report is a misdiagnosis.** `Learner` having only navigation properties is correct EF modeling — FKs live on dependents (`SkillMetric.LearnerId`, `CourseEnrollment.LearnerId`, `Course.CreatorLearnerId`, `CourseShareInvite.SenderLearnerId`); Learner→Course is many-to-many via `CourseEnrollment`, so no direct FK should exist, and nothing is "unconfigured." The **real gap** is that `Learner` has no profile/learning state and there is no learner-profile contract, API, or persistence (i.e. PR 2.1 was never done).

## Architecture additions
- **LLM layer:** `IFA.Application/Common/Interfaces/ILlmGateway.cs`; `IFA.Infrastructure/AI/OllamaLlmProvider.cs`, `HostedLlmProvider.cs` (Gemini/Groq), `LlmGateway.cs` (provider select + fallback), `PromptRegistry.cs` (versioned Model 1/2/3 + tutor prompts). Structured JSON output validated against schemas; retry/repair on invalid JSON.
- **Auth layer:** `IFA.Application/Common/Interfaces/ITokenService.cs`; `IFA.API` JWT bearer config; `PasswordHash`/`Role` on `Learner`; refresh-token entity.
- **Research layer:** `ResearchPackage`/`ResearchSource` contracts + persistence; real ScholarXiv MCP client + YouTube Data API adapter with fallback.
- **New controllers:** `AuthController`, `LearnerProfileController`, `DashboardController`, `CoursesController`, `EnrollmentController`, `ModulesController`, `LessonsController`, `QuizzesController`, `AiController`, `ResearchController`, `ActivityController`, `NotificationsController`, `RecommendationsController`, `SearchController`, `VoiceController`.

## Data model additions (additive migrations only)
- `Learner`: add `PasswordHash`, `Role`, `RefreshToken` support.
- `LearnerProfile` (1:1 Learner): LearningGoal, Subject, CurrentLevel, TargetOutcome, WeeklyStudyHours, PreferredLanguage, LearningStyle, Constraints, PreferredYouTubeChannels, KnownStrengths, KnownWeaknesses, Requirements, UpdatedAt.
- `LearnerState` (1:1 Learner): current course/module, overall progress, updatedAt.
- `Course` blueprint: store Model 2 blueprint fields; materialize `Module` rows with `IsGenerated=false` (existing `Module.IsGenerated`/`GeneratedAt` already support JIT). Model 3 fills `Lesson`/`Quiz`/`Question` and flips `IsGenerated=true`.
- `ResearchPackage` + `ResearchSource` (academic/practical/video, metadata, relevance).
- `Assessment` (quiz attempt: Score, Passed, AttemptNumber, timestamps) + `AssessmentResponse` (question-level result).
- `LearningActivity` (event type, description, metadata JSON, timestamp).
- `Notification` (type, title, body, IsRead).
- `RefreshToken`.

Cascade behavior already set: Course→Modules/Lessons/Quizzes cascades; Learner→Enrollments/Skills cascades; CourseShareInvite→SenderLearner is `Restrict`. New FKs follow the same pattern.

## Ordered task list
Implement in order; each item = one PR branch per spec §0.1/§13.

### Phase 2 — AI & Research foundation
1. Data model + migration for `LearnerProfile`, `LearnerState`, auth fields, `RefreshToken`.
2. `ILlmGateway` + Ollama provider + hosted fallback + `PromptRegistry` versioning; config in `appsettings.*` + `.env.example`.
3. Learner Profile contract/service/API (`GET/PUT /api/learners/me/profile`), validation, example profile for tests.
4. Research Package contract + persistence (`ResearchPackage`/`ResearchSource`).
5. ScholarXiv adapter: real MCP/HTTP client behind `IScholarxivService`, normalize results, handle timeout, keep seeded fallback.
6. YouTube adapter behind `IYouTubeResourceService` (goal/topic-linked, preferred channels, dedupe) + fallback.
7. Model 1 intake: conversation service + `POST /api/ai/intake/message`; produces profile (validated/persisted per ADR-06), triggers research, asks confirm before course creation.

### Phase 3 — Three-model pipeline
8. Model 2 course architect: blueprint schema + service + `POST /api/ai/course/propose`; persist Course + lightweight Modules (no lesson generation).
9. JIT/lazy generation boundary: select current module, pass only its spec to Model 3, persist generated content.
10. Model 3 course builder: lessons, exercises, formative quiz, module assessment, takeaways; `POST /api/ai/module/generate`.
11. End-to-end orchestrator service wiring Model 1 → research → Model 2 → JIT → Model 3 without manual intervention.

### Phase 4 — Learning experience (wire UI to real API)
12. Frontend API client layer (`client/src/lib/api/*.ts`) + typed contracts mirroring `client/src/lib/types/index.ts`; auth store + `VITE_API_URL`.
13. Login/register UI + protected routes + 401/refresh handling.
14. Lesson viewer + quiz interface (PR 4.4 — currently missing): course overview, module list, lesson content/resources, exercises, quiz, assessment, completion.
15. Replace `dashboardStore.ts` mocks with API calls: dashboard, my-learning, courses, my-skills, research, progress, settings, marketplace, activity, recommendations, notifications, search, profile menu.
16. AI Tutor dock + Voice overlay wired to `POST /api/voice/intent` / speech and tutor chat endpoint (context-aware).

### Phase 5 — Adaptive learning
17. Assessment tracking: `POST /api/quizzes/{id}/submit`, persist `Assessment`/`AssessmentResponse`, completion state.
18. Skill gap detection: update `SkillMetric` from assessments (mastered/weak/incomplete/reinforcement).
19. Adaptive engine (application logic, **not a model**): compute next-module constraints.
20. Adaptive course continuation: feed updated learner state into Model 2 → JIT → Model 3; must demonstrably change the next module.

### Phase 6 — Social & public
21. Fix `CourseSharingService` to actually persist `CourseShareInvite` and enroll via share code.
22. Enrollment + progress tracking APIs (dashboard/my-learning data source).
23. Course publishing (private/published/draft) + public/marketplace APIs + course detail.
24. Activity event emission on enroll/complete/assess/research/share/publish.
25. Peer comparison endpoints (privacy-bounded fields only).
26. Recommendation engine: rules over profile + skills + progress + research + public courses.

### Phase 7 — Account, search, platform polish
27. Complete auth: register/login/logout/refresh, roles/access checks, protected routes.
28. Profile + settings persistence APIs (learning, resources, notifications, theme).
29. Search backend (start with PostgreSQL full-text/`ILIKE`; defer pgvector unless needed) + command palette wiring.
30. Notification backend + triggers (course ready, assessment result, recommendation, shared course).
31. Polish: loading/error states, transitions, accessibility basics.

### Phase 8 — Integration, deployment, QA
32. Docker: add Ollama service, env vars (DB, LLM provider/keys, research/voice), networking, startup order; verify all services end-to-end.
33. CI: backend build, tests, frontend `npm run check`/build.
34. EthioDeploy: deploy, verify frontend↔API↔DB↔AI, CORS/auth, logging.
35. Idempotent demo seed data (learner, course, modules, assessment, skills, public course, research, activity) — no manual DB edits during demo.
36. Final QA: run the Phase 8.6 end-to-end story plus refresh, logout/login, empty states, API/AI failures, responsive, deployed env.
37. STARK changelog entries per merged PR; update `README.md` and spec status docs to match reality.

## Failure modes to handle
- Ollama unreachable → hosted fallback → deterministic seeded content; pipeline must not hard-fail.
- Malformed LLM JSON → schema validation, single retry/repair, then fallback content.
- ScholarXiv/YouTube/Voxide failure/timeout → adapter returns fallback + logs; no raw tool output passed downstream.
- Auth: token expiry/refresh, 401 handling in client, role checks.
- Migrations: additive only; never destructive on the existing DB.

## Validation
- Backend: `dotnet build IFA.slnx` (0 warnings/0 errors); `dotnet ef migrations add <Name>` + `dotnet ef database update`; run API and exercise each new endpoint via Swagger.
- Pipeline: create learner → intake → confirm profile persisted → research package stored → course + modules created (ungenerated) → generate current module → lesson/quiz rows exist → submit quiz → skill metrics change → next module reflects adaptation.
- Frontend: `npm run check` (0 errors/0 warnings); `npm run build`.
- Docs/changelog: `npm run changelog:verify` passes; each PR has a changelog entry.
- Docker: `docker-compose up` brings up DB + API + frontend + Ollama; health + one AI call succeeds.

## Risks / notes
- README vs `decisions.md` conflict must be resolved in docs (README is wrong under the locked decision).
- External-provider and EthioDeploy availability are environmental; adapters + fallback mitigate.
- Structured output reliability depends on the chosen local model; keep prompts/versioning centralized.
- Keep AI role boundaries per spec (Model 1 interviews, Model 2 designs, Model 3 builds content; adaptive engine is app logic).

## Open questions (non-blocking; resolve at implementation)
1. Exact default Ollama model (config value, e.g. `llama3.1:8b` or `qwen2.5:7b`).
2. Hosted provider keys/accounts for the fallback path.
3. EthioDeploy target + Ollama hosting for the public demo.
4. Whether pgvector is needed for search/recommendation quality (start without it).
