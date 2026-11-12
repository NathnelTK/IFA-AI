import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { expect, test } from './fixtures';

async function expectNoWcagViolations(page: Page) {
	await page.waitForLoadState('networkidle');
	const results = await new AxeBuilder({ page })
		.withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
		.analyze();
	const violations = results.violations.map(({ id, impact, nodes }) => ({
		id,
		impact,
		targets: nodes.map((node) => node.target.join(' '))
	}));

	expect(violations).toEqual([]);
}

test.describe('representative accessibility checks', () => {
	test('sign-in page has no automated WCAG A/AA violations', async ({ anonymousPage }) => {
		await anonymousPage.goto('/login');
		await expect(anonymousPage.getByRole('heading', { name: 'Welcome back.', exact: true })).toBeVisible();

		await expectNoWcagViolations(anonymousPage);
	});

	test('learner dashboard has no automated WCAG A/AA violations', async ({ learnerPage }) => {
		await learnerPage.goto('/app');
		await expect(learnerPage.getByRole('heading', { name: /Make the next hour count/ })).toBeVisible();

		await expectNoWcagViolations(learnerPage);
	});

	test('educator studio has no automated WCAG A/AA violations', async ({ educatorPage }) => {
		await educatorPage.goto('/studio/courses');
		await expect(educatorPage.getByRole('heading', { level: 1, name: 'Courses' })).toBeVisible();

		await expectNoWcagViolations(educatorPage);
	});
});
