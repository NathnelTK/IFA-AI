import { existsSync } from 'node:fs';
import type { Page, TestInfo } from '@playwright/test';
import { expect, test } from './fixtures';

function skipWhenBaselineIsMissing(testInfo: TestInfo, name: string) {
	const baselineExists = existsSync(testInfo.snapshotPath(name, { kind: 'screenshot' }));
	const updatingSnapshots = testInfo.config.updateSnapshots !== 'none';
	test.skip(
		!baselineExists && !updatingSnapshots,
		`No visual baseline for ${name}; generate it with npm run test:e2e:visual:update.`
	);
}

async function waitForFonts(page: Page) {
	await page.evaluate(async () => {
		await document.fonts.ready;
	});
}

test.describe('visual baselines', () => {
	test('landing hero', async ({ anonymousPage }, testInfo) => {
		skipWhenBaselineIsMissing(testInfo, 'landing-hero.png');
		await anonymousPage.goto('/');
		await expect(
			anonymousPage.getByRole('heading', { level: 1, name: /Turn what you know into what people can become/ })
		).toBeVisible();
		await waitForFonts(anonymousPage);

		await expect(anonymousPage).toHaveScreenshot('landing-hero.png', {
			animations: 'disabled',
			caret: 'hide',
			fullPage: false,
			maxDiffPixelRatio: 0.01,
			scale: 'css'
		});
	});

	test('learner dashboard', async ({ learnerPage }, testInfo) => {
		skipWhenBaselineIsMissing(testInfo, 'learner-dashboard.png');
		await learnerPage.goto('/app');
		await expect(learnerPage.getByRole('heading', { name: /Make the next hour count/ })).toBeVisible();
		await learnerPage.waitForLoadState('networkidle');
		await waitForFonts(learnerPage);

		await expect(learnerPage).toHaveScreenshot('learner-dashboard.png', {
			animations: 'disabled',
			caret: 'hide',
			fullPage: false,
			maxDiffPixelRatio: 0.01,
			scale: 'css'
		});
	});
});
