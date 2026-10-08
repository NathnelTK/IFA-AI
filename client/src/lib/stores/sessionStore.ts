import { writable, get } from 'svelte/store';
import { authApi, getAuthToken, setAuthToken, type LearnerMeDto } from '$lib/api';

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

/**
 * Establish a session exactly once per page load by validating any stored token
 * against `GET /api/auth/me`. If there is no token the session resolves as
 * unauthenticated, and the app shell redirects to `/login`.
 */
export function initSession(): Promise<SessionState> {
	if (!bootstrapPromise) bootstrapPromise = bootstrap();
	return bootstrapPromise;
}

async function bootstrap(): Promise<SessionState> {
	const storedToken = getAuthToken();
	if (storedToken) {
		try {
			const me = await authApi.me();
			return applyMe(me, storedToken, false);
		} catch {
			setAuthToken(null);
		}
	}

	return apply({ ...initialState, ready: true, offline: false, authenticated: false });
}

function apply(next: SessionState): SessionState {
	session.set(next);
	return next;
}

function applyMe(me: LearnerMeDto, token: string, demo: boolean): SessionState {
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

/** Sign in with email + password and hydrate the session. */
export async function signIn(email: string, password: string): Promise<SessionState> {
	const result = await authApi.login({ email, password });
	setAuthToken(result.accessToken);
	const me = await authApi.me();
	return applyMe(me, result.accessToken, false);
}

/** Register a new learner and hydrate the session. */
export async function register(name: string, email: string, password: string): Promise<SessionState> {
	const result = await authApi.register({ name, email, password });
	setAuthToken(result.accessToken);
	const me = await authApi.me();
	return applyMe(me, result.accessToken, false);
}

/** One-click access to the seeded demo learner. */
export async function continueAsDemo(): Promise<SessionState> {
	const token = await authApi.demoLogin();
	setAuthToken(token.accessToken);
	try {
		const me = await authApi.me();
		return applyMe(me, token.accessToken, true);
	} catch {
		return apply({
			ready: true,
			offline: false,
			demo: true,
			authenticated: true,
			learnerId: token.learnerId,
			token: token.accessToken,
			name: token.name || null,
			email: token.email || null,
			role: token.role || null,
			avatarUrl: null
		});
	}
}

/** Clear the session; the layout will route back to `/login`. */
export function signOut(): void {
	setAuthToken(null);
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
