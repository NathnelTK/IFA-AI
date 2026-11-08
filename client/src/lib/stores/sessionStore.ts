import { writable, get } from 'svelte/store';
import { ApiError, authApi, getAuthToken, setAuthToken, type LearnerMeDto, type TokenResult } from '$lib/api';
import { setPreferenceScope } from './preferencesStore';

export interface SessionState {
	/** True once the bootstrap attempt has finished. */
	ready: boolean;
	/** True when the API could not be reached. */
	offline: boolean;
	/** True when signed in as a seeded demo learner. */
	demo: boolean;
	/** True when a valid token backs this session. */
	authenticated: boolean;
	learnerId: string | null;
	token: string | null;
	name: string | null;
	email: string | null;
	role: string | null;
	avatarUrl: string | null;
}

const initialState: SessionState = {
	ready: false,
	offline: false,
	demo: false,
	authenticated: false,
	learnerId: null,
	token: null,
	name: null,
	email: null,
	role: null,
	avatarUrl: null
};

export const session = writable<SessionState>({ ...initialState });

let bootstrapPromise: Promise<SessionState> | null = null;
let authenticationVersion = 0;

/**
 * Establish a session exactly once per page load by validating any stored token
 * against `GET /api/auth/me`. If there is no token the session resolves as
 * unauthenticated, and the app shell redirects to `/login`.
 */
export function initSession(): Promise<SessionState> {
	if (!bootstrapPromise) bootstrapPromise = bootstrap();
	return bootstrapPromise;
}

export function refreshSession(): Promise<SessionState> {
	bootstrapPromise = null;
	return initSession();
}

async function bootstrap(): Promise<SessionState> {
	const version = authenticationVersion;
	const storedToken = getAuthToken();
	if (storedToken) {
		try {
			const me = await authApi.me();
			if (version !== authenticationVersion || getAuthToken() !== storedToken) return get(session);
			return applyMe(me, storedToken, false);
		} catch (cause) {
			if (cause instanceof ApiError && (cause.status === 401 || cause.status === 403)) {
				if (version === authenticationVersion && getAuthToken() === storedToken) setAuthToken(null);
			} else {
				if (version !== authenticationVersion || getAuthToken() !== storedToken) return get(session);
				const current = get(session);
				if (current.authenticated && current.token === storedToken) {
					return apply({ ...current, ready: true, offline: true });
				}
				return apply({ ...initialState, ready: true, offline: true, token: storedToken });
			}
		}
	}

	return apply({ ...initialState, ready: true, offline: false, authenticated: false });
}

function apply(next: SessionState): SessionState {
	session.set(next);
	return next;
}

function applyMe(me: LearnerMeDto, token: string, demo: boolean): SessionState {
	setPreferenceScope(me.id);
	return apply({
		ready: true,
		offline: false,
		demo,
		authenticated: true,
		learnerId: me.id,
		token,
		name: me.name ?? null,
		email: me.email ?? null,
		role: me.role ?? null,
		avatarUrl: me.avatarUrl ?? null
	});
}

function applyToken(result: TokenResult, demo: boolean): SessionState {
	setPreferenceScope(result.learnerId);
	return apply({
		ready: true,
		offline: false,
		demo,
		authenticated: true,
		learnerId: result.learnerId,
		token: result.accessToken,
		name: result.name || null,
		email: result.email || null,
		role: result.role || null,
		avatarUrl: null
	});
}

/** Sign in with email + password and hydrate the session. */
export async function signIn(email: string, password: string): Promise<SessionState> {
	const version = ++authenticationVersion;
	const result = await authApi.login({ email, password });
	if (version !== authenticationVersion) return get(session);
	setAuthToken(result.accessToken);
	return applyToken(result, false);
}

/** Register a new learner or educator and hydrate the session. */
export async function register(
	name: string,
	email: string,
	password: string,
	role: 'Learner' | 'Educator' = 'Learner'
): Promise<SessionState> {
	const version = ++authenticationVersion;
	const result = await authApi.register({ name, email, password, role });
	if (version !== authenticationVersion) return get(session);
	setAuthToken(result.accessToken);
	return applyToken(result, false);
}

/** One-click access to the seeded demo learner. */
export async function continueAsDemo(): Promise<SessionState> {
	const version = ++authenticationVersion;
	const token = await authApi.demoLogin();
	if (version !== authenticationVersion) return get(session);
	setAuthToken(token.accessToken);
	return applyToken(token, true);
}

/** Clear the session; the layout will route back to `/login`. */
export function signOut(): void {
	authenticationVersion++;
	setAuthToken(null);
	setPreferenceScope(null);
	bootstrapPromise = null;
	apply({ ...initialState, ready: true });
}

/** Convenience accessor for stores that need the active learner id. */
export function getLearnerId(): string | null {
	return get(session).learnerId;
}

/** True once the user is signed in. */
export function isAuthenticated(): boolean {
	return get(session).authenticated;
}
