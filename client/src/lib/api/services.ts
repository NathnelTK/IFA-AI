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
	TokenResult
} from './types';

/**
 * Domain-grouped API calls. Every function maps 1:1 to a backend route so the
 * request path stays traceable from the UI back to a controller/endpoint.
 */

export const authApi = {
	register: (body: { name: string; email: string; password: string; role?: 'Learner' | 'Educator' }) =>
		api.post<TokenResult>('/api/auth/register', body),
	login: (body: { email: string; password: string }) =>
		api.post<TokenResult>('/api/auth/login', body),
	/** No credentials required — issues a token for the seeded demo learner. */
	demoLogin: () => api.post<TokenResult>('/api/auth/demo-login'),
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
		body: { submissionText: string; fileUrl?: string; fileName?: string }
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

/* ------------------------------------------------------------------ */
/* Payments (Telebirr / Chapa via Chapa checkout)                      */
/* ------------------------------------------------------------------ */

export const paymentsApi = {
	initialize: (courseId: string, redirectUrl: string) =>
		api.post<PaymentInitializeDto>('/api/payments/initialize', { courseId, redirectUrl }),
	verify: (txRef: string) => api.get<PaymentVerifyDto>(`/api/payments/verify/${txRef}`)
};
