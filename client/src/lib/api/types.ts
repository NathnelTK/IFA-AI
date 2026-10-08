/**
 * Wire types mirroring the backend JSON contracts (ASP.NET camelCase output).
 * These are intentionally loose where the API returns anonymous projections.
 */

/** POST /api/auth/{register,login,demo-login}, GET /api/auth/me */
export interface TokenResult {
	accessToken: string;
	refreshToken: string;
	expiresAt: string;
	learnerId: string;
	name: string;
	email: string;
	role: string;
}

export interface LearnerProfileDto {
	id: string;
	learnerId: string;
	learningGoal: string;
	subject: string;
	currentLevel: string;
	targetOutcome: string;
	weeklyStudyHours: number;
	preferredLanguage: string;
	learningStyle: string;
	constraints: string;
	preferredYouTubeChannelsJson: string;
	knownStrengthsJson: string;
	knownWeaknessesJson: string;
	requirements: string;
	updatedAt: string;
}

export interface LearnerStateDto {
	id: string;
	learnerId: string;
	activeStreakDays: number;
	totalHoursLearned: number;
	completedLessonsCount: number;
	completedQuizzesCount: number;
	updatedAt: string;
}

export interface LearnerSettingsDto {
	id: string;
	learnerId: string;
	dailyReminders: boolean;
	weeklyDigest: boolean;
	assessmentResults: boolean;
	courseRecommendations: boolean;
	theme: string;
	accentColor: string;
	fontSize: string;
	compactMode: boolean;
	reducedMotion: boolean;
	preferredYouTubeChannelsJson: string;
	excludedYouTubeChannelsJson: string;
	preferredTopicsJson: string;
}

export interface LearnerMeDto {
	id: string;
	name: string;
	email: string;
	role: string;
	avatarUrl: string;
	overallProgress: number;
	profile: LearnerProfileDto | null;
	state: LearnerStateDto | null;
	settings: LearnerSettingsDto | null;
}

/** GET /api/courses — enrolled course summaries. */
export interface CourseSummaryDto {
	id: string;
	title: string;
	description: string;
	category: string;
	targetAudience: string;
	thumbnailUrl: string;
	providerName: string;
	estimatedDuration: string;
	rating: number;
	reviewCount: string;
	isPublic: boolean;
	shareCode: string;
	progressPercentage: number;
	lastAccessedAt: string;
	totalModules: number;
	completedModules: number;
	activeModuleNumber: number;
}

export interface BackendLessonDto {
	id: string;
	moduleId: string;
	lessonNumber: number;
	title: string;
	summary: string;
	contentMarkdown: string;
	readingTimeMinutes: number;
	youTubeVideoId: string | null;
	youTubeVideoTitle: string | null;
}

export interface BackendQuestionDto {
	id: string;
	prompt: string;
	options: string[];
	correctOptionIndex: number;
	explanation: string;
	targetSkillName: string;
	bloomTaxonomyLevel: string;
}

export interface BackendQuizDto {
	id: string;
	moduleId: string;
	title: string;
	passingScorePercentage: number;
	kind: number;
	orderIndex: number;
	questions: BackendQuestionDto[];
}

export interface BackendModuleDto {
	id: string;
	courseId: string;
	moduleNumber: number;
	title: string;
	summary: string;
	estimatedHours: number;
	generationStatus: number | string;
	lessons: BackendLessonDto[];
	quizzes: BackendQuizDto[];
}

/** GET /api/courses/{id} — full course with modules, lessons, and quizzes. */
export interface CourseDetailDto {
	id: string;
	title: string;
	description: string;
	category: string;
	targetAudience: string;
	thumbnailUrl: string;
	providerName: string;
	badge: string | null;
	rating: number;
	reviewCount: string;
	estimatedDuration: string;
	isPublic: boolean;
	shareCode: string;
	modules: BackendModuleDto[];
}

/** GET /api/courses/marketplace */
export interface MarketplaceCourseDto {
	id: string;
	title: string;
	description: string;
	category: string;
	targetAudience: string;
	thumbnailUrl: string;
	providerName: string;
	rating: number;
	reviewCount: string;
	estimatedDuration: string;
	badge: string | null;
	moduleCount: number;
	shareCode: string;
}

/** GET /api/skills/{learnerId} */
export interface LearnerSkillDto {
	name: string;
	percentage: number;
	color: string;
	improvement: string;
}

export interface SkillCategoryDto {
	name: string;
	skills: LearnerSkillDto[];
}

export interface LearnerSkillsResultDto {
	categories: SkillCategoryDto[];
	overallImprovement: number;
}

/** GET /api/recommendations — anonymous projection, fields vary by action type. */
export interface RecommendationDto {
	id: string;
	title: string;
	reason: string;
	category: string;
	estimatedHours: number;
	confidenceScore: number;
	actionType: string;
	topic?: string;
	courseId?: string;
}

/** GET /api/activity */
export interface LearningActivityDto {
	id: string;
	activityType: string;
	title: string;
	description: string;
	metadataJson: string;
	createdAt: string;
}

/** GET /api/notifications */
export interface NotificationDto {
	id: string;
	type: string;
	title: string;
	message: string;
	linkUrl: string | null;
	isRead: boolean;
	createdAt: string;
}

export interface PeerComparisonDto {
	courseId: string;
	userProgress: number;
	peerAverage: number;
	topPercentile: number;
	totalEnrolled: number;
	rank: number;
	paceStatus: string;
}

/** GET /api/progress — real per-learner aggregates for the Progress page. */
export interface ProgressStatsDto {
	totalCourses: number;
	completedCourses: number;
	inProgressCourses: number;
	overallProgress: number;
	totalStudyTime: string;
	totalHours: number;
	weeklyStudyTime: string;
	weeklyStudyHours: number;
	learningStreak: number;
}

export interface WeeklyActivityDto {
	day: string;
	date: string;
	hours: number;
}

export interface ProgressSkillDto {
	name: string;
	percentage: number;
	improvement: string;
	color: string;
}

export interface ProgressAssessmentDto {
	id: string;
	type: string;
	title: string;
	course: string;
	courseId: string;
	score: number;
	passed: boolean;
	attemptNumber: number;
	submittedAt: string;
}

export interface ProgressWeakAreaDto {
	skill: string;
	currentLevel: number;
	targetLevel: number;
}

export interface LearnerProgressDto {
	stats: ProgressStatsDto;
	weeklyActivity: WeeklyActivityDto[];
	skillProgress: ProgressSkillDto[];
	assessments: ProgressAssessmentDto[];
	weakAreas: ProgressWeakAreaDto[];
	avgQuizScore: number;
	totalQuestions: number;
}

/** GET /api/auth/demo-accounts */
export interface DemoAccountDto {
	name: string;
	email: string;
	role: string;
	avatarUrl: string;
}

/** POST /api/ai/intake/message — Model 1 (Learning Advisor / intake). */
export interface IntakeProfileDto {
	learningGoal: string;
	subject: string;
	currentLevel: string;
	targetOutcome: string;
	weeklyStudyHours: number;
	preferredLanguage: string;
	learningStyle: string;
	constraints: string;
	preferredYouTubeChannels: string[];
	knownStrengths: string[];
	knownWeaknesses: string[];
}

export interface IntakeResponseDto {
	reply: string;
	isProfileReady: boolean;
	profile: IntakeProfileDto | null;
	suggestedResearchTopics: string[];
	followUpQuestions: string[];
}

export interface IntakeHistoryMessage {
	role: 'user' | 'assistant';
	content: string;
}

/** POST /api/ai/course/propose — Model 2 (Course Architect) blueprint. */
export interface PipelineProposalModuleDto {
	moduleNumber: number;
	title: string;
	summary: string;
	estimatedHours: number;
	keyTopics: string[];
}

export interface CoursePipelineProposalDto {
	courseTitle: string;
	targetGoal: string;
	totalEstimatedHours: number;
	modules: PipelineProposalModuleDto[];
}
