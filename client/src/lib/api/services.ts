import { api } from './client';
import type {
	CourseDetailDto,
	CourseSummaryDto,
	LearnerMeDto,
	LearnerProfileDto,
	LearnerSettingsDto,
	LearnerSkillsResultDto,
	LearningActivityDto,
	MarketplaceCourseDto,
	NotificationDto,
	PeerComparisonDto,
	RecommendationDto,
	TokenResult
} from './types';

/**
 * Domain-grouped API calls. Every function maps 1:1 to a backend route so the
 * request path stays traceable from the UI back to a controller/endpoint.
 */

export const authApi = {
	register: (body: { name: string; email: string; password: string }) =>
		api.post<TokenResult>('/api/auth/register', body),
	login: (body: { email: string; password: string }) =>
		api.post<TokenResult>('/api/auth/login', body),
	/** No credentials required — issues a token for the seeded demo learner. */
	demoLogin: () => api.post<TokenResult>('/api/auth/demo-login'),
	me: () => api.get<LearnerMeDto>('/api/auth/me')
};

export const coursesApi = {
	/** Enrolled courses for the signed-in learner (auto-seeds one if empty). */
	listMine: () => api.get<CourseSummaryDto[]>('/api/courses'),
	getById: (id: string) => api.get<CourseDetailDto>(`/api/courses/${id}`),
	create: (body: { goal: string; hoursPerWeek?: number; preferredCreator?: string }) =>
		api.post<CourseDetailDto>('/api/courses', body),
	marketplace: (params?: { category?: string; search?: string }) =>
		api.get<MarketplaceCourseDto[]>('/api/courses/marketplace', params),
	publish: (id: string) => api.post<{ isPublic: boolean }>(`/api/courses/${id}/publish`),
	share: (id: string) => api.post<{ shareCode: string }>(`/api/courses/${id}/share`),
	previewJoin: (code: string) => api.get<MarketplaceCourseDto>(`/api/courses/join/${code}`),
	join: (code: string) => api.post<{ success: boolean; courseId?: string }>(`/api/courses/join/${code}`),
	peerComparison: (id: string) => api.get<PeerComparisonDto>(`/api/courses/${id}/peer-comparison`)
};

export const lessonsApi = {
	get: (id: string) => api.get<{ id: string; title: string }>(`/api/lessons/${id}`),
	complete: (id: string) =>
		api.post<{ success: boolean; lessonId: string; isCompleted: boolean }>(`/api/lessons/${id}/complete`)
};

export const modulesApi = {
	/** Just-in-time generate a module's lessons + quiz. */
	generate: (id: string) => api.post<unknown>(`/api/modules/${id}/generate`)
};

export const quizzesApi = {
	get: (id: string) => api.get<unknown>(`/api/quizzes/${id}`),
	/** Answers keyed by question GUID -> selected option index. */
	submit: (id: string, answers: Record<string, number>) =>
		api.post<{ scorePercentage?: number; isPassed?: boolean }>(`/api/quizzes/${id}/submit`, { answers })
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
	intakeMessage: (message: string, history: unknown[] = []) =>
		api.post<unknown>('/api/ai/intake/message', { message, history }),
	proposeCourse: (body: { goal: string; hoursPerWeek?: number; preferredCreator?: string }) =>
		api.post<unknown>('/api/ai/course/propose', body),
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
