import { expect, test as base, type Page } from '@playwright/test';

export const APP_ORIGIN = 'http://127.0.0.1:4173';
export const CANVAS_COURSE_ID = '11111111-1111-1111-1111-111111111111';

const API_PREFIX = '/__e2e_api__';
const FIXED_NOW = new Date('2026-01-15T10:00:00.000Z');
const TEST_PREFERENCES = {
	theme: 'light',
	accentColor: 'pine',
	fontSize: 'medium',
	compactMode: false,
	reducedMotion: true
};

type Role = 'anonymous' | 'Learner' | 'Educator';

interface RoleIdentity {
	learnerId: string;
	token: string;
	name: string;
	email: string;
	role: 'Learner' | 'Educator';
}

interface MockResponse {
	status?: number;
	body: unknown;
	headers?: Record<string, string>;
}

interface RoleFixtures {
	anonymousPage: Page;
	learnerPage: Page;
	educatorPage: Page;
}

const identities: Record<'Learner' | 'Educator', RoleIdentity> = {
	Learner: {
		learnerId: 'learner-e2e',
		token: 'e2e-learner-token',
		name: 'Ada Learner',
		email: 'ada.learner@example.test',
		role: 'Learner'
	},
	Educator: {
		learnerId: 'educator-e2e',
		token: 'e2e-educator-token',
		name: 'Ada Educator',
		email: 'ada.educator@example.test',
		role: 'Educator'
	}
};

function learnerMe(identity: RoleIdentity) {
	return {
		id: identity.learnerId,
		name: identity.name,
		email: identity.email,
		role: identity.role,
		roles: [identity.role],
		workspaces: identity.role === 'Educator'
			? [
					{
						id: '00000000-0000-4000-8000-000000000001',
						name: 'E2E Studio',
						slug: 'e2e-studio',
						type: 'Educator',
						role: 'Owner'
					}
				]
			: [],
		avatarUrl: '',
		overallProgress: 0,
		profile: null,
		state: null,
		settings: null
	};
}

function settings(identity: RoleIdentity) {
	return {
		id: `settings-${identity.learnerId}`,
		learnerId: identity.learnerId,
		dailyReminders: false,
		weeklyDigest: false,
		assessmentResults: false,
		courseRecommendations: false,
		theme: 'light',
		accentColor: 'pine',
		fontSize: 'medium',
		compactMode: false,
		reducedMotion: true,
		preferredYouTubeChannelsJson: '[]',
		excludedYouTubeChannelsJson: '[]',
		preferredTopicsJson: '[]'
	};
}

function responsesFor(role: Role): Map<string, MockResponse> {
	if (role === 'anonymous') {
		return new Map([
			['GET /api/auth/me', { status: 401, body: { message: 'Unauthorized' } }],
			[
				'POST /api/auth/login',
				{
					status: 429,
					headers: { 'Retry-After': '30' },
					body: {
						code: 'rate_limited',
						message: 'Try again in 30 seconds.',
						retryAfterSeconds: 30
					}
				}
			],
			[
				'GET /api/auth/demo-accounts',
				{
					body: [
						{
							name: 'Demo Learner',
							email: 'learner@example.test',
							role: 'Learner',
							avatarUrl: ''
						},
						{
							name: 'Demo Educator',
							email: 'educator@example.test',
							role: 'Educator',
							avatarUrl: ''
						}
					]
				}
			]
		]);
	}

	const identity = identities[role];
	const common: Array<[string, MockResponse]> = [
		['GET /api/auth/me', { body: learnerMe(identity) }],
		['GET /api/settings', { body: settings(identity) }],
		['GET /api/notifications?unreadOnly=true', { body: [] }],
		['GET /api/courses', { body: [] }],
		[`GET /api/skills/${identity.learnerId}`, { body: { categories: [], overallImprovement: 0 } }],
		['GET /api/recommendations', { body: [] }],
		['GET /api/activity?limit=6', { body: [] }],
		['GET /api/courses/marketplace', { body: [] }],
		['GET /api/studio/courses?workspaceId=00000000-0000-4000-8000-000000000001', { body: [] }],
		[`GET /api/courses/${CANVAS_COURSE_ID}`, { body: { id: CANVAS_COURSE_ID, title: 'Evidence-led Learning', description: 'A course about credible evidence.', modules: [], isPublished: false, appearance: null } }],
		[`PUT /api/educator/courses/${CANVAS_COURSE_ID}/appearance`, { body: { appearance: { canvasLayout: 'Presentation', primaryColor: '#176B4A', secondaryColor: '#C7F464', backgroundColor: '#F7F5EF', surfaceColor: '#FFFFFF', textColor: '#252525', headingFont: 'Manrope', bodyFont: 'Inter', contentWidth: 'Standard', spacing: 'Comfortable', cardStyle: 'Rounded', buttonStyle: 'Solid', borderStyle: 'Subtle' }, version: 1, updatedAt: FIXED_NOW.toISOString() } }],
		[`DELETE /api/educator/courses/${CANVAS_COURSE_ID}/appearance`, { body: null }]
	];

	if (role === 'Learner') {
		return new Map(common);
	}

	return new Map([
		...common,
		['GET /api/studio/courses', { body: [] }]
	]);
}

function apiKey(method: string, url: URL): string {
	const query = new URLSearchParams(
		[...url.searchParams.entries()].sort(([leftKey, leftValue], [rightKey, rightValue]) =>
			leftKey === rightKey ? leftValue.localeCompare(rightValue) : leftKey.localeCompare(rightKey)
		)
	).toString();
	const path = url.pathname.startsWith(`${API_PREFIX}/`)
		? url.pathname.slice(API_PREFIX.length)
		: url.pathname;
	return `${method.toUpperCase()} ${path}${query ? `?${query}` : ''}`;
}

function roleFixture(role: Role) {
	return async ({ page }: { page: Page }, use: (page: Page) => Promise<void>) => {
		const identity = role === 'anonymous' ? null : identities[role];
		const mocks = responsesFor(role);
		const unexpectedRequests: string[] = [];

		await page.context().addInitScript(
			({ identity, preferences }) => {
				localStorage.clear();
				localStorage.setItem('witforge-preferences:device', JSON.stringify(preferences));
				if (identity) {
					localStorage.setItem(
						`witforge-preferences:${identity.learnerId}`,
						JSON.stringify(preferences)
					);
				}
			},
			{ identity, preferences: TEST_PREFERENCES }
		);
		if (identity) {
			await page.context().addCookies([
				{
					name: 'witforge-session',
					value: identity.token,
					url: APP_ORIGIN,
					httpOnly: true,
					sameSite: 'Lax'
				},
				{
					name: 'witforge-csrf',
					value: 'e2e-csrf-token',
					url: APP_ORIGIN,
					sameSite: 'Lax'
				}
			]);
		}

		await page.clock.setFixedTime(FIXED_NOW);
		await page.context().route('**/*', async (route) => {
			const request = route.request();
			const url = new URL(request.url());

			if (url.hostname === 'fonts.googleapis.com') {
				await route.fulfill({
					status: 200,
					contentType: 'text/css',
					body: '/* External fonts are intentionally disabled in browser tests. */'
				});
				return;
			}

			if (url.origin !== APP_ORIGIN) {
				unexpectedRequests.push(`NETWORK ${request.method()} ${url.toString()}`);
				await route.abort('blockedbyclient');
				return;
			}

			if (url.pathname.startsWith(`${API_PREFIX}/`) || url.pathname.startsWith('/api/')) {
				const key = apiKey(request.method(), url);
				const mock = mocks.get(key);

				if (!mock) {
					unexpectedRequests.push(key);
					await route.fulfill({
						status: 500,
						contentType: 'application/json',
						body: JSON.stringify({ message: `Unexpected browser API request: ${key}` })
					});
					return;
				}

				await route.fulfill({
					status: mock.status ?? 200,
					contentType: 'application/json',
					headers: mock.headers,
					body: JSON.stringify(mock.body)
				});
				return;
			}

			await route.continue();
		});

		await use(page);

		expect(
			unexpectedRequests,
			'Every browser API call must be registered in tests/fixtures.ts; external network is blocked.'
		).toEqual([]);
	};
}

export const test = base.extend<RoleFixtures>({
	anonymousPage: roleFixture('anonymous'),
	learnerPage: roleFixture('Learner'),
	educatorPage: roleFixture('Educator')
});

export { expect };
