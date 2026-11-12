import { api } from './client';
import type {
	AssignmentDto,
	AssignmentSubmissionDto,
	CourseDetailDto,
	CourseInvitePreviewDto,
	CourseDiscussionDto,
	CoursePipelineProposalDto,
	CourseSummaryDto,
	CourseTemplateDto,
	CourseVisibility,
	DemoAccountDto,
	EducatorConflictDto,
	EducatorDashboardDto,
	EducatorEnrollmentDto,
	EducatorGenerationStatusDto,
	EducatorProjectDetailDto,
	EducatorProjectDto,
	EducatorSourceDto,
	EnrollmentResultDto,
	IntakeHistoryMessage,
	IntakeResponseDto,
	LearnerMeDto,
	LearnerProfileDto,
	LearnerSettingsDto,
	LearnerSkillsResultDto,
	LearnerProgressDto,
	LearningActivityDto,
	LiveSessionDto,
	MarketplaceCourseDto,
	NotificationDto,
	PaymentInitializeDto,
	PaymentVerifyDto,
	PeerComparisonDto,
	QuizAuthoringDto,
	QuizSubmissionResultDto,
	RecommendationDto,
	RegistrationResult,
	TokenResult
} from './types';

/**
 * Domain-grouped API calls. Every function maps 1:1 to a backend route so the
 * request path stays traceable from the UI back to a controller/endpoint.
 */

export const authApi = {
	register: (body: { name: string; email: string; password: string }) =>
		api.post<RegistrationResult>('/api/auth/register', body),
	login: (body: { email: string; password: string }) =>
		api.post<TokenResult>('/api/auth/login', body),
	completeMfaLogin: (body: { challengeToken: string; code: string }) =>
		api.post<TokenResult>('/api/auth/login/mfa', body),
	/** No credentials required — issues a token for the seeded demo learner. */
	demoLogin: () => api.post<TokenResult>('/api/auth/demo-login'),
	exchangeLegacy: (body: { host: string; site: 'Auth' | 'App' | 'Public' }) =>
		api.post<TokenResult>('/api/auth/legacy-exchange', body),
	logout: () => api.post<void>('/api/auth/logout'),
	verifyEmail: (token: string) => api.post<void>('/api/auth/verify-email', { token }),
	resendVerification: (email: string) => api.post<{ message: string }>('/api/auth/resend-verification', { email }),
	forgotPassword: (email: string) => api.post<{ message: string }>('/api/auth/forgot-password', { email }),
	resetPassword: (token: string, password: string) => api.post<void>('/api/auth/reset-password', { token, password }),
	beginMfaEnrollment: () => api.post<{ secret: string; otpauthUri: string }>('/api/auth/mfa/enroll'),
	confirmMfaEnrollment: (code: string) => api.post<{ recoveryCodes: string[] }>('/api/auth/mfa/confirm', { code }),
	disableMfa: (code: string) => api.post<void>('/api/auth/mfa/disable', { code }),
	/** Seeded demo learners the sign-in screen can offer for one-click access. */
	demoAccounts: () => api.get<DemoAccountDto[]>('/api/auth/demo-accounts'),
	me: () => api.get<LearnerMeDto>('/api/auth/me'),
	/** Update the signed-in learner's editable profile (name, avatar). */
	updateMe: (body: { name?: string; avatarUrl?: string }) =>
		api.put<{ id: string; name: string; email: string; role: string; avatarUrl: string; overallProgress: number }>(
			'/api/auth/me',
			body
		)
};

export const ssoApi = {
	authorize: (body: { clientId: string; redirectUri: string; codeChallenge: string; state: string }) =>
		api.post<{ redirectUrl: string }>('/api/sso/authorize', body),
	exchange: (body: { clientId: string; redirectUri: string; code: string; codeVerifier: string }) =>
		api.post<TokenResult>('/api/sso/token', body)
};

export const workspacesApi = {
	mine: () => api.get<Array<{
		id: string;
		name: string;
		slug: string;
		type: string;
		status: string;
		membershipId: string;
		role: string;
		state: string;
	}>>('/api/workspaces/mine'),
	createIndividualApplication: (body: {
		displayName: string;
		title?: string;
		bio?: string;
		evidenceJson: string;
	}) => api.post<{ id: string; status: string; workspaceId: string; slug: string }>(
		'/api/workspaces/applications/individual', body),
	createOrganizationApplication: (body: {
		displayName: string;
		title?: string;
		bio?: string;
		evidenceJson: string;
	}) => api.post<{ id: string; status: string; workspaceId: string; slug: string }>(
		'/api/workspaces/applications/organization', body),
	submitApplication: (id: string) => api.post<{ id: string; status: string }>(`/api/workspaces/applications/${id}/submit`),
	acceptInvitation: (token: string) => api.post<{ workspaceId: string }>('/api/workspaces/invitations/accept', { token }),
	members: (workspaceId: string) => api.get<{
		members: Array<{ id: string; accountId: string; role: string; state: string; name: string; email: string; avatarUrl?: string }>;
		pendingInvitations: Array<{ id: string; email: string; role: string; createdAt: string; expiresAt: string }>;
	}>(`/api/workspaces/${workspaceId}/members`),
	invite: (workspaceId: string, email: string, role: 'Admin' | 'Instructor') =>
		api.post(`/api/workspaces/${workspaceId}/invitations`, { email, role }),
	revokeMember: (workspaceId: string, membershipId: string) =>
		api.delete<void>(`/api/workspaces/${workspaceId}/members/${membershipId}`)
};

export const siteManagementApi = {
	saveEducatorProfile: (workspaceId: string, body: Record<string, unknown>) =>
		api.put(`/api/site-management/workspaces/${workspaceId}/educator-profile`, body),
	publishEducatorProfile: (workspaceId: string) =>
		api.post<void>(`/api/site-management/workspaces/${workspaceId}/educator-profile/publish`),
	saveOrganizationSite: (workspaceId: string, body: Record<string, unknown>) =>
		api.put(`/api/site-management/workspaces/${workspaceId}/organization-site`, body),
	saveOrganizationPage: (workspaceId: string, body: Record<string, unknown>) =>
		api.put(`/api/site-management/workspaces/${workspaceId}/organization-site/pages`, body),
	publishOrganizationSite: (workspaceId: string) =>
		api.post<void>(`/api/site-management/workspaces/${workspaceId}/organization-site/publish`),
	addDomain: (workspaceId: string, host: string) =>
		api.post(`/api/site-management/workspaces/${workspaceId}/domains`, { host }),
	addExternalWebsite: (workspaceId: string, url: string) =>
		api.post(`/api/site-management/workspaces/${workspaceId}/external-websites`, { url })
};

export const analyticsApi = {
	workspace: (workspaceId: string, from?: string, to?: string) =>
		api.get<any>(`/api/analytics/workspaces/${workspaceId}/overview`, { from, to })
};

export const communityApi = {
	reviews: (courseId: string) => api.get<any>(`/api/community/courses/${courseId}/reviews`),
	saveReview: (courseId: string, rating: number, body: string) =>
		api.put(`/api/community/courses/${courseId}/reviews/mine`, { rating, body }),
	reply: (reviewId: string, body: string) => api.put(`/api/community/reviews/${reviewId}/reply`, { body }),
	report: (reviewId: string, reason: string, detail = '') =>
		api.post(`/api/community/reviews/${reviewId}/reports`, { reason, detail }),
	follow: (workspaceId: string) => api.post<void>(`/api/community/workspaces/${workspaceId}/follow`),
	unfollow: (workspaceId: string) => api.delete<void>(`/api/community/workspaces/${workspaceId}/follow`)
};

export const assetsApi = {
	createUpload: (body: {
		workspaceId?: string;
		courseId?: string;
		purpose: 'CourseCover' | 'CourseMaterial' | 'AssignmentSubmission';
		fileName: string;
		contentType: string;
		sizeBytes: number;
		sha256: string;
	}) => api.post<{
		id: string;
		uploadSessionId: string;
		multipart: boolean;
		uploadUrl?: string;
		requiredHeaders?: Record<string, string>;
		partSizeBytes?: number;
		partCount?: number;
		expiresAt: string;
		status: string;
	}>('/api/assets/uploads', body),
	createPartUrl: (sessionId: string, partNumber: number) =>
		api.post<{ uploadUrl: string; requiredHeaders: Record<string, string>; expiresAt: string }>(`/api/assets/uploads/${sessionId}/parts/${partNumber}`),
	completeUpload: (sessionId: string, parts: Array<{ partNumber: number; eTag: string }> = []) =>
		api.post<{ id: string; status: string }>(`/api/assets/uploads/${sessionId}/complete`, { parts }),
	abortUpload: (sessionId: string) => api.delete<void>(`/api/assets/uploads/${sessionId}`),
	get: (id: string) => api.get<{
		id: string;
		originalFileName: string;
		contentType: string;
		sizeBytes: number;
		status: string;
		rejectionCode?: string;
	}>(`/api/assets/${id}`),
	attachToCourse: (assetId: string, courseId: string) => api.post<void>(`/api/assets/${assetId}/attach/course/${courseId}`)
};

export const releasesApi = {
	mine: (courseId: string) => api.get<any>(`/api/releases/course/${courseId}/mine`),
	get: (releaseId: string) => api.get<any>(`/api/releases/${releaseId}`),
	completeLesson: (releaseId: string, lessonId: string) =>
		api.post<{ progressPercentage: number }>(`/api/releases/${releaseId}/lessons/${lessonId}/complete`),
	submitQuiz: (releaseId: string, quizId: string, answers: number[]) =>
		api.post<{
			id: string;
			scorePercentage: number;
			isPassed: boolean;
			attemptsRemaining: number;
			explanations: Array<{ questionId: string; isCorrect: boolean; explanation: string }>;
		}>(`/api/releases/${releaseId}/quizzes/${quizId}/attempts`, { answers })
};

export const coursesApi = {
	/** Persisted enrollments for the signed-in learner. */
	listMine: () => api.get<CourseSummaryDto[]>('/api/courses'),
	getById: (id: string) => api.get<CourseDetailDto>(`/api/courses/${id}`),
	create: (body: {
		goal: string;
		hoursPerWeek?: number;
		preferredCreator?: string;
		materials?: string[];
		coverImageUrl?: string;
		/** When false, only the blueprint is saved; content is generated later per module. */
		generateFirstModule?: boolean;
	}) => api.post<CourseDetailDto>('/api/courses', body),
	marketplace: (params?: { category?: string; search?: string }) =>
		api.get<MarketplaceCourseDto[]>('/api/courses/marketplace', params),
	publish: (id: string) => api.post<{ isPublic: boolean }>(`/api/courses/${id}/publish`),
	share: (id: string) => api.post<{ shareCode: string }>(`/api/courses/${id}/share`),
	previewJoin: (code: string) => api.get<CourseInvitePreviewDto>(`/api/courses/join/${encodeURIComponent(code)}`),
	join: (code: string) => api.post<EnrollmentResultDto>(`/api/courses/join/${encodeURIComponent(code)}`),
	enroll: (id: string) => api.post<EnrollmentResultDto>(`/api/courses/${id}/enroll`),
	peerComparison: (id: string) => api.get<PeerComparisonDto>(`/api/courses/${id}/peer-comparison`),
	liveSessions: (id: string) => api.get<LiveSessionDto[]>(`/api/courses/${id}/live-sessions`),
	discussions: (id: string, lessonId?: string) =>
		api.get<CourseDiscussionDto[]>(`/api/courses/${id}/discussions`, lessonId ? { lessonId } : undefined),
	postDiscussion: (id: string, body: { lessonId?: string; title?: string; content: string }) =>
		api.post<CourseDiscussionDto>(`/api/courses/${id}/discussions`, body),
	postReply: (id: string, discussionId: string, body: { content: string }) =>
		api.post<{ id: string; content: string }>(`/api/courses/${id}/discussions/${discussionId}/reply`, body),
	assignments: (id: string) => api.get<AssignmentDto[]>(`/api/courses/${id}/assignments`),
	submitAssignment: (
		id: string,
		assignmentId: string,
		body: { submissionText: string; assetId?: string }
	) => api.post<AssignmentSubmissionDto>(`/api/courses/${id}/assignments/${assignmentId}/submit`, body)
};

export const lessonsApi = {
	get: (id: string) => api.get<{ id: string; title: string }>(`/api/lessons/${id}`),
	complete: (id: string) =>
		api.post<{ success: boolean; lessonId: string; isCompleted: boolean; courseId: string; progressPercentage: number }>(`/api/lessons/${id}/complete`)
};

export const modulesApi = {
	/** Just-in-time generate a module's lessons + quiz. */
	generate: (id: string) => api.post<unknown>(`/api/modules/${id}/generate`)
};

export const quizzesApi = {
	get: (id: string) => api.get<unknown>(`/api/quizzes/${id}`),
	/** Answers keyed by question GUID -> selected option index. */
	submit: (id: string, answers: Record<string, number>) =>
		api.post<QuizSubmissionResultDto>(`/api/quizzes/${id}/submit`, { answers })
};

export const learnerApi = {
	me: () => api.get<LearnerMeDto>('/api/auth/me'),
	profile: () => api.get<LearnerProfileDto>('/api/learners/me/profile'),
	updateProfile: (body: Partial<LearnerProfileDto>) =>
		api.put<LearnerProfileDto>('/api/learners/me/profile', body),
	state: () => api.get<{ activeStreakDays: number; totalHoursLearned: number; completedLessonsCount: number }>(
		'/api/learners/me/state'
	),
	/** The intake conversation that produces a learner profile. */
	startIntake: (learnerId: string) =>
		api.post<{ id: string; content: string }>(`/api/learners/${learnerId}/intake/sessions`),
	sendIntakeMessage: (learnerId: string, sessionId: string, message: string) =>
		api.post<{ reply: string }>(`/api/learners/${learnerId}/intake/sessions/${sessionId}/messages`, {
			message
		}),
	finalizeIntake: (learnerId: string, sessionId: string) =>
		api.post<LearnerProfileDto>(`/api/learners/${learnerId}/intake/sessions/${sessionId}/finalize`)
};

export const skillsApi = {
	forLearner: (learnerId: string) => api.get<LearnerSkillsResultDto>(`/api/skills/${learnerId}`),
	weakAreas: (learnerId: string) => api.get<LearnerSkillsResultDto['categories'][number]['skills']>(
		`/api/skills/${learnerId}/weak-areas`
	)
};

export const recommendationsApi = {
	list: () => api.get<RecommendationDto[]>('/api/recommendations')
};

export const activityApi = {
	recent: (limit = 20) => api.get<LearningActivityDto[]>('/api/activity', { limit })
};

export const progressApi = {
	/** Aggregated real per-learner progress used by the Progress page. */
	get: () => api.get<LearnerProgressDto>('/api/progress')
};

export const notificationsApi = {
	list: (unreadOnly = false) => api.get<NotificationDto[]>('/api/notifications', { unreadOnly }),
	markRead: (id: string) => api.post<{ success: boolean }>(`/api/notifications/${id}/read`),
	markAllRead: () => api.post<{ success: boolean }>('/api/notifications/read-all')
};

export const settingsApi = {
	get: () => api.get<LearnerSettingsDto>('/api/settings'),
	update: (body: Partial<LearnerSettingsDto>) => api.put<LearnerSettingsDto>('/api/settings', body)
};

export const searchApi = {
	query: (q: string) =>
		api.get<{ courses: unknown[]; lessons: unknown[] }>('/api/search', { q })
};

export const aiApi = {
	/** Model 1 (Learning Advisor): one conversational turn of the intake. */
	intakeMessage: (message: string, history: IntakeHistoryMessage[] = []) =>
		api.post<IntakeResponseDto>('/api/ai/intake/message', { message, history }),
	proposeCourse: (body: {
		goal: string;
		hoursPerWeek?: number;
		preferredCreator?: string;
		materials?: string[];
	}) => api.post<CoursePipelineProposalDto>('/api/ai/course/propose', body),
	tutorChat: (body: {
		message: string;
		history?: Array<{ role: 'user' | 'assistant'; content: string }>;
		courseId?: string;
		moduleId?: string;
		lessonId?: string;
	}) =>
		api.post<{
		  replyMarkdown: string;
		  suggestedFollowUps: string[];
		  codeSnippet: string | null;
		  language: string | null;
		}>('/api/ai/tutor/chat', body)
};

export const researchApi = {
	conduct: (topic: string, courseId?: string) =>
		api.get<ResearchPackageDto>('/api/research', { topic, courseId }),
	forCourse: (courseId: string) =>
		api.get<ResearchPackageDto>(`/api/research/course/${courseId}`)
};

export interface ResearchPackageDto {
	id: string;
	topic: string;
	summary: string;
	keyConceptsJson: string;
	createdAt: string;
	sources: Array<{
		id: string;
		title: string;
		url: string;
		sourceType: string;
		authors: string;
		snippet: string;
		publishedYear: number | null;
	}>;
}

export const healthApi = {
	check: () => api.get<{ status: string; service: string; version: string }>('/api/health')
};

/* ------------------------------------------------------------------ */
/* Educator Studio                                                     */
/* ------------------------------------------------------------------ */

export const educatorApi = {
	projects: () => api.get<EducatorProjectDto[]>('/api/educator/projects'),
	createProject: (body: { title: string; description: string }) =>
		api.post<EducatorProjectDto>('/api/educator/projects', body),
	project: (id: string) => api.get<EducatorProjectDetailDto>(`/api/educator/projects/${id}`),
	updateProject: (id: string, body: { title?: string; description?: string }) =>
		api.put<EducatorProjectDto>(`/api/educator/projects/${id}`, body),
	deleteProject: (id: string) => api.delete<void>(`/api/educator/projects/${id}`),

	addSource: (projectId: string, body: { url: string; type?: string; title?: string }) =>
		api.post<EducatorSourceDto>(`/api/educator/projects/${projectId}/sources`, body),
	deleteSource: (projectId: string, sourceId: string) =>
		api.delete<void>(`/api/educator/projects/${projectId}/sources/${sourceId}`),
	setSourceIncluded: (projectId: string, sourceId: string, included: boolean) =>
		api.patch<EducatorSourceDto>(`/api/educator/projects/${projectId}/sources/${sourceId}/include`, { included }),

	/** AI research suggestions — persisted server-side with addedBy:"Ai". */
	suggestSources: (projectId: string) =>
		api.post<{ suggestions: EducatorSourceDto[] }>(`/api/educator/projects/${projectId}/research/suggest`),

	analyze: (projectId: string) =>
		api.post<{ conflictCount: number; conflicts: EducatorConflictDto[] }>(
			`/api/educator/projects/${projectId}/analyze`
		),
	resolveConflict: (
		projectId: string,
		conflictId: string,
		body: { resolution: 'Keep' | 'Remove' | 'Merge' | 'ManualNote'; manualNote?: string }
	) => api.post<{ success: boolean; status: string }>(`/api/educator/projects/${projectId}/conflicts/${conflictId}/resolve`, body),

	generateBlueprint: (projectId: string) =>
		api.post<{ blueprintJson: string }>(`/api/educator/projects/${projectId}/blueprint/generate`),
	approveBlueprint: (projectId: string) =>
		api.post<{ courseId: string; jobs: EducatorProjectDetailDto['generationJobs'] }>(
			`/api/educator/projects/${projectId}/blueprint/approve`
		),
	generationStatus: (projectId: string) =>
		api.get<EducatorGenerationStatusDto>(`/api/educator/projects/${projectId}/status`),

	setCourseCover: (courseId: string, imageUrl: string) =>
		api.post<{ coverImageUrl: string }>(`/api/educator/courses/${courseId}/cover`, { imageUrl }),
	/** Without a body: returns candidates only. With imageUrl: applies the cover. */
	autoCourseCover: (courseId: string, imageUrl?: string) =>
		api.post<{ candidates?: Array<{ url: string; title: string }>; coverImageUrl?: string }>(
			`/api/educator/courses/${courseId}/cover/auto`,
			imageUrl ? { imageUrl } : undefined
		),

	updateModule: (
		courseId: string,
		moduleId: string,
		body: { title?: string; description?: string; lessons?: Array<{ id: string; title: string; content: string; order: number; readingTimeMinutes: number; youTubeVideoId?: string | null }> }
	) => api.put<unknown>(`/api/educator/courses/${courseId}/modules/${moduleId}`, body),
	aiUpdateModule: (courseId: string, moduleId: string, instruction: string) =>
		api.post<unknown>(`/api/educator/courses/${courseId}/modules/${moduleId}/ai-update`, { instruction }),

	publishCourse: (courseId: string, body: { isPaid: boolean; price?: number; currency?: string }) =>
		api.post<{ isPublished: boolean; isPublic: boolean; isPaid: boolean; price: number | null }>(
			`/api/educator/courses/${courseId}/publish`,
			body
		),
	unpublishCourse: (courseId: string) =>
		api.post<{ isPublished: boolean }>(`/api/educator/courses/${courseId}/unpublish`),
	reorderModules: (courseId: string, moduleIds: string[]) =>
		api.put<unknown>(`/api/educator/courses/${courseId}/modules/reorder`, { moduleIds }),
	duplicateModule: (courseId: string, moduleId: string) =>
		api.post<unknown>(`/api/educator/courses/${courseId}/modules/${moduleId}/duplicate`),
	duplicateLesson: (courseId: string, moduleId: string, lessonId: string) =>
		api.post<unknown>(`/api/educator/courses/${courseId}/modules/${moduleId}/lessons/${lessonId}/duplicate`),
	updateQuizzes: (courseId: string, moduleId: string, quizzes: QuizAuthoringDto[]) =>
		api.put<unknown>(`/api/educator/courses/${courseId}/modules/${moduleId}/quizzes`, { quizzes }),

	dashboard: () => api.get<EducatorDashboardDto>('/api/educator/dashboard'),
	enrollments: (status = 'PendingApproval') =>
		api.get<EducatorEnrollmentDto[]>('/api/educator/enrollments', { status }),
	approveEnrollment: (enrollmentId: string) =>
		api.post<unknown>(`/api/educator/enrollments/${enrollmentId}/approve`),
	rejectEnrollment: (enrollmentId: string) =>
		api.post<unknown>(`/api/educator/enrollments/${enrollmentId}/reject`),

	templates: () => api.get<CourseTemplateDto[]>('/api/educator/templates'),
	createManualCourse: (body: {
		title: string;
		description?: string;
		category?: string;
		targetAudience?: string;
		estimatedDuration?: string;
		templateId?: string;
		accentColor?: string;
		themeMode?: string;
		enrollmentMode?: string;
		level?: string;
		language?: string;
		learningObjectives?: string[];
		requirePassingQuizzes?: boolean;
		visibility?: CourseVisibility;
		modules?: Array<{
			title: string;
			summary: string;
			estimatedHours: number;
			lessons: Array<{ title: string; summary: string; contentMarkdown: string; readingTimeMinutes: number }>;
		}>;
	}) =>
		api.post<{ courseId: string; projectId: string; title: string; shareCode: string }>(
			'/api/educator/courses/manual',
			body
		),
	addModule: (courseId: string, body: { title: string; summary?: string; estimatedHours?: number }) =>
		api.post<{ moduleId: string; moduleNumber: number; title: string }>(
			`/api/educator/courses/${courseId}/modules`,
			body
		),
	deleteModule: (courseId: string, moduleId: string) =>
		api.delete<{ success: boolean }>(`/api/educator/courses/${courseId}/modules/${moduleId}`),
	addLesson: (
		courseId: string,
		moduleId: string,
		body: { title: string; summary?: string; contentMarkdown?: string; readingTimeMinutes?: number }
	) =>
		api.post<{ lessonId: string; lessonNumber: number; title: string }>(
			`/api/educator/courses/${courseId}/modules/${moduleId}/lessons`,
			body
		),
	deleteLesson: (courseId: string, moduleId: string, lessonId: string) =>
		api.delete<{ success: boolean }>(`/api/educator/courses/${courseId}/modules/${moduleId}/lessons/${lessonId}`),
	updateBranding: (
		courseId: string,
		body: {
			title?: string;
			description?: string;
			category?: string;
			targetAudience?: string;
			estimatedDuration?: string;
			accentColor?: string;
			themeMode?: string;
			enrollmentMode?: string;
			passingScorePercentage?: number;
			maxQuizAttempts?: number;
			prerequisites?: string;
			instructorBio?: string;
			instructorTitle?: string;
			level?: string;
			language?: string;
			learningObjectives?: string[];
			requirePassingQuizzes?: boolean;
			visibility?: CourseVisibility;
			enrollmentLimit?: number | null;
			enrollmentOpensAt?: string | null;
			enrollmentClosesAt?: string | null;
		}
	) => api.put<unknown>(`/api/educator/courses/${courseId}/branding`, body),
	liveSessions: (courseId: string) =>
		api.get<LiveSessionDto[]>(`/api/educator/courses/${courseId}/live-sessions`),
	createLiveSession: (
		courseId: string,
		body: {
			title: string;
			description?: string;
			startTime: string;
			durationMinutes?: number;
			timeZone?: string;
			meetingUrl?: string;
		}
	) => api.post<LiveSessionDto>(`/api/educator/courses/${courseId}/live-sessions`, body),
	updateLiveSession: (courseId: string, sessionId: string, body: Partial<LiveSessionDto>) =>
		api.put<LiveSessionDto>(`/api/educator/courses/${courseId}/live-sessions/${sessionId}`, body),
	deleteLiveSession: (courseId: string, sessionId: string) =>
		api.delete<LiveSessionDto>(`/api/educator/courses/${courseId}/live-sessions/${sessionId}`),
	assignments: (courseId: string) =>
		api.get<AssignmentDto[]>(`/api/educator/courses/${courseId}/assignments`),
	createAssignment: (
		courseId: string,
		body: {
			title: string;
			instructionsMarkdown?: string;
			dueDaysAfterEnrollment?: number;
			maxScore?: number;
			allowedFileTypes?: string;
		}
	) => api.post<AssignmentDto>(`/api/educator/courses/${courseId}/assignments`, body),
	assignmentSubmissions: (courseId: string, assignmentId: string) =>
		api.get<AssignmentSubmissionDto[]>(
			`/api/educator/courses/${courseId}/assignments/${assignmentId}/submissions`
		),
	gradeSubmission: (submissionId: string, body: { score: number; feedback?: string }) =>
		api.post<{ submissionId: string; status: string; score: number }>(
			`/api/educator/assignments/submissions/${submissionId}/grade`,
			body
		)
};

export const studioCoursesApi = {
	list: () => api.get<Array<{
		id: string;
		workspaceId: string;
		courseId: string;
		mode: string;
		state: string;
		updatedAt: string;
		version: number;
		title: string;
		description: string;
		slug?: string;
		coverImageUrl?: string;
		isPublished: boolean;
	}>>('/api/studio/courses'),
	get: (id: string) => api.get<{
		project: Record<string, unknown>;
		course: Record<string, unknown>;
		jobs: Array<Record<string, unknown>>;
	}>(`/api/studio/courses/${id}`),
	create: (body: {
		mode: 'Manual' | 'Prompt' | 'Materials' | 'Template' | 'Voice';
		workspaceId?: string;
		title?: string;
		description?: string;
		prompt?: string;
		category?: string;
		targetAudience?: string;
		learningObjectives?: string[];
		templateId?: string;
		sourceUrls?: string[];
	}) => api.post<{
		id: string;
		courseId: string;
		workspaceId: string;
		mode: string;
		state: string;
		title: string;
		slug: string;
	}>('/api/studio/courses', body),
	analyze: (id: string) => api.post<{ id: string; state: string; jobId: string }>(`/api/studio/courses/${id}/analyze`),
	approve: (id: string) => api.post<{ id: string; state: string; jobId: string }>(`/api/studio/courses/${id}/approve`),
	retry: (id: string) => api.post<{ id: string; state: string; jobId: string }>(`/api/studio/courses/${id}/retry`),
	attachAssetSource: (id: string, assetId: string) => api.post(`/api/studio/courses/${id}/sources/assets/${assetId}`)
};

/* ------------------------------------------------------------------ */
/* Payments and immutable offer snapshots                              */
/* ------------------------------------------------------------------ */

export const paymentsApi = {
	initialize: (courseId: string, redirectUrl: string, offerId?: string) =>
		api.post<PaymentInitializeDto>('/api/payments/initialize', {
			courseId,
			offerId,
			redirectUrl,
			idempotencyKey: typeof crypto !== 'undefined' && 'randomUUID' in crypto ? crypto.randomUUID() : `${courseId}-${Date.now()}`
		}),
	verify: (txRef: string) => api.get<PaymentVerifyDto>(`/api/payments/verify/${txRef}`)
};

export const offersApi = {
	active: (courseId: string) => api.get<any>(`/api/offers/course/${courseId}`),
	createVersion: (courseId: string, body: Record<string, unknown>) => api.post(`/api/offers/course/${courseId}`, body)
};
