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
	/** True when the course is sold (Telebirr/Chapa checkout). */
	isPaid?: boolean;
	/** Price in minor-unit-free decimal string/number as returned by the API. */
	priceAmount?: number | string | null;
	currency?: string | null;
	coverImageUrl?: string | null;
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
	/** Paid-courses surface these on the marketplace. */
	isPaid?: boolean;
	priceAmount?: number | string | null;
	currency?: string | null;
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

/* ------------------------------------------------------------------ */
/* Educator Studio                                                     */
/* ------------------------------------------------------------------ */

/** GET /api/educator/projects (list item) and POST response. */
export interface EducatorProjectDto {
	id: string;
	title: string;
	description: string;
	status: string;
	sourceCount: number;
	courseId: string | null;
	createdAt: string;
	updatedAt: string;
}

/** Source inside GET /api/educator/projects/{id}. */
export interface EducatorSourceDto {
	id: string;
	url: string;
	type: string;
	title: string;
	description: string;
	status: string;
	failureReason: string | null;
	addedBy: string;
	included: boolean;
}

/** Conflict inside GET /api/educator/projects/{id} and the analyze response. */
export interface EducatorConflictDto {
	id: string;
	kind: string;
	sourceIds: string[];
	explanation: string;
	suggestedResolutions: string[];
	resolution: string | null;
	manualNote: string | null;
	resolvedBy: string | null;
}

/** Blueprint module inside the blueprint JSON string. */
export interface BlueprintChapterDto {
	title: string;
	summary: string;
	sourceUrls: string[];
}

export interface BlueprintModuleDto {
	title: string;
	description: string;
	estimatedHours: number;
	sources: Array<{ url: string; title: string }>;
	chapters: BlueprintChapterDto[];
}

/** Parsed shape of the blueprint JSON string returned by blueprint/generate. */
export interface CourseBlueprint {
	title: string;
	description: string;
	category: string;
	estimatedDuration: string;
	modules: BlueprintModuleDto[];
}

/** GET /api/educator/projects/{id}. */
export interface EducatorProjectDetailDto {
	id: string;
	title: string;
	description: string;
	status: string;
	sources: EducatorSourceDto[];
	conflicts: EducatorConflictDto[];
	blueprint: { json: string; approvedAt: string | null } | null;
	generationJobs: Array<{ id: string; moduleIndex: number; moduleTitle: string; status: string; error: string | null }>;
	courseId: string | null;
	createdAt: string;
	updatedAt: string;
}

/** GET /api/educator/projects/{id}/status. */
export interface EducatorGenerationStatusDto {
	status: string;
	jobs: Array<{ moduleIndex: number; moduleTitle: string; status: string; error: string | null }>;
	completedCount: number;
	totalCount: number;
	courseId: string | null;
}

/** GET /api/educator/dashboard. */
export interface EducatorDashboardDto {
	courses: Array<{
		courseId: string;
		title: string;
		isPublic: boolean;
		isPaid: boolean;
		price: number | null;
		enrollments: number;
		activeEnrollments: number;
		pendingApprovals: number;
		avgProgress: number;
		traffic: Array<{ date: string; enrollments: number; activity: number }>;
	}>;
	totals: { enrollments: number; pendingApprovals: number; publishedCourses: number };
}

/** GET /api/educator/enrollments?status=PendingApproval. */
export interface EducatorEnrollmentDto {
	enrollmentId: string;
	courseId: string;
	courseTitle: string;
	learnerId: string;
	learnerName: string;
	status: string;
	enrolledAt: string;
}

/* ------------------------------------------------------------------ */
/* Payments                                                            */
/* ------------------------------------------------------------------ */

/** POST /api/payments/initialize. */
export interface PaymentInitializeDto {
	checkoutUrl: string;
	txRef: string;
}

/** GET /api/payments/verify/{txRef}. */
export interface PaymentVerifyDto {
	status: string;
	enrollmentStatus: string;
}
