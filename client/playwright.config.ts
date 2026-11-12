import { defineConfig } from '@playwright/test';

const baseURL = 'http://127.0.0.1:4173';

export default defineConfig({
	testDir: './tests',
	outputDir: './test-results',
	fullyParallel: false,
	forbidOnly: Boolean(process.env.CI),
	retries: process.env.CI ? 2 : 0,
	workers: 1,
	timeout: 30_000,
	expect: {
		timeout: 5_000
	},
	reporter: process.env.CI
		? [['line'], ['html', { open: 'never' }]]
		: [['list'], ['html', { open: 'never' }]],
	updateSnapshots: 'none',
	snapshotPathTemplate: '{testDir}/visual-snapshots/{projectName}/{arg}{ext}',
	use: {
		baseURL,
		browserName: 'chromium',
		channel: 'chromium',
		viewport: { width: 1440, height: 900 },
		deviceScaleFactor: 1,
		colorScheme: 'light',
		locale: 'en-US',
		timezoneId: 'UTC',
		reducedMotion: 'reduce',
		serviceWorkers: 'block',
		permissions: [],
		trace: 'retain-on-failure',
		screenshot: 'only-on-failure',
		video: 'off'
	},
	webServer: {
		command: 'npm run build && npm run serve:e2e',
		url: baseURL,
		reuseExistingServer: !process.env.CI,
		timeout: 240_000,
		env: {
			...process.env,
			API_INTERNAL_URL: 'http://127.0.0.1:9',
			BFF_INTERNAL_SECRET: 'e2e-only-secret-that-is-longer-than-thirty-two-characters',
			PUBLIC_AUTH_ORIGIN: baseURL
		}
	},
	projects: [
		{
			name: 'smoke',
			testMatch: /smoke\.spec\.ts/
		},
		{
			name: 'a11y',
			testMatch: /a11y\.spec\.ts/
		},
		{
			name: 'responsive',
			testMatch: /responsive\.spec\.ts/
		},
		{
			name: 'visual',
			testMatch: /visual\.spec\.ts/
		}
	]
});
