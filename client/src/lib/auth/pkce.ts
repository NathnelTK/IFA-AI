import { env } from '$env/dynamic/public';

const STORAGE_KEY = 'witforge.sso.pkce';

interface PkceState {
	clientId: string;
	redirectUri: string;
	verifier: string;
	state: string;
	returnTo: string;
}

export async function beginCentralSignIn(returnTo = '/dashboard'): Promise<void> {
	if (typeof window === 'undefined') return;
	const host = window.location.hostname.toLowerCase();
	const clientId = host === 'app.witforge.com' || host === 'localhost' || host === '127.0.0.1'
		? 'witforge-app'
		: 'witforge-public-site';
	const redirectUri = `${window.location.origin}/auth/callback`;
	const verifier = randomValue(32);
	const state = randomValue(24);
	const challengeBytes = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
	const challenge = base64Url(new Uint8Array(challengeBytes));
	const saved: PkceState = { clientId, redirectUri, verifier, state, returnTo: safeReturnTo(returnTo) };
	window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(saved));

	const authOrigin = (env.PUBLIC_AUTH_ORIGIN || 'https://auth.witforge.com').replace(/\/+$/, '');
	const target = new URL('/sso', authOrigin);
	target.searchParams.set('client_id', clientId);
	target.searchParams.set('redirect_uri', redirectUri);
	target.searchParams.set('code_challenge', challenge);
	target.searchParams.set('state', state);
	window.location.assign(target.toString());
}

export function consumePkceState(state: string): PkceState | null {
	if (typeof window === 'undefined') return null;
	const raw = window.sessionStorage.getItem(STORAGE_KEY);
	window.sessionStorage.removeItem(STORAGE_KEY);
	if (!raw) return null;
	try {
		const saved = JSON.parse(raw) as PkceState;
		return saved.state === state ? saved : null;
	} catch {
		return null;
	}
}

function randomValue(length: number): string {
	const bytes = new Uint8Array(length);
	crypto.getRandomValues(bytes);
	return base64Url(bytes);
}

function base64Url(bytes: Uint8Array): string {
	let binary = '';
	for (const byte of bytes) binary += String.fromCharCode(byte);
	return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

function safeReturnTo(value: string): string {
	return value.startsWith('/') && !value.startsWith('//') ? value : '/dashboard';
}
