import { API_BASE_URL } from './config';

/**
 * Thin fetch wrapper for the IFA API.
 *
 * Calls the same-origin SvelteKit BFF. Legacy bearer attachment remains only for
 * the one-time migration endpoint and is removed after exchange.
 */
export class ApiError extends Error {
	constructor(
		public readonly status: number,
		message: string,
		public readonly body?: unknown,
		public readonly retryAfterSeconds?: number
	) {
		super(message);
		this.name = 'ApiError';
	}
}

/** Include server validation details rather than hiding them behind a generic title. */
export function apiErrorMessage(cause: unknown, fallback = 'The request failed.'): string {
	const message = cause instanceof Error ? cause.message : fallback;
	if (cause instanceof ApiError && cause.status === 429) {
		const wait = cause.retryAfterSeconds ? ` Try again in ${cause.retryAfterSeconds} seconds.` : ' Try again shortly.';
		return `Too many requests.${wait}`;
	}
	if (!(cause instanceof ApiError) || !cause.body || typeof cause.body !== 'object' || !('errors' in cause.body)) return message;
	const errors = cause.body.errors;
	const details = Array.isArray(errors)
		? errors
		: errors && typeof errors === 'object' ? Object.values(errors).flat() : [];
	return [message, ...details.filter((detail): detail is string => typeof detail === 'string')].join('\n');
}

const TOKEN_STORAGE_KEY = 'ifa.accessToken';
let inMemoryToken: string | null = null;

export function setAuthToken(token: string | null): void {
	inMemoryToken = token;
	if (typeof localStorage === 'undefined') return;
	if (token) localStorage.setItem(TOKEN_STORAGE_KEY, token);
	else localStorage.removeItem(TOKEN_STORAGE_KEY);
}

export function getAuthToken(): string | null {
	if (inMemoryToken) return inMemoryToken;
	if (typeof localStorage !== 'undefined') {
		inMemoryToken = localStorage.getItem(TOKEN_STORAGE_KEY);
	}
	return inMemoryToken;
}

type QueryValue = string | number | boolean | null | undefined;

export interface RequestOptions {
	method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
	body?: unknown;
	query?: Record<string, QueryValue>;
	signal?: AbortSignal;
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
	const origin = API_BASE_URL || (typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173');
	const url = new URL(path.startsWith('/') ? path : `/${path}`, origin);
	if (query) {
		for (const [key, value] of Object.entries(query)) {
			if (value !== undefined && value !== null) url.searchParams.set(key, String(value));
		}
	}
	return url.toString();
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
	const headers: Record<string, string> = { Accept: 'application/json' };
	if (options.body !== undefined) headers['Content-Type'] = 'application/json';
	if (typeof window !== 'undefined') {
		const activeWorkspace = window.localStorage.getItem('witforge.activeWorkspace');
		if (activeWorkspace) headers['X-Witforge-Workspace'] = activeWorkspace;
	}
	if (options.method && options.method !== 'GET') {
		const csrf = readCookie('witforge-csrf');
		if (csrf) headers['X-CSRF-Token'] = csrf;
	}

	const token = getAuthToken();
	if (token && path.includes('/auth/legacy-exchange')) headers.Authorization = `Bearer ${token}`;

	let response: Response;
	try {
		response = await fetch(buildUrl(path, options.query), {
			method: options.method ?? 'GET',
			headers,
			body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
			signal: options.signal,
			credentials: 'same-origin'
		});
	} catch (cause) {
		throw new ApiError(0, `Cannot reach the Witforge API at ${API_BASE_URL}.`, cause);
	}

	if (!response.ok) {
		const raw = await response.text().catch(() => '');
		let body: unknown = raw;
		try {
			body = raw ? JSON.parse(raw) : undefined;
		} catch {
			/* keep the raw text */
		}
		const payload = body && typeof body === 'object' ? body as Record<string, unknown> : undefined;
		const detail = typeof payload?.message === 'string'
			? payload.message
			: typeof payload?.detail === 'string'
				? payload.detail
				: typeof payload?.title === 'string'
					? payload.title
					: response.statusText;
		const migrationInfo = payload?.code === 'database_migrations_pending'
			? [
				Array.isArray(payload.pendingMigrations)
					? `Pending migrations: ${payload.pendingMigrations.filter((item): item is string => typeof item === 'string').join(', ')}`
					: undefined,
				typeof payload.command === 'string' ? `Run: ${payload.command}` : undefined
			].filter((item): item is string => Boolean(item))
			: [];
		const retryAfterSeconds = parseRetryAfter(response.headers.get('retry-after'), payload?.retryAfterSeconds);
		throw new ApiError(
			response.status,
			[detail, ...migrationInfo].filter(Boolean).join('\n') || `Request to ${path} failed.`,
			body,
			retryAfterSeconds
		);
	}

	if (response.status === 204) return undefined as T;

	const text = await response.text();
	return (text ? JSON.parse(text) : undefined) as T;
}

function parseRetryAfter(header: string | null, bodyValue: unknown): number | undefined {
	if (typeof bodyValue === 'number' && Number.isFinite(bodyValue) && bodyValue > 0) {
		return Math.ceil(bodyValue);
	}
	if (!header) return undefined;
	const seconds = Number(header);
	if (Number.isFinite(seconds) && seconds > 0) return Math.ceil(seconds);
	const date = Date.parse(header);
	if (Number.isNaN(date)) return undefined;
	return Math.max(1, Math.ceil((date - Date.now()) / 1000));
}

function readCookie(name: string): string | null {
	if (typeof document === 'undefined') return null;
	const prefix = `${encodeURIComponent(name)}=`;
	for (const entry of document.cookie.split(';')) {
		const value = entry.trim();
		if (value.startsWith(prefix)) return decodeURIComponent(value.slice(prefix.length));
	}
	return null;
}

export const api = {
	get: <T>(path: string, query?: Record<string, QueryValue>, signal?: AbortSignal) =>
		apiRequest<T>(path, { method: 'GET', query, signal }),
	post: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: 'POST', body }),
	put: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: 'PUT', body }),
	patch: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: 'PATCH', body }),
	delete: <T>(path: string) => apiRequest<T>(path, { method: 'DELETE' })
};
