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

## [2026-09-24] FEATURE: Right Sidebar Analytics & Peer Comparison Radar (PR 4.3)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.3
- **Type:** FEATURE
- **Summary:** Added right sidebar analytics and peer comparison radar to the dashboard. Expanded peer comparison store data with user/peer progress and per-skill comparison metrics. Created PeerComparisonView component to display detailed side-by-side progress and skill radar metrics with user/peer legend and share/compare actions.
- **Files:** `client/src/lib/stores/dashboardStore.ts`, `client/src/lib/types/index.ts`, `client/src/lib/components/PeerComparisonView.svelte`, `client/src/routes/+page.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.
- **Problem / Solution:** Store needed additional peer comparison data structure; added dedicated PeerComparison and SkillComparison types to handle the expanded data model.

## [2026-09-24] FEATURE: AI Tutor Floating Dock & Voice Command Visualizer (PR 4.4)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.4
- **Type:** FEATURE
- **Summary:** Enhanced AI tutor dock with voice command capabilities. Created VoiceCommandOverlay component for voice input visualization and audio HUD feedback. Added voice-command button in tutor dock, integrated voice input callback into tutor chat, and implemented simulated voice question behavior with visual recognition/processing feedback.
- **Files:** `client/src/lib/components/VoiceCommandOverlay.svelte`, `client/src/lib/components/AiTutorDock.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: My Learning Dashboard (PR 4.5)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.5
- **Type:** FEATURE
- **Summary:** Created dedicated My Learning page with active, completed, and paused course tabs. Implemented ActiveCourseCard, CourseProgressCard, and CompletedCourseCard components for different course states. Features include current progress, current module, next lesson, continue learning actions, JIT generation status, study-time summary, completion percentage, module statistics, and share/compare progress affordances.
- **Files:** `client/src/routes/my-learning/+page.svelte`, `client/src/lib/components/ActiveCourseCard.svelte`, `client/src/lib/components/CourseProgressCard.svelte`, `client/src/lib/components/CompletedCourseCard.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Courses Library (PR 4.6)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.6
- **Type:** FEATURE
- **Summary:** Implemented personal Courses Library page for enrolled courses. Created CourseLibrary component with grid/list view modes and CourseFilterBar for filtering and sorting. Features include search, status filtering (all/in-progress/completed/bookmarked), sorting (recent/progress/name/duration), course cards with progress/status/duration/bookmarking, and empty state for no results.
- **Files:** `client/src/routes/courses/+page.svelte`, `client/src/lib/components/CourseLibrary.svelte`, `client/src/lib/components/CourseFilterBar.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.
- **Problem / Solution:** Fixed status naming inconsistency between `inProgress` and `in-progress` in filter logic to ensure proper filtering of in-progress courses.

## [2026-09-24] FEATURE: My Skills & Skill Profile (PR 4.7)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.7
- **Type:** FEATURE
- **Summary:** Implemented My Skills page with skill radar charts and skill breakdowns. Created SkillRadar component for visual skill representation, SkillBreakdown for skill categories and progress, and WeakSkillCard for highlighting areas needing improvement. Added SkillProfileService backend service, GetLearnerSkillsQuery DTOs, and SkillsController API endpoint for skill data management.
- **Files:** `client/src/routes/my-skills/+page.svelte`, `client/src/lib/components/SkillRadar.svelte`, `client/src/lib/components/SkillBreakdown.svelte`, `client/src/lib/components/WeakSkillCard.svelte`, `src/IFA.Application/Skills/Queries/GetLearnerSkillsQuery.cs`, `src/IFA.Application/Skills/Services/SkillProfileService.cs`, `src/IFA.API/Controllers/SkillsController.cs`, `src/IFA.Infrastructure/DependencyInjection.cs`
- **Verification:** Frontend `npm run check` reports 0 errors and 0 warnings. Backend `dotnet build` succeeds with 0 warnings and 0 errors.

## [2026-09-24] FEATURE: Research Workspace (PR 4.8)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.8
- **Type:** FEATURE
- **Summary:** Implemented Research page with research history and detailed source display. Created ResearchResults component for browsing research history, ResearchSourceCard for individual research sources, and ResearchSummary for research summaries. Displays Scholarxiv academic sources, web resources, and YouTube videos with source credibility, type, and ability to add to learning path.
- **Files:** `client/src/routes/research/+page.svelte`, `client/src/lib/components/ResearchResults.svelte`, `client/src/lib/components/ResearchSourceCard.svelte`, `client/src/lib/components/ResearchSummary.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Full Progress Dashboard (PR 4.9)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.9
- **Type:** FEATURE
- **Summary:** Implemented comprehensive Progress page with learning analytics. Created ProgressOverview for overall statistics, SkillProgressChart for skill visualization, AssessmentHistory for quiz/test history, and LearningActivityChart for weekly activity tracking. Displays overall progress, study time, learning streak, achievements, weak areas with recommended practice actions, and time statistics.
- **Files:** `client/src/routes/progress/+page.svelte`, `client/src/lib/components/ProgressOverview.svelte`, `client/src/lib/components/SkillProgressChart.svelte`, `client/src/lib/components/AssessmentHistory.svelte`, `client/src/lib/components/LearningActivityChart.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Settings & Learning Preferences (PR 4.10)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.10
- **Type:** FEATURE
- **Summary:** Implemented Settings page with tabbed interface for preferences. Created LearningPreferences for learning style and goals, ResourcePreferences for content and channel preferences, and NotificationPreferences for notification toggles. Settings include language, learning style, study hours, difficulty, video preference, YouTube channels, topics, exclusions, notifications (reminders, alerts, recommendations, email digest), and account settings (name, email, timezone).
- **Files:** `client/src/routes/settings/+page.svelte`, `client/src/lib/components/LearningPreferences.svelte`, `client/src/lib/components/ResourcePreferences.svelte`, `client/src/lib/components/NotificationPreferences.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Global Search Command Palette (PR 4.11)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.11
- **Type:** FEATURE
- **Summary:** Created CommandPalette component for global search and navigation. Features keyboard navigation with arrow keys and Enter to select, Escape key to close, search results including courses, lessons, and pages, type-based icon and color coding for result categories, and keyboard shortcuts displayed in footer for UX. Can be toggled via props for integration with header.
- **Files:** `client/src/lib/components/CommandPalette.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Notification Center (PR 4.12)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.12
- **Type:** FEATURE
- **Summary:** Created NotificationCenter component for managing notifications. Notification types include course updates, achievements, recommendations, and reminders. Features unread count badge on notification bell, mark individual or all notifications as read, type-based icon and color coding for notification categories, click notification to navigate to relevant content, and empty state for no notifications.
- **Files:** `client/src/lib/components/NotificationCenter.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Learner Profile Menu (PR 4.13)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.13
- **Type:** FEATURE
- **Summary:** Created LearnerProfileMenu component for user account navigation. Displays user avatar, name, and email in dropdown. Quick navigation to My Learning, Courses, Skills, Progress, and Settings. Includes sign out action, click outside to close functionality, and responsive design with hidden name on mobile.
- **Files:** `client/src/lib/components/LearnerProfileMenu.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Theme & Appearance Preferences (PR 4.14)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.14
- **Type:** FEATURE
- **Summary:** Created ThemePreferences component for theme customization. Features theme selection (Light, Dark, System), accent color selection with color swatches, font size options (Small, Medium, Large, Extra Large), compact mode toggle for reduced spacing, and reduced motion toggle for accessibility. Integrated into Settings page as new Theme tab.
- **Files:** `client/src/lib/components/ThemePreferences.svelte`, `client/src/routes/settings/+page.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Hero Goal Suggestions (PR 4.15)
- **Author:** Team XOR
- **Phase:** Phase 4 / PR 4.15
- **Type:** FEATURE
- **Summary:** Created HeroGoalSuggestions component for IFA-recommended learning goals. Displays personalized goal suggestions based on learner progress. Goal cards show title, description, estimated time, and difficulty with difficulty-based color coding (Beginner, Intermediate, Advanced) and icon-based goal categorization. Click to select and start learning path with view all suggestions link for expanded options.
- **Files:** `client/src/lib/components/HeroGoalSuggestions.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Course Sharing & Enrollment (PRs 5.1 + 5.7)
- **Author:** Team XOR
- **Phase:** Phase 5 / PRs 5.1 + 5.7
- **Type:** FEATURE
- **Summary:** Implemented course sharing and enrollment system. Added CourseSharingService backend service for share code generation and validation. Created ShareCourseCommand DTOs and CourseSharingController API endpoint. Created CourseShareDialog component for generating share links with optional expiration and message. Created course join route at /courses/join/[code] for enrollment via share code. Integrated with CourseShareInvite entity and dependency injection.
- **Files:** `src/IFA.Application/Courses/Commands/ShareCourseCommand.cs`, `src/IFA.Application/Courses/Services/CourseSharingService.cs`, `src/IFA.API/Controllers/CourseSharingController.cs`, `client/src/lib/components/CourseShareDialog.svelte`, `client/src/routes/courses/join/[code]/+page.svelte`, `src/IFA.Infrastructure/DependencyInjection.cs`
- **Verification:** Backend `dotnet build` succeeds with 0 warnings and 0 errors. Frontend `npm run check` reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Course Marketplace & Discovery (PRs 5.2 + 5.8)
- **Author:** Team XOR
- **Phase:** Phase 5 / PRs 5.2 + 5.8
- **Type:** FEATURE
- **Summary:** Implemented Course Marketplace for public course discovery. Created Marketplace page with search, category filters, and grid/list view toggle. Created PublicCourseCard component with rating, reviews, enrollment stats, and course metadata. Created MarketplaceFilters component for category-based filtering. Course cards display thumbnail, instructor, rating, reviews, enrollments, level, duration, modules, and tags. Includes enroll and bookmark actions for public courses. Statistics showing total courses and enrollments.
- **Files:** `client/src/routes/marketplace/+page.svelte`, `client/src/lib/components/PublicCourseCard.svelte`, `client/src/lib/components/MarketplaceFilters.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.

## [2026-09-24] FEATURE: Social Features & Recommendation Engine (PRs 5.3 + 5.4 + 5.5 + 5.6)
- **Author:** Team XOR
- **Phase:** Phase 5 / PRs 5.3 + 5.4 + 5.5 + 5.6
- **Type:** FEATURE
- **Summary:** Implemented social learning features and AI-powered recommendations. Created ActivityStream component for peer activity feed with enrollment, completion, achievement, and recommendation activity types. Created PeerProgressComparison component for skill-level peer comparison with overall progress and skill breakdown charts. Created RecommendationEngine component for AI-powered course recommendations with confidence scores and personalization reasons. Type-based icon and color coding for activity items and visual skill comparison bar charts for peer benchmarking.
- **Files:** `client/src/lib/components/ActivityStream.svelte`, `client/src/lib/components/PeerProgressComparison.svelte`, `client/src/lib/components/RecommendationEngine.svelte`
- **Verification:** `npm run check` in client reports 0 errors and 0 warnings.
