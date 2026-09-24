# Database Design

## Database

PostgreSQL through the existing project Infrastructure layer.

## Core tables/entities

- Users
- LearnerProfiles
- Skills
- LearnerSkills
- LearningGoals
- Courses
- CourseModules
- Lessons
- Resources
- Enrollments
- ResearchRequests
- ResearchSources
- ResearchPackages
- Quizzes
- Questions
- AssessmentAttempts
- LearnerAnswers
- ProgressRecords
- LearningStates
- Recommendations
- Activities
- Notifications
- CourseShares

## Design rule

The Domain entities and EF Core configurations are the primary implementation source. Migrations represent the actual database state.

The team does not need to freeze every future database table before development. When a feature is implemented, its required entity relationships and migration are added in that feature PR.

## Environment

The Supabase/PostgreSQL connection must be provided through environment configuration or secure secrets. Do not commit passwords or connection strings containing credentials.
