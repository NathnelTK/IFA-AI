# IFA — STARK Engineering Specification

**Team:** Team XOR  
**Project:** IFA  
**Hackathon:** STARK Official Hackathon 2026

## Purpose

This documentation reorganizes the existing IFA development execution plan into the engineering-document structure requested by the team.

The development direction remains the same:

- Phase 1 foundation is complete ✅
- Phase 4 learning experience UI is complete ✅
- Phase 5 social learning UI is complete ✅
- Current team priority is backend, API, database, integrations, and AI orchestration
- The AI system will use Ollama with one free locally supported model
- No fine-tuning is planned for the hackathon
- ScholarXiv MCP is used for research
- Voxide is used for voice interaction with application functionality
- AI responsibilities are implemented as role-based workflows around the same model rather than requiring a separate trained model for every feature
- Learner profile and learning state are persisted and controlled by the backend
- API contracts grow with each implemented feature and are reviewed through PRs

## Current Implementation Status

### ✅ Completed
- **Phase 1:** Foundation (Backend + Frontend scaffolding, Docker, STARK Changelog)
- **Phase 4:** Learning Experience UI (Dashboard, My Learning, Courses, Skills, Research, Progress, Settings, Search, Notifications, Profile, Theme, Goals)
- **Phase 5:** Social Learning UI (Course Sharing, Marketplace, Activity Stream, Peer Comparison, Recommendations)
- **Phase 6 Documentation:** README, Pitch Deck, STARK Changelog updates

### ⏳ Not Started (Critical)
- **Phase 2:** AI & Research Foundation (Learner Profile, Research Package, Scholarxiv, Web/YouTube, Model 1)
- **Phase 3:** Three-Model Pipeline (Model 2 Course Architect, JIT Generation, Model 3 Course Builder, Adaptive Engine)
- **Phase 4 AI Integration:** Learner Intake UI, Course Learning Experience (lesson viewer, quiz interface)
- **Phase 5 Adaptive:** Assessment Tracking, Skill Gap Detection, Adaptive Engine

### ⏳ Partial (UI Complete, Backend Missing)
- Course enrollment and progress tracking APIs
- Skills backend (API exists, data static)
- Course sharing (API exists, needs database persistence)
- Recommendations (UI exists, backend algorithm missing)
- Activity tracking (UI exists, event system missing)
- Search (UI exists, backend indexing missing)
- Notifications (UI exists, delivery system missing)
- Settings (UI exists, persistence API missing)

## Source of truth

The original phase-by-phase execution plan remains the development sequence. This folder translates that plan into product, architecture, API, workflow, security, testing, and project-management documents.

The `IFA_Development_Execution_Plan_Phase_by_Phase.md` file has been updated with current implementation status for each PR.

Do not restart Phase 1 unless a real integration blocker is found.

## Implementation principle

Documentation defines the intended contracts. Phases define implementation order. Developers implement the feature and its API endpoints together. PR review is used to resolve contract changes rather than blocking the whole team on designing every future endpoint in advance.

## Next Priority

**Phase 2 (AI & Research Foundation)** is the critical blocker. All Phase 4 and Phase 5 UI exists but cannot function without the AI backend. The team should focus on:

1. PR 2.1: Learner Profile Contract
2. PR 2.2: Research Package Contract
3. PR 2.3: Scholarxiv Research Integration
4. PR 2.4: Web & YouTube Resource Search
5. PR 2.5: Model 1 Learner Interaction

Once Phase 2 is complete, Phase 3 (Three-Model Pipeline) can be implemented, which will enable the actual AI-powered learning experience.
