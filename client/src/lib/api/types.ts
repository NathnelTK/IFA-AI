/**
 * Wire types mirroring the backend JSON contracts (ASP.NET camelCase output).
 * These are intentionally loose where the API returns anonymous projections.
 */

/** POST /api/auth/{register,login,demo-login}, GET /api/auth/me */
export interface TokenResult {
	accessToken?: string;
	refreshToken?: string;
	expiresAt: string;
	learnerId: string;
	name: string;
	email: string;
	role: string;
	accountType?: 'Learner' | 'Educator';
	educatorVerificationRequired?: boolean;
	mfaRequired?: boolean;
	challengeToken?: string;
}

export interface RegistrationResult extends Partial<TokenResult> {
	verificationRequired?: boolean;
	message?: string;
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
	accountType?: 'Learner' | 'Educator';
	educatorVerificationRequired?: boolean;
	roles?: string[];
	workspaces?: Array<{
		id: string;
		name: string;
		slug: string;
		type: string;
		role: string;
	}>;
	emailVerifiedAt?: string | null;
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
	overview?: string;
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
	isOwner?: boolean;
	hasContentAccess?: boolean;
	isPublished?: boolean;
	visibility?: CourseVisibility;
	enrollmentStatus?: string | null;
}

export type CourseVisibility = 'Public' | 'Unlisted' | 'Private';

export interface EnrollmentResultDto {
	courseId: string;
	status: string;
	paymentRequired: boolean;
}

export interface QuizAttemptDto {
	assessmentId: string;
	scorePercentage: number;
	isPassed: boolean;
	attemptNumber: number;
	submittedAt: string;
}

export interface QuizSubmissionResultDto {
	assessmentId: string;
	scorePercentage: number;
	isPassed: boolean;
	totalQuestions: number;
	correctAnswersCount: number;
	attemptCount: number;
	progressPercentage: number;
	questionFeedbacks: Array<{
		questionId: string;
		selectedOptionIndex: number;
		correctOptionIndex: number;
		isCorrect: boolean;
		explanation: string;
	}>;
}

export interface QuizAuthoringDto {
	id?: string;
	title: string;
	passingScorePercentage: number;
	kind: 0 | 1;
	questions: Array<{
		id?: string;
		prompt: string;
		options: string[];
		correctOptionIndex: number;
		explanation: string;
		targetSkillName: string;
	}>;
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
	isCompleted?: boolean;
}

export interface BackendQuestionDto {
	id: string;
	prompt: string;
	options: string[];
	correctOptionIndex?: number;
	explanation?: string;
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
	attemptCount?: number;
	latestAttempt?: QuizAttemptDto | null;
	isPassed?: boolean;
	maxAttempts?: number;
	questionCount?: number;
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

export interface CourseAppearanceDto {
	canvasLayout: 'Classic' | 'Sidebar' | 'Editorial' | 'Presentation';
	primaryColor: string;
	secondaryColor: string;
	backgroundColor: string;
	surfaceColor: string;
	textColor: string;
	headingFont: 'Manrope' | 'Inter' | 'SourceSerif' | 'Merriweather' | 'IbmPlexSans';
	bodyFont: 'Manrope' | 'Inter' | 'SourceSerif' | 'Merriweather' | 'IbmPlexSans';
	contentWidth: 'Narrow' | 'Standard' | 'Wide';
	spacing: 'Compact' | 'Comfortable' | 'Airy';
	cardStyle: 'Flat' | 'Rounded' | 'Elevated';
	buttonStyle: 'Solid' | 'Soft' | 'Outline';
	borderStyle: 'None' | 'Subtle' | 'Strong';
}

/** GET /api/courses/{id} — full course with modules, lessons, and quizzes. */
export interface CourseDetailDto {
	id: string;
	title: string;
	description: string;
	overview?: string;
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
	isPublished: boolean;
	visibility: CourseVisibility;
	isOwner: boolean;
	hasContentAccess: boolean;
	enrollmentStatus: string | null;
	progressPercentage: number;
	level?: string;
	language?: string;
	learningObjectives?: string[];
	requirePassingQuizzes?: boolean;
	courseAssistantEnabled?: boolean;
	awardsCertificate?: boolean;
	appearance?: CourseAppearanceDto | null;
	appearanceVersion?: number;
	assignmentCount?: number;
	quizCount?: number;
	examCount?: number;
	upcomingSessions?: Array<{
		id: string;
		title: string;
		description: string;
		startTime: string;
		durationMinutes: number;
		timeZone: string;
	}>;
	enrollmentLimit?: number | null;
	enrollmentOpensAt?: string | null;
	enrollmentClosesAt?: string | null;
	/** True when the course is sold (Telebirr/Chapa checkout). */
	isPaid?: boolean;
	/** Price in minor-unit-free decimal string/number as returned by the API. */
	priceAmount?: number | string | null;
	currency?: string | null;
	coverImageUrl?: string | null;
	accentColor?: string | null;
	themeMode?: string | null;
	enrollmentMode?: string | null;
	passingScorePercentage?: number | null;
	maxQuizAttempts?: number | null;
	prerequisites?: string | null;
	instructorBio?: string | null;
	instructorTitle?: string | null;
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
	coverImageUrl?: string | null;
	accentColor?: string | null;
	themeMode?: string | null;
	enrollmentMode?: string | null;
	level?: string;
	language?: string;
	enrollmentCount?: number;
	isOwner?: boolean;
	hasContentAccess?: boolean;
	enrollmentStatus?: string | null;
}

export interface CourseInvitePreviewDto {
	id: string;
	title: string;
	description: string;
	moduleCount: number;
	estimatedDuration: string;
	providerName: string;
	isPaid: boolean;
	priceAmount: number;
	currency: string;
	visibility: CourseVisibility;
	enrollmentMode: string;
	expiresAt: string | null;
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
	estimatedHours?: number;
	estimatedDuration?: string | null;
	confidenceScore?: number | null;
	actionType: string;
	topic?: string;
	courseId?: string;
	evidenceType?: string;
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
		isPublished: boolean;
		visibility: CourseVisibility;
		isPaid: boolean;
		price: number | null;
		enrollments: number;
		activeEnrollments: number;
		pendingApprovals: number;
		avgProgress: number;
		completedEnrollments: number;
		completionRate: number;
		activeLearnersLast30Days: number;
		assessmentAttempts: number;
		avgAssessmentScore: number | null;
		assessmentPassRate: number | null;
		assignmentSubmissions: number;
		pendingGrading: number;
		upcomingLiveSessions: number;
		traffic: Array<{ date: string; enrollments: number; activity: number }>;
	}>;
	totals: { enrollments: number; pendingApprovals: number; publishedCourses: number; totalCourses: number; activeLearnersLast30Days: number; pendingGrading: number; upcomingLiveSessions: number };
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
	checkoutUrl?: string;
	txRef?: string;
	enrollmentStatus?: string;
	message?: string;
}

/** GET /api/payments/verify/{txRef}. */
export interface PaymentVerifyDto {
	status: string;
	enrollmentStatus?: string;
}

/* ------------------------------------------------------------------ */
/* Course Templates                                                   */
/* ------------------------------------------------------------------ */

export interface CourseTemplateLessonDto {
	title: string;
	summary: string;
	contentMarkdown: string;
	readingTimeMinutes: number;
}

export interface CourseTemplateModuleDto {
	title: string;
	summary: string;
	estimatedHours: number;
	lessons: CourseTemplateLessonDto[];
	quizTitle: string;
	quizQuestions: string[];
}

export interface CourseTemplateDto {
	id: string;
	title: string;
	description: string;
	category: string;
	targetAudience: string;
	estimatedDuration: string;
	level: string;
	accentColor: string;
	themeMode: string;
	icon: string;
	keyFeatures: string[];
	modules: CourseTemplateModuleDto[];
	learningObjectives: string[];
	completionRule: string;
	requirePassingQuizzes: boolean;
	assessmentSuggestions: string[];
	sampleAssignmentTitle: string;
	sampleAssignmentInstructions: string;
}

/* ------------------------------------------------------------------ */
/* Live Sessions                                                      */
/* ------------------------------------------------------------------ */

export interface LiveSessionDto {
	id: string;
	courseId: string;
	title: string;
	description: string;
	startTime: string;
	durationMinutes: number;
	timeZone: string;
	meetingUrl: string;
	status: 'Scheduled' | 'Live' | 'Completed' | 'Cancelled';
	createdAt: string;
}

/* ------------------------------------------------------------------ */
/* Course Discussions & Q&A                                           */
/* ------------------------------------------------------------------ */

export interface CourseDiscussionReplyDto {
	id: string;
	content: string;
	isInstructorReply: boolean;
	createdAt: string;
	author: {
		name: string;
		avatarUrl: string;
	};
}

export interface CourseDiscussionDto {
	id: string;
	courseId: string;
	lessonId: string | null;
	title: string;
	content: string;
	isInstructorReply: boolean;
	createdAt: string;
	author: {
		name: string;
		avatarUrl: string;
	};
	replies: CourseDiscussionReplyDto[];
}

/* ------------------------------------------------------------------ */
/* Assignments & Submissions                                          */
/* ------------------------------------------------------------------ */

export interface AssignmentSubmissionDto {
	id: string;
	assignmentId: string;
	learnerId: string;
	learnerName?: string;
	learnerEmail?: string;
	submissionText: string;
	fileUrl?: string | null;
	fileName?: string | null;
	assetId?: string | null;
	submittedAt: string;
	status: 'Submitted' | 'Graded' | 'ResubmissionRequested';
	score?: number | null;
	feedback?: string | null;
	gradedAt?: string | null;
}

export interface AssignmentDto {
	id: string;
	courseId: string;
	title: string;
	instructionsMarkdown: string;
	dueDaysAfterEnrollment: number;
	maxScore: number;
	allowedFileTypes: string;
	maxFileSizeMb?: number;
	createdAt: string;
	submissionsCount?: number;
	pendingGradingCount?: number;
	mySubmission?: AssignmentSubmissionDto | null;
}
