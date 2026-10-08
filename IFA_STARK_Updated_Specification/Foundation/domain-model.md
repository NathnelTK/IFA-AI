# Domain Model

The domain model is the foundation for the database structure. The backend should use the existing Onion Architecture and domain entities as the source for persistent business objects.

Core concepts include:

- User
- LearnerProfile
- Skill
- LearningGoal
- Course
- Enrollment
- Module
- Lesson
- Resource
- ResearchRequest
- ResearchSource
- ResearchPackage
- Quiz
- Question
- AssessmentAttempt
- LearnerAnswer
- Progress
- SkillProgress
- LearningState
- Recommendation
- Activity
- Notification
- CourseShare

Relationships and fields should be finalized in code through the Domain/Application layers and reflected in database migrations.

The database should evolve from actual feature requirements rather than requiring every future table to be finalized before implementation starts.
