# API Specification

The API is implemented incrementally with each backend feature. The endpoint list below is the target contract, not a requirement to implement every endpoint before Phase 2.

**Current Status:** Partially implemented. Most endpoints exist in plan but not in code.

## Authentication
**STATUS: ⏳ NOT STARTED**

POST /api/auth/register ⏳
POST /api/auth/login ⏳
POST /api/auth/logout ⏳
GET /api/auth/me ⏳

## Learner profile
**STATUS: ⏳ PARTIAL**

GET /api/learners/me ⏳
PUT /api/learners/me ⏳
GET /api/learners/me/skills ✅ (partial - `/api/skills` exists)
PUT /api/learners/me/skills ⏳
GET /api/learners/me/goals ⏳
POST /api/learners/me/goals ⏳

## AI onboarding
**STATUS: ⏳ NOT STARTED**

POST /api/ai/onboarding/sessions ⏳
POST /api/ai/onboarding/sessions/{id}/messages ⏳
POST /api/ai/onboarding/sessions/{id}/complete ⏳

## Research
**STATUS: ⏳ PARTIAL**

POST /api/research ⏳
GET /api/research/{id} ⏳
GET /api/research/{id}/sources ⏳

**Note:** ScholarxivService exists but research endpoint not created.

## Courses
**STATUS: ⏳ PARTIAL**

POST /api/courses/generate ⏳
GET /api/courses/{id} ⏳
GET /api/courses/{id}/modules ⏳
GET /api/courses/{id}/modules/{moduleId} ⏳
POST /api/courses/{id}/enroll ⏳

**Note:** Course sharing endpoint exists (`/api/courses/sharing`), but core course endpoints not implemented.

## Learning
**STATUS: ⏳ NOT STARTED**

POST /api/modules/{moduleId}/generate ⏳
POST /api/lessons/{lessonId}/complete ⏳
GET /api/learners/me/progress ⏳

## Quiz and assessment
**STATUS: ⏳ NOT STARTED**

POST /api/modules/{moduleId}/quiz/generate ⏳
GET /api/quizzes/{quizId} ⏳
POST /api/quizzes/{quizId}/attempts ⏳
POST /api/attempts/{attemptId}/submit ⏳
GET /api/learners/me/assessments ⏳

## AI tutor
**STATUS: ⏳ NOT STARTED**

POST /api/ai/tutor/messages ⏳

## Voice
**STATUS: ⏳ NOT STARTED**

POST /api/voice/session ⏳
POST /api/voice/action ⏳

## Public courses
**STATUS: ⏳ PARTIAL**

GET /api/courses/public ⏳
POST /api/courses/{id}/publish ⏳
POST /api/courses/{id}/share ✅ (implemented as `/api/courses/sharing/share`)

**Note:** Course sharing endpoint exists. Public course discovery endpoint not implemented.

## API implementation rule

The developer implementing a feature owns the endpoint/request/response DTOs for that feature. The PR must follow the existing architecture and update this specification if the contract changes.

Exact DTOs should be finalized in the Application layer when the feature is implemented rather than inventing detailed contracts for unused future features.
