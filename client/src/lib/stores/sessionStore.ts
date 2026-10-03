import { writable, get } from 'svelte/store';
import { authApi, getAuthToken, setAuthToken, type LearnerMeDto } from '$lib/api';

export interface SessionState {
	/** True once the bootstrap attempt has finished (success, offline, or no auth). */
	ready: boolean;
	/** True when the API could not be reached, so stores should keep demo data. */
	offline: boolean;
	/** True when signed in with the seeded demo learner. */
	demo: boolean;
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
 * Establish a session exactly once per page load.
 *
 * - Reuses a stored token when `GET /api/auth/me` confirms it.
 * - Otherwise falls back to `POST /api/auth/demo-login`, which issues a token for
 *   the seeded demo learner and requires no credentials.
 * - If the API is unreachable, resolves as `offline` so stores can serve demo data.
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

	try {
		const token = await authApi.demoLogin();
		setAuthToken(token.accessToken);

		let me: LearnerMeDto | null = null;
		try {
			me = await authApi.me();
		} catch {
			/* profile enrichment is optional */
		}

		const next: SessionState = {
			ready: true,
			offline: false,
			demo: true,
			learnerId: token.learnerId,
			token: token.accessToken,
			name: token.name || me?.name || null,
			email: token.email || me?.email || null,
			role: token.role || me?.role || null,
			avatarUrl: me?.avatarUrl ?? null
		};
		session.set(next);
		return next;
	} catch {
		const next: SessionState = { ...initialState, ready: true, offline: true };
		session.set(next);
		return next;
	}
}

function applyMe(me: LearnerMeDto, token: string, demo: boolean): SessionState {
	const next: SessionState = {
		ready: true,
		offline: false,
		demo,
		learnerId: me.id,
		token,
		name: me.name ?? null,
		email: me.email ?? null,
		role: me.role ?? null,
		avatarUrl: me.avatarUrl ?? null
	};
	session.set(next);
	return next;
}

/** Convenience accessor for stores that need the active learner id. */
export function getLearnerId(): string | null {
	return get(session).learnerId;
}
