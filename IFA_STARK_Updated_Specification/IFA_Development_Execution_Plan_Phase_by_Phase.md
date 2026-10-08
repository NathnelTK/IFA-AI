# IFA — Development Execution Plan
## STARK Official Hackathon 2026 · Team XOR

**Status:** Phase 1 completed. Phase 4 UI and Phase 5 Social UI completed. Development now continues from Phase 2 (AI & Research Foundation).
**Goal of this document:** Give the team a clear implementation order, ownership boundaries, PR structure, and delivery rules. This is an execution plan, not a product proposal.

**Implementation Status Summary:**
- ✅ Phase 1: Foundation (Backend + Frontend scaffolding)
- ✅ Phase 4: Learning Experience UI (Dashboard, My Learning, Courses, Skills, Research, Progress, Settings, Search, Notifications, Profile, Theme, Goals)
- ✅ Phase 5: Social Learning UI (Course Sharing, Marketplace, Activity Stream, Peer Comparison, Recommendations)
- ⏳ Phase 2: AI & Research Foundation (NOT STARTED - Next Priority)
- ⏳ Phase 3: Three-Model Pipeline (NOT STARTED - Depends on Phase 2)
- ⏳ Phase 4 AI Integration: PR 4.1 (Learner Intake), PR 4.4 (Course Learning Experience with actual lessons) - UI exists, backend missing
- ⏳ Phase 5 Adaptive: PRs 5.1-5.4 (Assessment tracking, skill gap, adaptive engine) - NOT STARTED
- ⏳ Phase 6 Social Backend: Course sharing/enrollment APIs exist, full social backend needs completion
- ⏳ Phase 7: Account, Search, Platform Polish - Partial (search/notifications UI done, auth not implemented)
- ⏳ Phase 8: Integration, Docker, Deployment - Documentation done, actual deployment not completed

---

# 0. DEVELOPMENT RULES

These rules apply to every phase and every teammate.

## 0.1 Work through PRs, not directly on the main branch

For every task:

1. Create a branch from the current development branch.
2. Implement only the assigned PR scope.
3. Test locally.
4. Update documentation if the change affects architecture, API contracts, environment variables, or setup.
5. Open a PR.
6. Another teammate reviews it.
7. Fix review comments.
8. Merge only after the PR is approved.

Do not mix unrelated features into one PR.

### Branch naming

```text
feature/pr-02-...
feature/pr-03-...
feature/pr-04-...
fix/...
chore/...
docs/...
```

---

# 1. TEAM DEVELOPMENT ROLES

Roles are responsibility areas, not permanent ownership of every task.

### AI / Research
Responsible for:
- Model 1 learner interaction
- learner profiling
- Scholarxiv research flow
- research package format
- Model 2 course architecture
- Model 3 course generation
- prompt/version management
- AI evaluation

### Backend
Responsible for:
- API
- database
- authentication
- course/module/lesson data
- learner progress
- assessment data
- research data
- AI orchestration endpoints
- integrations

### Frontend
Responsible for:
- dashboard
- onboarding/intake UI
- course experience
- research workspace
- skills
- progress
- public courses
- sharing/comparison
- settings/profile/search/notifications

### Integration / DevOps
Responsible for:
- Docker
- environment configuration
- CI/CD
- EthioDeploy
- STARK Changelog
- service integration
- deployment verification

One person can cover multiple roles. What matters is that every PR has a clear owner.

---

# 2. HACKATHON TOOLING RULES

The hackathon tools should be used as part of development, not added as decoration at the end.

## Docker
Use Docker to keep local development and deployment consistent.

At minimum, document:
- services
- ports
- environment variables
- startup commands
- database/service dependencies

## STARK Changelog
Use the changelog continuously.

Every meaningful completed feature should have a short entry covering:
- what changed
- why it changed
- important implementation note
- any breaking/configuration change

Do not wait until the final day to write the changelog.

## EthioDeploy
Use deployment as soon as the first integrated backend/frontend flow is stable.

Deployment is part of development:
- deploy early
- test the deployed API
- test frontend against deployed API
- record configuration requirements
- fix deployment issues while they are still small

## Scholarxiv MCP
Use it for the research part of the AI pipeline.

Research results must be returned in a structured format so later agents can consume them.

## Voxide
Use it for voice interaction with application functionality.

Voice should be able to initiate/continue the learner interaction and trigger supported app actions rather than functioning only as a voice-message feature.

---

# 3. PHASE 1 — FOUNDATION
## STATUS: COMPLETED

Phase 1 is already finished.

The team should **not restart Phase 1** unless an integration blocker is discovered.

Completed foundation areas from the existing plan include:
- project/repository foundation
- Docker/development environment foundation
- backend/frontend foundation
- initial application structure
- core data/API groundwork

### Phase 1 maintenance
Only create fixes when needed:
- broken setup
- broken Docker service
- environment/configuration issue
- foundation API problem
- CI/deployment blocker

---

# 4. PHASE 2 — AI & RESEARCH FOUNDATION
## NEXT PHASE

**Objective:** Build the data contracts and services required before the three-model pipeline is connected.

Do not start by trying to generate the entire course. Build the inputs and outputs first.

---

## PR 2.1 — Learner Profile Contract

### Implement
Create the structured learner profile produced by Model 1.

The profile should support information such as:
- learning goal
- subject/topic
- current level
- target outcome
- available study time
- preferred learning language
- preferred learning style
- constraints
- preferred YouTube channels/creators when provided
- known strengths
- known weaknesses
- learner requirements

### Acceptance criteria
- Profile has a defined schema.
- Backend can store/retrieve it.
- AI layer can consume it.
- Missing optional fields do not break the pipeline.
- Example profile exists for testing.

---

## PR 2.2 — Research Package Contract

### Implement
Create the structured output of the research stage.

The package should separate:
- academic evidence
- practical learning resources
- videos
- documentation/tutorial resources
- source metadata
- relevance to learner requirements

### Acceptance criteria
- Research output has a stable schema.
- Scholarxiv results can be mapped into it.
- Web/YouTube resources can be represented.
- Model 2 can consume the package without knowing how the research was collected.

---

## PR 2.3 — Scholarxiv Research Integration

### Implement
Connect the research workflow to Scholarxiv MCP.

The flow should be:

```text
Learner Profile
      ↓
Research Request
      ↓
Scholarxiv MCP
      ↓
Research Results
      ↓
Normalized Research Package
```

### Acceptance criteria
- A research request can be created from a learner profile.
- Scholarxiv results are captured.
- Results are normalized.
- Failures/timeouts are handled.
- Raw tool output is not passed blindly to Model 2.

---

## PR 2.4 — Practical Resource Search

### Implement
Add the practical-resource side of research.

Support:
- web resources
- YouTube resources
- learner-selected YouTube channels when provided

The system should search for relevant resources rather than automatically accepting every result from a preferred channel.

### Acceptance criteria
- Resources are linked to the learner goal/topic.
- Preferred channels can influence search.
- Results are normalized into the research package.
- Duplicate/irrelevant resources can be filtered.

---

## PR 2.5 — Model 1 Learner Interaction

### Implement
Build Model 1 as the learner-facing discovery/orchestration model.

Model 1 should:
1. Start a conversation.
2. Ask relevant questions.
3. Avoid asking unnecessary questions when enough information is available.
4. Identify the learner goal.
5. Identify current level and constraints.
6. Ask about preferred resources/channels where relevant.
7. Produce the learner profile.
8. Trigger research.

### Acceptance criteria
- Chat interaction works.
- Structured learner profile is produced.
- Research can be triggered.
- Model 1 does not generate the full course itself.
- The model boundary is documented.

---

# 5. PHASE 3 — THREE-MODEL CORE PIPELINE
## OBJECTIVE: CONNECT THE AI SYSTEM

This phase turns the separate AI components into one working pipeline.

---

## PR 3.1 — Model 2 Course Architect

### Implement
Model 2 receives:

```text
Learner Profile
+
Research Package
```

and produces:

```text
Complete Course Blueprint
```

The blueprint should contain:
- course goal
- prerequisites
- modules
- module objectives
- module order
- resources
- assessments
- adaptation points

### Important rule
Model 2 designs the course. It does **not** generate every lesson immediately.

---

## PR 3.2 — JIT / Lazy Module Generation

### Implement
Introduce the lazy-generation boundary.

Flow:

```text
Complete Course Blueprint
        ↓
Current Module Specification
        ↓
Model 3
```

Only the current module is fully materialized.

Future modules remain lightweight blueprint objects until needed.

### Acceptance criteria
- Full blueprint can exist without full lesson generation.
- Current module can be selected.
- Model 3 receives only the required module specification.
- Generated content is persisted.

---

## PR 3.3 — Model 3 Fine-Tuned Course Builder

### Implement
Connect the specialized/fine-tuned course-building model.

Input:

```text
Current Module Specification
+
Research Package / approved resources
+
Relevant learner constraints
```

Output should include:
- lesson content
- explanations
- examples
- exercises
- learning resources
- formative quiz
- module assessment
- takeaways

### Boundary
Model 3 should focus on consistent course-content production.

It should not:
- interview the learner
- perform open-ended learner profiling
- replace Scholarxiv research
- redesign the entire course

---

## PR 3.4 — End-to-End AI Orchestrator

### Implement
Connect:

```text
Learner
 ↓
Model 1
 ↓
Learner Profile
 ↓
Research
 ↓
Research Package
 ↓
Model 2
 ↓
Course Blueprint
 ↓
Current Module
 ↓
Model 3
 ↓
Learning Content
```

### Acceptance criteria
A new learner can complete the complete initial AI flow without manual developer intervention.

---

## PR 3.5 — AI Prompt / Model Versioning

### Implement
Create a consistent place for:
- model configuration
- system prompts
- prompt versions
- structured output rules
- model-specific settings

Do not scatter important prompts throughout controllers/components.

### Acceptance criteria
- Prompts are versioned.
- Model 1, 2, and 3 have clear boundaries.
- Changing a prompt does not require searching the entire codebase.
- Test cases exist for structured outputs.

---

# 6. PHASE 4 — LEARNING EXPERIENCE
## OBJECTIVE: MAKE THE AI PIPELINE USABLE

Build the UI around the already-working AI core.

**Current Status:** UI components implemented, AI backend integration missing.

---

## PR 4.1 — Learner Intake / AI Advisor UI
**STATUS: ⏳ NOT STARTED - UI placeholder exists, needs Model 1 integration**

### Implement
Create the first learner interaction experience.

Support:
- chat
- voice entry through Voxide
- goal discovery
- questions from Model 1
- learner confirmation before course creation

### Done when
A learner can start from an empty account and reach the generated learning plan.

---

## PR 4.2 — Dashboard
**STATUS: ✅ COMPLETED (UI only)**

### Implement
Implement the dashboard from the existing UI direction.

Include the existing planned areas:
- learning goal / hero area ✅ (HeroGoalSuggestions component created)
- Continue Learning ✅ (My Learning page created)
- Explore Public Courses ✅ (Marketplace page created)
- skills/progress summary ✅ (Skills and Progress pages created)
- AI Tutor entry ✅ (AiTutorDock component created)
- recent activity ✅ (ActivityStream component created)
- notifications ✅ (NotificationCenter component created)
- navigation ✅ (Sidebar navigation exists)

**Note:** Data is currently seeded/mock. Backend integration needed for real data.

Do not add decorative widgets that have no backend data.

---

## PR 4.3 — My Learning
**STATUS: ✅ COMPLETED (UI only)**

Implement:
- enrolled courses ✅
- current course ✅
- progress ✅
- continue learning ✅
- completed courses ✅
- course state ✅

**Files:** `client/src/routes/my-learning/+page.svelte`, `client/src/lib/components/ActiveCourseCard.svelte`, `client/src/lib/components/CourseProgressCard.svelte`, `client/src/lib/components/CompletedCourseCard.svelte`

**Note:** Backend APIs for course enrollment and progress tracking need implementation.

---

## PR 4.4 — Course Learning Experience
**STATUS: ⏳ NOT STARTED - Critical missing piece**

### Implement
Implement:
- course overview ⏳
- module list ⏳
- current lesson ⏳
- lesson content ⏳
- resources ⏳
- exercises ⏳
- quiz ⏳
- module assessment ⏳
- completion state ⏳

**Priority:** HIGH - This is the core learning experience that connects the AI pipeline to the learner.

---

## PR 4.5 — My Skills
**STATUS: ✅ COMPLETED (UI + Partial Backend)**

Implement:
- detected skills ✅
- skill level ✅
- strengths ✅
- weaknesses ✅
- improvement areas ✅
- skill changes after assessment ⏳ (needs adaptive engine)

**Files:** `client/src/routes/my-skills/+page.svelte`, `client/src/lib/components/SkillRadar.svelte`, `client/src/lib/components/SkillBreakdown.svelte`, `client/src/lib/components/WeakSkillCard.svelte`, `src/IFA.Application/Skills/Services/SkillProfileService.cs`, `src/IFA.API/Controllers/SkillsController.cs`

**Note:** Backend API exists but data is currently static. Needs integration with adaptive engine.

---

## PR 4.6 — Research Workspace
**STATUS: ✅ COMPLETED (UI only)**

Implement the research area.

Show:
- learner research request ✅
- research status ✅
- sources ✅
- academic papers ✅
- practical resources ✅
- videos ✅
- relevance/context ✅
- resources used in the course ✅

**Files:** `client/src/routes/research/+page.svelte`, `client/src/lib/components/ResearchResults.svelte`, `client/src/lib/components/ResearchSourceCard.svelte`, `client/src/lib/components/ResearchSummary.svelte`

**Note:** Scholarxiv service exists but not fully integrated. Real research workflow needs Phase 2 completion.

---

## PR 4.7 — Progress
**STATUS: ✅ COMPLETED (UI only)**

Implement:
- course progress ✅
- module progress ✅
- assessment results ✅
- skill progress ✅
- learning activity ✅

**Files:** `client/src/routes/progress/+page.svelte`, `client/src/lib/components/ProgressOverview.svelte`, `client/src/lib/components/SkillProgressChart.svelte`, `client/src/lib/components/AssessmentHistory.svelte`, `client/src/lib/components/LearningActivityChart.svelte`

**Note:** Data is currently seeded. Backend progress tracking APIs need implementation.

---

## PR 4.8 — AI Tutor
**STATUS: ✅ COMPLETED (UI only - needs AI integration)**

Implement the tutor entry point inside the learning experience.

The tutor should be aware of the current learner/course context.

**Files:** `client/src/lib/components/AiTutorDock.svelte`, `client/src/lib/components/VoiceCommandOverlay.svelte`

**Note:** UI components exist. AI chat integration needs Phase 2 LLM Gateway completion.

Keep its first version focused. Do not create a second independent course-generation system.

---

# 7. PHASE 5 — ADAPTIVE LEARNING
## OBJECTIVE: MAKE THE SYSTEM ADAPT AFTER ASSESSMENT

This phase is what turns the generated course into an adaptive learning system.

**Current Status:** NOT STARTED - Depends on Phase 3 (Three-Model Pipeline)

---

## PR 5.1 — Assessment Tracking
**STATUS: ⏳ NOT STARTED**

Store:
- answers
- score
- attempt
- completion
- question-level results where needed
- module result

**Priority:** HIGH - Required for adaptive learning

---

## PR 5.2 — Skill Gap Detection
**STATUS: ⏳ NOT STARTED**

From assessment results, identify:
- mastered areas
- weak areas
- incomplete requirements
- areas needing reinforcement

**Priority:** HIGH - Drives adaptive recommendations

---

## PR 5.3 — Adaptive Learning Engine
**STATUS: ⏳ NOT STARTED**

Application-level engine receives assessment/learning data and updates learner state.

Important:

**Adaptive Engine is not Model 4.**

It is application logic.

It determines constraints such as:

```text
Learner is weak in X
→ reinforce X
→ add practice
→ adjust next module emphasis
```

**Priority:** CRITICAL - Core adaptive learning feature

---

## PR 5.4 — Adaptive Course Continuation
**STATUS: ⏳ NOT STARTED**

Send the updated learner state back into the course architecture process.

Flow:

```text
Assessment
 ↓
Adaptive Engine
 ↓
Updated Learner State
 ↓
Next Module Constraints
 ↓
Model 2
 ↓
Next Module
 ↓
Model 3
```

### Acceptance criteria
The next module is demonstrably affected by previous learner performance.

**Priority:** HIGH - Completes the adaptive loop

---

## PR 5.5 — Recommendation Engine
**STATUS: ✅ COMPLETED (UI only)**

Implement recommendations based on:
- learner goal ✅
- skills ✅
- progress ✅
- research ✅
- public courses ✅
- current learning state ✅

**Files:** `client/src/lib/components/RecommendationEngine.svelte`

**Note:** UI component exists with mock data. Backend recommendation algorithm needs implementation.

---

## PR 5.6 — Recent Activity
**STATUS: ✅ COMPLETED (UI only)**

Implement real activity events such as:
- course enrolled ✅
- lesson completed ✅
- assessment completed ✅
- research completed ✅
- course published/shared ✅

**Files:** `client/src/lib/components/ActivityStream.svelte`

**Note:** UI component exists with mock data. Backend activity tracking and event system needs implementation.

---

# 8. PHASE 6 — SOCIAL & PUBLIC LEARNING
**Current Status:** UI completed, Partial backend implemented

## PR 6.1 — Public Course Discovery
**STATUS: ✅ COMPLETED (UI only)**

Implement:
- public courses ✅
- search ✅
- filters ✅
- categories ✅
- course cards ✅
- course detail ⏳

**Files:** `client/src/routes/marketplace/+page.svelte`, `client/src/lib/components/PublicCourseCard.svelte`, `client/src/lib/components/MarketplaceFilters.svelte`

**Note:** UI complete. Backend course publishing and public course APIs need implementation.

---

## PR 6.2 — Course Publishing
**STATUS: ⏳ NOT STARTED**

Allow a learner/creator to publish a course to the public area.

Include appropriate course state:
- private
- published
- unpublished/draft

**Priority:** MEDIUM - Nice to have for hackathon

---

## PR 6.3 — Course Sharing
**STATUS: ✅ COMPLETED (Backend + UI)**

Allow learners to share courses with others.

**Files:** `src/IFA.Application/Courses/Services/CourseSharingService.cs`, `src/IFA.API/Controllers/CourseSharingController.cs`, `client/src/lib/components/CourseShareDialog.svelte`, `client/src/routes/courses/join/[code]/+page.svelte`

**Note:** Share code generation and enrollment flow implemented. Database persistence of share invites needs completion.

---

## PR 6.4 — Peer Progress Comparison
**STATUS: ✅ COMPLETED (UI only)**

Implement invited/allowed comparison of progress.

Keep privacy boundaries clear.

Show comparable learning information rather than exposing private learner data.

**Files:** `client/src/lib/components/PeerProgressComparison.svelte`

**Note:** UI component exists with mock data. Backend peer data fetching and comparison logic needs implementation.

---

# 9. PHASE 7 — ACCOUNT, SEARCH & PLATFORM POLISH
**Current Status:** Partial - Search/Notifications/Settings UI complete, Auth not implemented

## PR 7.1 — Authentication & Account Flow
**STATUS: ⏳ NOT STARTED**

Implement/finalize:
- registration ⏳
- login ⏳
- logout ⏳
- protected routes ⏳
- role/access checks ⏳
- session/token handling ⏳

**Priority:** CRITICAL - Required for multi-user system

---

## PR 7.2 — Profile
**STATUS: ✅ COMPLETED (UI only)**

Implement:
- profile information ✅
- learner preferences ✅
- preferred language ✅
- preferred learning resources/channels ✅
- profile editing ✅

**Files:** `client/src/lib/components/LearnerProfileMenu.svelte`, `client/src/routes/settings/+page.svelte`

**Note:** UI exists. Backend profile management and authentication needed.

---

## PR 7.3 — Settings
**STATUS: ✅ COMPLETED (UI only)**

Implement the settings areas already planned.

**Files:** `client/src/routes/settings/+page.svelte`, `client/src/lib/components/LearningPreferences.svelte`, `client/src/lib/components/ResourcePreferences.svelte`, `client/src/lib/components/NotificationPreferences.svelte`, `client/src/lib/components/ThemePreferences.svelte`

**Note:** All settings UI exists. Backend settings persistence API needed.

Do not create settings that have no actual behavior.

---

## PR 7.4 — Global Search
**STATUS: ✅ COMPLETED (UI only)**

Search across the platform where appropriate:
- courses ✅
- learning content ✅
- public resources ✅
- research/resources ✅

**Files:** `client/src/lib/components/CommandPalette.svelte`

**Note:** UI component exists. Backend search API and indexing needed.

---

## PR 7.5 — Notifications
**STATUS: ✅ COMPLETED (UI only)**

Implement real notifications for meaningful events:
- course ready ✅
- assessment result ✅
- recommendation ✅
- shared course ✅
- relevant learning activity ✅

**Files:** `client/src/lib/components/NotificationCenter.svelte`

**Note:** UI component exists. Backend notification system, event triggers, and delivery needed.

---

## PR 7.6 — Theme / UI Polish
**STATUS: ✅ COMPLETED (UI only)**

Finalize:
- theme ✅
- transitions ⏳
- responsive layout ✅
- loading states ⏳
- empty states ✅
- error states ⏳
- accessibility basics ⏳

**Files:** `client/src/lib/components/ThemePreferences.svelte`

**Note:** Theme toggle exists. Loading states, error handling, and accessibility improvements needed.

---

# 10. PHASE 8 — INTEGRATION, DOCKER, DEPLOYMENT & HACKATHON DELIVERY
**Current Status:** Documentation complete, actual deployment not completed

This phase should overlap with development. It is not something to start only at the end.

## PR 8.1 — Docker Integration
**STATUS: ⏳ PARTIAL - Docker config exists, not tested end-to-end**

Verify all services work together through the documented Docker setup.

Checklist:
- frontend ✅ (Dockerfile exists)
- backend ✅ (Dockerfile exists)
- database ✅ (docker-compose.yml exists)
- AI services/integration configuration ⏳
- environment variables ⏳
- networking ⏳
- startup order ⏳

---

## PR 8.2 — CI / Automated Checks
**STATUS: ⏳ NOT STARTED**

At minimum:
- build ⏳
- tests ⏳
- lint/format checks where configured ⏳
- backend validation ⏳
- frontend build ✅ (npm run check works)

---

## PR 8.3 — EthioDeploy
**STATUS: ⏳ NOT STARTED**

Deploy the working system.

Verify:
- frontend loads ⏳
- API is reachable ⏳
- database works ⏳
- AI flow works ⏳
- environment variables are configured ⏳
- CORS/authentication work ⏳
- production errors are logged ⏳

---

## PR 8.4 — STARK Changelog Finalization
**STATUS: ✅ COMPLETED**

The changelog should show the development progression from foundation to working product.

Include:
- major features ✅
- AI pipeline milestones ⏳
- integration milestones ⏳
- deployment ⏳
- important fixes ✅

**Files:** `docs/stark-changelog.md`

**Note:** Changelog automation script exists and is being used. Will need updates as AI pipeline is implemented.

---

## PR 8.5 — Production Demo Seed Data
**STATUS: ⏳ NOT STARTED**

Create safe demo data for:
- learner ⏳
- course ⏳
- modules ⏳
- assessment ⏳
- skills ⏳
- public course ⏳
- research results ⏳
- activity ⏳

The demo must not depend on manually changing database records during presentation.

---

## PR 8.6 — Final Hackathon QA
**STATUS: ⏳ NOT STARTED**

Test the complete story:

```text
New Learner
 ↓
AI Conversation
 ↓
Learner Profile
 ↓
Research
 ↓
Personalized Course Blueprint
 ↓
Current Module Generated
 ↓
Learning
 ↓
Assessment
 ↓
Skill/Progress Update
 ↓
Adaptive Next Step
 ↓
Continue Learning
```

Also test:
- refresh ⏳
- logout/login ⏳
- empty states ✅
- API failures ⏳
- AI failures ⏳
- mobile/responsive UI ✅
- deployed environment ⏳

---

# 11. CURRENT TEAM START POINT

## Phase 1 is finished.
## Phase 4 UI is finished.
## Phase 5 Social UI is finished.

The team should now begin **Phase 2 (AI & Research Foundation)** - this is the critical missing piece that makes IFA an "AI-powered" platform.

### Current Completed Work Summary

**Phase 1: Foundation** ✅
- Backend scaffolding (ASP.NET Core, Clean Architecture, PostgreSQL)
- Frontend scaffolding (SvelteKit 2, Svelte 5, Tailwind CSS)
- Docker configuration
- STARK Changelog automation

**Phase 4: Learning Experience UI** ✅
- Dashboard with hero suggestions
- My Learning page (active, completed, paused courses)
- Courses Library (search, filter, sort)
- My Skills page (radar charts, skill breakdowns, weak areas)
- Research Workspace (Scholarxiv, web, YouTube sources)
- Progress Dashboard (analytics, activity charts)
- Settings page (learning, resources, notifications, theme)
- Global Search Command Palette
- Notification Center
- Learner Profile Menu
- Theme & Appearance Preferences
- Hero Goal Suggestions

**Phase 5: Social Learning UI** ✅
- Course Sharing (share code generation, enrollment flow)
- Course Marketplace (public course discovery)
- Activity Stream (peer activity feed)
- Peer Progress Comparison (skill benchmarking)
- Recommendation Engine (AI-powered suggestions)

**Note:** All Phase 4 and Phase 5 work is UI-only with seeded/mock data. Backend APIs are partially implemented (e.g., skills, course sharing) but most features need real backend integration.

### First parallel work

**AI / Research** (Highest Priority)
- PR 2.1 Learner Profile Contract
- PR 2.2 Research Package Contract
- PR 2.3 Scholarxiv Research Integration (service exists, needs full implementation)
- PR 2.4 Practical Resource Search (web/YouTube)
- PR 2.5 Model 1 Learner Interaction design

**Backend** (Highest Priority)
- PR 2.1 storage/API support for learner profiles
- PR 2.2 storage/API support for research packages
- PR 2.3 research endpoint/integration layer
- Course enrollment and progress tracking APIs
- Assessment tracking APIs
- Adaptive engine logic

**Frontend** (Lower Priority - UI exists)
- Connect Phase 4 UI to real backend APIs
- Connect Phase 5 UI to real backend APIs
- Build lesson viewer (PR 4.4 - currently missing)
- Build quiz taking interface (currently missing)
- Do not build disconnected mock features

**Integration / DevOps**
- Test Docker end-to-end
- Configure environment variables for AI services
- Configure EthioDeploy path
- Set up CI/CD if time permits

The team can work in parallel, but dependent PRs should merge before the next layer is built.

### Critical Path (Must Work for Hackathon)

```text
Phase 2: AI Foundation (BLOCKER)
  ↓
Phase 3: Three-Model Pipeline (BLOCKER)
  ↓
Phase 4: Course Learning Experience (UI exists, backend missing)
  ↓
Phase 5: Adaptive Engine (NOT STARTED)
  ↓
Phase 8: Deployment & QA
```

### Nice-to-Have (Can defer if time limited)

- Phase 6: Course Publishing
- Phase 7: Authentication (can use single-user demo mode)
- Phase 7: Full notification system
- Phase 8: CI/CD

---

# 12. DEPENDENCY ORDER

```text
PHASE 1
Foundation
   │
   ▼
PHASE 2
Learner Profile + Research Package
   │
   ├──────────────┐
   ▼              ▼
Model 1       Scholarxiv/Web/YouTube
   │              │
   └──────┬───────┘
          ▼
PHASE 3
Model 2 Course Blueprint
          │
          ▼
JIT Current Module
          │
          ▼
Model 3 Course Builder
          │
          ▼
End-to-End AI Pipeline
          │
          ▼
PHASE 4
Learning Experience
          │
          ▼
PHASE 5
Assessment + Adaptive Engine
          │
          ▼
Next Module Adaptation
          │
          ▼
PHASE 6
Public + Social Learning
          │
          ▼
PHASE 7
Account + Search + Notifications + Polish
          │
          ▼
PHASE 8
Docker + CI + EthioDeploy + QA + Demo
```

---

# 13. PR STANDARD

Every PR description should use this structure:

```text
## What
What is being implemented?

## Why
Why is this required?

## Scope
What files/modules/services are expected to change?

## Dependencies
Which PRs must exist first?

## Implementation
How should the feature work?

## Acceptance Criteria
- [ ] Requirement 1
- [ ] Requirement 2
- [ ] Requirement 3

## Testing
How was it tested?

## Screenshots / Evidence
Add screenshots, API responses, logs, or demo evidence when useful.

## Changelog
What should be added to the STARK Changelog?
```

---

# 14. DEFINITION OF DONE

A PR is not done just because the code works on one machine.

A PR is done when:

- [ ] implementation is complete
- [ ] code is integrated with the correct layer
- [ ] no unrelated features were added
- [ ] local testing passes
- [ ] required API/data contracts are respected
- [ ] errors are handled
- [ ] Docker/setup still works if affected
- [ ] documentation is updated if needed
- [ ] STARK Changelog is updated when appropriate
- [ ] PR is reviewed
- [ ] review comments are resolved
- [ ] PR is merged

---

# 15. PRIORITY RULE

If time becomes limited, protect the core pipeline first.

### Must work

```text
Model 1
→ Research
→ Model 2
→ JIT Module
→ Model 3
→ Learning
→ Assessment
→ Adaptive Engine
→ Next Module
```

### Then build

```text
Dashboard
My Learning
My Skills
Research Workspace
Progress
AI Tutor
```

### Then build

```text
Public Courses
Sharing
Peer Comparison
Recommendations
Notifications
Search
```

### Final polish

```text
Theme
Animations
Extra dashboard details
Non-essential visual enhancements
```

Do not sacrifice the working AI learning loop just to add more dashboard features.

---

# 16. FINAL DEVELOPMENT PRINCIPLE

Every feature should answer one of these questions:

1. Does it help understand the learner?
2. Does it help research what the learner needs?
3. Does it help design the learning path?
4. Does it generate the current learning content?
5. Does it measure learning?
6. Does it adapt what comes next?
7. Does it make the learning experience usable?
8. Does it support sharing/public learning?
9. Does it make the system reliable and deployable?

If a feature does none of these, it should not take priority over the core implementation.
