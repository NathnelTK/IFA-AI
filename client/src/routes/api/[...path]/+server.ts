import { createHmac, randomBytes, timingSafeEqual } from 'node:crypto';
import { env } from '$env/dynamic/private';
import { error, json, type RequestHandler } from '@sveltejs/kit';

const SESSION_COOKIE = '__Host-witforge-session';
const DEV_SESSION_COOKIE = 'witforge-session';
const LEGACY_COOKIE = '__Host-witforge-legacy';
const DEV_LEGACY_COOKIE = 'witforge-legacy';
const CSRF_COOKIE = 'witforge-csrf';
const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

type SiteClass = 'Auth' | 'App' | 'Public';

const APP_PREFIXES = new Set([
	'activity',
	'analytics',
	'assets',
	'ai',
	'auth',
	'certificates',
	'courses',
	'community',
	'educator',
	'intake',
	'learner',
	'learnerprofile',
	'lessons',
	'modules',
	'notifications',
	'offers',
	'operations',
	'payments',
	'platform-admin',
	'progress',
	'public-sites',
	'quizzes',
	'recommendations',
	'releases',
	'research',
	'search',
	'settings',
	'site-management',
	'skills',
	'sso',
	'studio',
	'voice',
	'workspaces'
]);

const AUTH_PATHS = new Set([
	'auth/register',
	'auth/login',
	'auth/login/mfa',
	'auth/logout',
	'auth/me',
	'auth/legacy-exchange',
	'auth/verify-email',
	'auth/resend-verification',
	'auth/forgot-password',
	'auth/reset-password',
	'auth/mfa/enroll',
	'auth/mfa/confirm',
	'auth/mfa/disable',
	'sso/authorize',
	'sso/token',
	'workspaces/invitations/accept'
]);

const PUBLIC_GET_PREFIXES = new Set(['auth', 'certificates', 'community', 'courses', 'offers', 'public-sites', 'search']);

export const GET: RequestHandler = proxy;
export const POST: RequestHandler = proxy;
export const PUT: RequestHandler = proxy;
export const PATCH: RequestHandler = proxy;
export const DELETE: RequestHandler = proxy;

async function proxy(event: Parameters<RequestHandler>[0]): Promise<Response> {
	const rawPath = event.params.path ?? '';
	const normalizedPath = normalizePath(rawPath);
	const site = classifyHost(event.url.hostname);
	if (!allowed(site, event.request.method, normalizedPath)) throw error(404, 'Not found');

	if (UNSAFE_METHODS.has(event.request.method)) {
		validateSameOrigin(event.request, event.url);
	}

	const dev = isLocalhost(event.url.hostname);
	const sessionCookieName = dev ? DEV_SESSION_COOKIE : SESSION_COOKIE;
	const legacyCookieName = dev ? DEV_LEGACY_COOKIE : LEGACY_COOKIE;
	const sessionToken = event.cookies.get(sessionCookieName) ?? '';
	const legacyToken = event.cookies.get(legacyCookieName) ?? '';
	const csrfCookie = event.cookies.get(CSRF_COOKIE) ?? '';
	if (sessionToken && UNSAFE_METHODS.has(event.request.method)) {
		const suppliedCsrf = event.request.headers.get('x-csrf-token') ?? '';
		if (!constantTimeEqual(csrfCookie, suppliedCsrf)) throw error(403, 'Invalid CSRF token');
	}

	const apiBase = (env.API_INTERNAL_URL || 'http://localhost:5011').replace(/\/+$/, '');
	const target = new URL(`${apiBase}/api/${normalizedPath}`);
	target.search = event.url.search;
	const headers = forwardedHeaders(event.request.headers);
	const workspace = safeWorkspace(event.request.headers.get('x-witforge-workspace'));
	headers.set('x-witforge-host', event.url.hostname);
	headers.set('x-witforge-site', site);
	const secret = env.BFF_INTERNAL_SECRET;
	if (sessionToken && (!secret || secret.length < 32)) throw error(503, 'The session gateway is not configured');
	if (secret && secret.length >= 32) {
		const timestamp = Math.floor(Date.now() / 1000).toString();
		const nonce = randomBytes(18).toString('base64url');
		let clientAddress = 'unknown';
		try {
			clientAddress = event.getClientAddress();
		} catch {
			clientAddress = event.request.headers.get('x-forwarded-for')?.split(',')[0]?.trim() || 'unknown';
		}
		const clientKey = createHmac('sha256', secret).update(`client:${clientAddress}`).digest('base64url');
		const pathAndQuery = `${target.pathname}${target.search}`;
		const canonical = [
			event.request.method.toUpperCase(),
			pathAndQuery,
			timestamp,
			nonce,
			event.url.hostname.toLowerCase(),
			site,
			workspace,
			clientKey,
			sessionToken
		].join('\n');
		if (sessionToken) headers.set('x-witforge-session', sessionToken);
		headers.set('x-witforge-client-key', clientKey);
		headers.set('x-witforge-timestamp', timestamp);
		headers.set('x-witforge-nonce', nonce);
		headers.set('x-witforge-signature', createHmac('sha256', secret).update(canonical).digest('base64url'));
		if (workspace) headers.set('x-witforge-workspace', workspace);
		if (csrfCookie && UNSAFE_METHODS.has(event.request.method)) headers.set('x-csrf-token', csrfCookie);
	}

	const legacyAuthorization = event.request.headers.get('authorization');
	if (!sessionToken && legacyToken) {
		headers.set('authorization', `Bearer ${legacyToken}`);
	} else if (normalizedPath === 'auth/legacy-exchange' && legacyAuthorization?.startsWith('Bearer ')) {
		headers.set('authorization', legacyAuthorization);
	}

	const body = event.request.method === 'GET' || event.request.method === 'HEAD'
		? undefined
		: await event.request.arrayBuffer();
	let upstream: Response;
	try {
		upstream = await fetch(target, {
			method: event.request.method,
			headers,
			body,
			redirect: 'manual'
		});
	} catch {
		throw error(502, 'The Witforge API is unavailable');
	}

	const responseText = await upstream.text();
	if (upstream.ok && isSessionIssuingPath(normalizedPath) && responseText) {
		const payload = JSON.parse(responseText) as Record<string, unknown>;
		if (typeof payload.sessionToken === 'string' && typeof payload.csrfToken === 'string') {
			setSessionCookies(event, payload.sessionToken, payload.csrfToken, dev);
			event.cookies.delete(legacyCookieName, { path: '/' });
			delete payload.sessionToken;
			delete payload.csrfToken;
			return json(payload, { status: upstream.status, headers: safeResponseHeaders(upstream.headers) });
		}
		if (typeof payload.accessToken === 'string') {
			setLegacyCookie(event, payload.accessToken, dev);
			delete payload.accessToken;
			delete payload.refreshToken;
			return json(payload, { status: upstream.status, headers: safeResponseHeaders(upstream.headers) });
		}
	}
	if (upstream.ok && normalizedPath === 'auth/logout') {
		event.cookies.delete(sessionCookieName, { path: '/' });
		event.cookies.delete(legacyCookieName, { path: '/' });
		event.cookies.delete(CSRF_COOKIE, { path: '/' });
	}

	return new Response(responseText || null, {
		status: upstream.status,
		headers: safeResponseHeaders(upstream.headers)
	});
}

function setLegacyCookie(event: Parameters<RequestHandler>[0], token: string, dev: boolean): void {
	event.cookies.set(dev ? DEV_LEGACY_COOKIE : LEGACY_COOKIE, token, {
		path: '/',
		httpOnly: true,
		secure: !dev,
		sameSite: 'lax',
		maxAge: 60 * 60 * 24 * 7
	});
}

function classifyHost(hostname: string): SiteClass {
	const host = hostname.toLowerCase();
	if (isLocalhost(host)) return 'App';
	if (host.endsWith('.vercel.app')) return 'App';
	if (host === (env.AUTH_HOST || 'auth.witforge.com').toLowerCase()) return 'Auth';
	if (host === (env.APP_HOST || 'app.witforge.com').toLowerCase()) return 'App';
	const publicHosts = (env.PUBLIC_HOSTS || 'witforge.com,www.witforge.com')
		.split(',')
		.map((value) => value.trim().toLowerCase());
	if (publicHosts.includes(host)) return 'Public';
	return 'Public';
}

function allowed(site: SiteClass, method: string, path: string): boolean {
	if (site === 'Auth') return AUTH_PATHS.has(path);
	const prefix = path.split('/')[0];
	if (site === 'App') return APP_PREFIXES.has(prefix);
	return (method === 'GET' && PUBLIC_GET_PREFIXES.has(prefix)) || (method === 'POST' && path === 'sso/token');
}

function normalizePath(value: string): string {
	const decoded = decodeURIComponent(value).replace(/^\/+|\/+$/g, '');
	if (!decoded || decoded.includes('..') || decoded.includes('\\')) throw error(404, 'Not found');
	return decoded;
}

function validateSameOrigin(request: Request, url: URL): void {
	const origin = request.headers.get('origin');
	if (!origin) return;
	let parsed: URL;
	try {
		parsed = new URL(origin);
	} catch {
		throw error(403, 'Invalid request origin');
	}
	if (parsed.host !== url.host || parsed.protocol !== url.protocol) throw error(403, 'Cross-origin request denied');
}

function forwardedHeaders(source: Headers): Headers {
	const headers = new Headers();
	for (const name of ['accept', 'content-type', 'if-none-match', 'if-match']) {
		const value = source.get(name);
		if (value) headers.set(name, value);
	}
	return headers;
}

function safeResponseHeaders(source: Headers): Headers {
	const headers = new Headers();
	for (const name of ['content-type', 'cache-control', 'etag', 'location', 'retry-after', 'x-correlation-id']) {
		const value = source.get(name);
		if (value) headers.set(name, value);
	}
	return headers;
}

function setSessionCookies(
	event: Parameters<RequestHandler>[0],
	sessionToken: string,
	csrfToken: string,
	dev: boolean
): void {
	event.cookies.set(dev ? DEV_SESSION_COOKIE : SESSION_COOKIE, sessionToken, {
		path: '/',
		httpOnly: true,
		secure: !dev,
		sameSite: 'lax',
		maxAge: 60 * 60 * 12
	});
	event.cookies.set(CSRF_COOKIE, csrfToken, {
		path: '/',
		httpOnly: false,
		secure: !dev,
		sameSite: 'lax',
		maxAge: 60 * 60 * 12
	});
}

function constantTimeEqual(left: string, right: string): boolean {
	const a = Buffer.from(left);
	const b = Buffer.from(right);
	return a.length > 0 && a.length === b.length && timingSafeEqual(a, b);
}

function safeWorkspace(value: string | null): string {
	return value && /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value)
		? value
		: '';
}

function isSessionIssuingPath(path: string): boolean {
	return path === 'auth/register'
		|| path === 'auth/login'
		|| path === 'auth/login/mfa'
		|| path === 'auth/legacy-exchange'
		|| path === 'auth/demo-login'
		|| path === 'sso/token';
}

function isLocalhost(hostname: string): boolean {
	return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '::1';
}
