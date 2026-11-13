import { get, writable } from 'svelte/store';
import { ApiError, authApi, getAuthToken, setAuthToken, type LearnerMeDto } from '$lib/api';
import { setPreferenceScope } from './preferencesStore';

export interface SessionState {
	ready: boolean;
	offline: boolean;
	demo: boolean;
	authenticated: boolean;
	learnerId: string | null;
	name: string | null;
	email: string | null;
	role: string | null;
	accountType: 'Learner' | 'Educator' | null;
	educatorVerificationRequired: boolean;
	roles: string[];
	workspaces: LearnerMeDto['workspaces'];
	activeWorkspaceId: string | null;
	avatarUrl: string | null;
}

export interface RegistrationOutcome {
	session: SessionState | null;
	verificationRequired: boolean;
	message?: string;
}

export class MfaRequiredError extends Error {
	constructor(public readonly challengeToken: string) {
		super('Enter the code from your authenticator app or a recovery code.');
		this.name = 'MfaRequiredError';
	}
}

const initialState: SessionState = {
	ready: false,
	offline: false,
	demo: false,
	authenticated: false,
	learnerId: null,
	name: null,
	email: null,
	role: null,
	accountType: null,
	educatorVerificationRequired: false,
	roles: [],
	workspaces: [],
	activeWorkspaceId: null,
	avatarUrl: null
};

export const session = writable<SessionState>({ ...initialState });

let bootstrapPromise: Promise<SessionState> | null = null;
let authenticationVersion = 0;

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
	const legacyToken = getAuthToken();
	if (legacyToken) {
		try {
			await authApi.exchangeLegacy(sessionContext());
			setAuthToken(null);
		} catch (cause) {
			if (cause instanceof ApiError && [401, 403, 409].includes(cause.status)) {
				setAuthToken(null);
			} else {
				return apply({ ...initialState, ready: true, offline: true });
			}
		}
	}

	try {
		const me = await authApi.me();
		if (version !== authenticationVersion) return get(session);
		return applyMe(me, false);
	} catch (cause) {
		if (version !== authenticationVersion) return get(session);
		if (cause instanceof ApiError && (cause.status === 401 || cause.status === 403)) {
			return apply({ ...initialState, ready: true });
		}
		const current = get(session);
		if (current.authenticated) return apply({ ...current, ready: true, offline: true });
		return apply({ ...initialState, ready: true, offline: true });
	}
}

function apply(next: SessionState): SessionState {
	session.set(next);
	return next;
}

function applyMe(me: LearnerMeDto, demo: boolean): SessionState {
	setPreferenceScope(me.id);
	const workspaces = me.workspaces ?? [];
	let activeWorkspaceId = workspaces[0]?.id ?? null;
	if (typeof window !== 'undefined') {
		const selected = window.localStorage.getItem('witforge.activeWorkspace');
		if (selected && workspaces.some((workspace) => workspace.id === selected)) {
			activeWorkspaceId = selected;
		} else {
			if (workspaces[0]) window.localStorage.setItem('witforge.activeWorkspace', workspaces[0].id);
			else window.localStorage.removeItem('witforge.activeWorkspace');
		}
	}
	return apply({
		ready: true,
		offline: false,
		demo,
		authenticated: true,
		learnerId: me.id,
		name: me.name ?? null,
		email: me.email ?? null,
		role: me.role ?? null,
		accountType: me.accountType ?? (me.role === 'Educator' ? 'Educator' : 'Learner'),
		educatorVerificationRequired: me.educatorVerificationRequired ?? false,
		roles: me.roles ?? [me.role].filter(Boolean),
		workspaces,
		activeWorkspaceId,
		avatarUrl: me.avatarUrl ?? null
	});
}

export async function signIn(email: string, password: string): Promise<SessionState> {
	const version = ++authenticationVersion;
	const result = await authApi.login({ email, password });
	if (result.mfaRequired && result.challengeToken) throw new MfaRequiredError(result.challengeToken);
	setAuthToken(null);
	const me = await authApi.me();
	if (version !== authenticationVersion) return get(session);
	return applyMe(me, false);
}

export async function completeMfaSignIn(challengeToken: string, code: string): Promise<SessionState> {
	const version = ++authenticationVersion;
	await authApi.completeMfaLogin({ challengeToken, code });
	const me = await authApi.me();
	if (version !== authenticationVersion) return get(session);
	return applyMe(me, false);
}

export async function register(
	name: string,
	email: string,
	password: string,
	accountType: 'Learner' | 'Educator'
): Promise<RegistrationOutcome> {
	const version = ++authenticationVersion;
	const result = await authApi.register({ name, email, password, accountType });
	if (result.verificationRequired) {
		return {
			session: null,
			verificationRequired: true,
			message: result.message
		};
	}
	const me = await authApi.me();
	if (version !== authenticationVersion) return { session: get(session), verificationRequired: false };
	return { session: applyMe(me, false), verificationRequired: false };
}

export async function continueAsDemo(): Promise<SessionState> {
	const version = ++authenticationVersion;
	await authApi.demoLogin();
	const me = await authApi.me();
	if (version !== authenticationVersion) return get(session);
	return applyMe(me, true);
}

export function signOut(): void {
	authenticationVersion++;
	void authApi.logout().catch(() => undefined);
	setAuthToken(null);
	setPreferenceScope(null);
	bootstrapPromise = null;
	apply({ ...initialState, ready: true });
}

export function getLearnerId(): string | null {
	return get(session).learnerId;
}

export function isAuthenticated(): boolean {
	return get(session).authenticated;
}

export function setActiveWorkspace(workspaceId: string): void {
	const current = get(session);
	if (!current.workspaces?.some((workspace) => workspace.id === workspaceId)) {
		throw new Error('The selected workspace is not available in this session.');
	}
	if (typeof window !== 'undefined') window.localStorage.setItem('witforge.activeWorkspace', workspaceId);
	session.update((state) => ({ ...state, activeWorkspaceId: workspaceId }));
}

function sessionContext(): { host: string; site: 'App' } {
	return {
		host: typeof window === 'undefined' ? 'localhost' : window.location.hostname,
		site: 'App'
	};
}
