# Witforge Upgrade: Audit and Delivery

## Audit (2026-10-29)

The working tree already contained uncommitted Witforge changes. This implementation extends those changes; it does not replace the application or discard existing work.

### Preserve

- SvelteKit 2, Svelte 5, Tailwind theme tokens, responsive application shell, and light/dark modes.
- .NET 10 API, Application/Domain/Infrastructure boundaries, EF Core, PostgreSQL/Supabase, JWT authentication, and existing learner identities.
- Course -> Module -> Lesson/Quiz, CourseEnrollment, LessonProgress, QuizAttempt/QuizAnswer, and assessment/skill records.
- EducatorProject -> sources/conflicts/blueprint/generation jobs, the shared studio CourseEditor, and seven reusable template definitions.
- Gemini/Groq/Ollama gateway, research integrations, notifications, external meeting links, and the separate Chapa payment integration.

### Implemented or Partial Before This Pass

- Manual and template creation already save Course and EducatorProject records. Curriculum add/edit/delete, branding, assignments, discussions, and live-session forms exist.
- Source-based AI approval already creates a persisted course and generation jobs. Topic-proposal approval discards the proposed modules; conversation creation only fills a brief.
- Marketplace, learner course player, enrollment approval, lessons, quizzes, and analytics pages exist, but several UI/API contracts are incomplete.
- The template catalog is real server-side application data, not merely decorative cards. Assessment suggestions and completion rules are not fully materialized.

### Critical Gaps Verified in Code

- Free/private courses and raw quiz answer keys are returned by the course-detail endpoint without appropriate access checks.
- Several learning endpoints use a shared demo identity when no authentication is present. Module generation, progress, assessments, discussions, and assignment operations need consistent course authorization.
- QuizView grades in browser memory instead of submitting to the backend. Lesson completion is optimistic, failures are swallowed, and completion state is not restored reliably.
- The lesson-completion backend counts progress before saving and repeated completion inflates activity totals. Missing quiz answers can be interpreted as answer zero.
- Publishing does not validate curriculum and lacks an explicit draft/publication distinction. Module reordering, direct assessment authoring, and reliable save/preview feedback are incomplete.
- New course settings and learning entities have no migration. The startup DDL uses CourseDiscussions while EF conventions use Discussions; migration-history stamping is hardcoded.
- The AI gateway silently substitutes deterministic invented content when providers fail. Blueprint JSON casing and review navigation are inconsistent.
- Source URL ingestion lacks private-network/redirect/size safeguards. There is no verified private file-upload storage pipeline.
- Some optional tabs repeatedly fetch empty datasets. Some analytics and marketplace defaults look like real data but are synthetic or attributed to the wrong course.

## Prioritized Implementation Plan

1. Establish additive schema changes and explicit publication/access settings. Reuse existing entities; validate against isolated PostgreSQL, never the configured Supabase database.
2. Finish the shared educator workflow: manual/template drafts, editable course details and curriculum, stable ordering, quiz authoring, safe saves, actual preview, publish/unpublish validation, and ownership enforcement.
3. Preserve reviewed AI blueprints in the same editor and fail clearly when generation is unavailable. Keep source/research claims honest and preserve manual creation as a first-class path.
4. Complete authorized enrollment, lesson delivery, server-graded assessments, reload-safe progress, and course-scoped educator metrics.
5. Harden existing supporting endpoints and empty states where they intersect this workflow. Run frontend checks/build, backend build, real HTTP permission/persistence tests, and PostgreSQL migration checks.

## Deferred Scope and Dependencies

- Native conferencing, attendance integrations, cohort administration, private binary file uploads/downloads, slide extraction, OCR, and malware scanning require additional infrastructure. The implemented first version stores validated external meeting links and performs bounded extraction only for supported public PDF/text/web sources.
- Certificates, advanced lesson prerequisites/drip scheduling, payment-provider activation, custom educator websites, and comprehensive moderation are not to be represented as complete by this vertical slice.
- Full published-version snapshots and advanced autosave/collaborative editing are larger changes. An explicit unpublish/edit/republish workflow is the safe initial publishing boundary.
- AI and external research require reachable configured providers. No fallback may pretend that invented material or citations are verified AI research.

## Baseline Verification

- `dotnet build IFA.slnx --no-restore`: passed, zero warnings/errors.
- `npm run check` in `client`: zero errors, 25 warnings across seven files.
- Local .NET SDK 10.0.303, EF CLI 10.0.12, Node 24.21.0, and PostgreSQL 18 are available. Docker is not installed/on PATH.
- A disposable PostgreSQL cluster is used for validation, bound to loopback on a separate port. Existing configured application databases are not modified.

## Implemented Vertical Slice

### Shared Data and Publishing

- Added explicit publication and visibility fields, learning objectives, language, level, completion rules, and bounded enrollment settings to `Course` without removing legacy fields.
- Added EF configurations for live sessions, assignments/submissions, and discussions, including the intended `CourseDiscussions` table mapping.
- Added migration `20261029110932_AddWitforgeCourseWorkflow`. It preserves legacy listed courses as public; ambiguous legacy non-public courses remain drafts rather than being exposed accidentally.
- Reconciled databases previously provisioned by the manual startup DDL before stamping the new migration. The normal migration path remains the default.
- Separated `IsPublished` from `Visibility`. Public marketplace listing mirrors `IsPublished && Visibility == Public`; unlisted links remain accessible without appearing in search, and private access requires a valid invite/enrollment.

### Educator Workflow

- Manual, template, reviewed AI-topic, conversation-brief, and source-blueprint flows now converge on a persisted `Course` plus `EducatorProject` and the same Studio editor.
- The seven templates now carry reusable curricula, objectives, assessment suggestions, completion rules, and appropriate starter assignments.
- Added metadata/access editing, module and lesson ordering/duplication, Markdown lessons, video IDs, MCQ/answer/explanation authoring, saved preview, unsaved-change protection, and actionable publication validation.
- Published content cannot be edited until the educator explicitly unpublishes it. This prevents learner-visible drafts without introducing an unsafe pseudo-versioning system.
- Added persisted external-link live sessions with time zone, edit/cancel states, and learner notifications. No native conferencing or attendance is claimed.
- Added text-only assignments, deadlines, submissions, instructor grading/feedback, resubmission handling, and real grading metrics. File fields are rejected until private upload storage exists.

### Learner Delivery and Access

- Replaced implicit demo identities with JWT-derived identities and role/ownership checks. Legacy sharing no longer accepts caller-supplied learner IDs.
- Course reads now return explicit owner/access/enrollment state. Anonymous visitors receive public metadata and curriculum titles only; private lesson content and all answer keys are redacted.
- Added controlled open, approval, invite, paid-pending, capacity, and date-window enrollment transitions. Paid publication is rejected if Chapa is not configured.
- Lesson completion is idempotent and persisted. Quiz answers are validated and graded on the server, attempt limits are enforced, feedback is revealed only after submission, and course completion is recalculated from saved lesson/quiz evidence.
- The learner UI now awaits writes, restores completion and attempts after reload, distinguishes preview/pending/payment/active states, and keeps educator preview read-only.

### Data Integrity, AI, and Product Trust

- Educator analytics now use course-scoped enrollments, lesson completions, assessments, assignments, and live sessions with explicit metric denominators.
- Recommendations now require persisted weak-skill evidence and only link matching published course metadata; invented confidence values and generic trending claims were removed.
- Notifications now load and update real API records instead of hardcoded examples. Cross-account course/chat caches are identity-scoped and cleared on sign-out.
- Removed silent deterministic AI content fallback. Unavailable providers return `503 ai_unavailable`; configured proposals remain editable before persistence.
- Hosted cross-provider failover is opt-in to avoid silently forwarding source content to another vendor. Manual/templates and saved editing continue without AI.
- Added public-address DNS checks, redirect/proxy blocking, streamed byte limits, MIME restrictions, and honest unsupported-state handling for source ingestion.
- New passwords use versioned PBKDF2-SHA256 with per-password salts. Existing SHA-256 rows remain sign-in compatible and are upgraded after a successful login.
- Demo data and demo-login endpoints are development-only unless `DEMO_DATA_ENABLED=true` is explicitly configured.

## Principal Files and Models

- Domain and schema: `Course.cs`, `CourseLearningConfiguration.cs`, `CourseConfiguration.cs`, `ApplicationDbContext.cs`, and `20261029110932_AddWitforgeCourseWorkflow*`.
- Educator APIs: `EducatorController.cs`, `EducatorController.Curriculum.cs`, `CourseTemplateCatalog.cs`, and `CoursePublication.cs`.
- Learner APIs: `CoursesController.cs`, `LessonsController.cs`, `QuizzesController.cs`, `CourseAccess.cs`, and `CourseProgress.cs`.
- Supporting APIs: `PaymentsController.cs`, `CourseSharingController.cs`, `AuthController.cs`, `RecommendationsController.cs`, and `ResearchController.cs`.
- AI/source services: `LlmGateway.cs`, provider implementations, `SourceIngestionService.cs`, `SourceAnalysisService.cs`, `BlueprintService.cs`, and `ModuleGenerationBackgroundService.cs`.
- Frontend: `studio/create`, `studio/[id]`, `CourseEditor.svelte`, `QuizView.svelte`, learner course/marketplace/invite routes, API types/services, course/session stores, notifications, recommendations, and research views.
- Repeatable smoke test: `scripts/test-witforge-workflow.ps1`.

## Remaining Scope

- Certificates, advanced prerequisites/drip release, cohort rosters, attendance, native video, binary uploads, assignment-file malware scanning, and comprehensive moderation/report queues remain incomplete because their required infrastructure or product policy is not present.
- Chapa checkout requires valid server-side credentials and webhook configuration. Payment success is never simulated.
- AI generation, academic/web discovery, YouTube, and image search require their respective configured services. Manual/template authoring remains fully available without them.
- Distributed generation leases, automatic crash recovery/retry, versioned published snapshots, autosave collaboration, and full educator assignment-review UI remain follow-up work.

## Final Verification

- `dotnet build IFA.slnx --no-restore --nologo --verbosity minimal`: passed with 0 warnings and 0 errors.
- `npm run check -- --output human` in `client`: passed with 0 errors and 0 warnings.
- `npm run build` in `client`: passed; both client and SSR bundles were produced by the Node adapter.
- `dotnet ef migrations has-pending-model-changes`: reported no model changes after the migration snapshot.
- `dotnet ef database update` against a fresh disposable PostgreSQL 18 database: applied the complete migration chain through `20261029110932_AddWitforgeCourseWorkflow` successfully.
- `scripts/test-witforge-workflow.ps1 -TestAi` against the isolated PostgreSQL/API instance: passed 38 checks. The run covered manual persistence, server publication validation, public redaction, invite/open enrollment, idempotent completion, quiz answer/attempt enforcement, cold-read progress, assignments/grades, scheduled-class edit/cancel notifications, role and self-access restrictions, private-search isolation, paid-provider gating, real dashboard metrics, PBKDF2 login, and honest `ai_unavailable` behavior.
- `git diff --check`: passed; Git only reported existing CRLF-to-LF normalization notices on three files.

Live Chapa checkout/webhook traffic, a browser-driven E2E suite, live AI/research-provider quality, binary uploads, native conferencing, and deployment were not executed. The disposable API and PostgreSQL validation processes were stopped after the run; the configured Supabase database was not modified.
