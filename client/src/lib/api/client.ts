import { API_BASE_URL } from './config';

/**
 * Thin fetch wrapper for the IFA API.
 *
 * Handles JSON encoding/decoding, bearer-token attachment, and turns non-2xx
 * responses into a typed `ApiError`. The token is kept in memory and mirrored to
 * localStorage so a page reload keeps the learner signed in.
 */
export class ApiError extends Error {
	constructor(
		public readonly status: number,
		message: string,
		public readonly body?: unknown
	) {
		super(message);
		this.name = 'ApiError';
	}
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
	method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
	body?: unknown;
	query?: Record<string, QueryValue>;
	signal?: AbortSignal;
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
	const url = new URL(`${API_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`);
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

	const token = getAuthToken();
	if (token) headers.Authorization = `Bearer ${token}`;

	let response: Response;
	try {
		response = await fetch(buildUrl(path, options.query), {
			method: options.method ?? 'GET',
			headers,
			body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
			signal: options.signal
		});
	} catch (cause) {
		throw new ApiError(0, `Cannot reach the IFA API at ${API_BASE_URL}.`, cause);
	}

	if (!response.ok) {
		const raw = await response.text().catch(() => '');
		let body: unknown = raw;
		try {
			body = raw ? JSON.parse(raw) : undefined;
		} catch {
			/* keep the raw text */
		}
		const detail =
			(body && typeof body === 'object' && 'message' in body && typeof body.message === 'string'
				? body.message
				: undefined) ??
			(body && typeof body === 'object' && 'title' in body && typeof body.title === 'string'
				? body.title
				: undefined) ??
			response.statusText;
		throw new ApiError(response.status, detail || `Request to ${path} failed.`, body);
	}

	if (response.status === 204) return undefined as T;

	const text = await response.text();
	return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
	get: <T>(path: string, query?: Record<string, QueryValue>, signal?: AbortSignal) =>
		apiRequest<T>(path, { method: 'GET', query, signal }),
	post: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: 'POST', body }),
	put: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: 'PUT', body })
};
