import { expect, test } from './fixtures';

test.describe('responsive layouts', () => {
	test('landing page keeps its editorial layout usable on mobile', async ({ anonymousPage }) => {
		await anonymousPage.setViewportSize({ width: 390, height: 844 });
		await anonymousPage.goto('/');
		await expect(
			anonymousPage.getByRole('heading', { level: 1, name: /Turn what you know into what people can become/ })
		).toBeVisible();
		await anonymousPage.getByRole('button', { name: 'Toggle navigation' }).click();
		await expect(anonymousPage.locator('#mobile-nav').getByRole('link', { name: 'Create an account' })).toBeVisible();
		const overflows = await anonymousPage.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1);
		expect(overflows).toBe(false);
	});

	test('learner workspace exposes an accessible mobile drawer', async ({ learnerPage }) => {
		await learnerPage.setViewportSize({ width: 390, height: 844 });
		await learnerPage.goto('/app');
		await learnerPage.getByRole('button', { name: 'Open navigation' }).click();
		await expect(learnerPage.getByRole('complementary', { name: 'Learner navigation' })).toBeVisible();
		await learnerPage.getByRole('complementary', { name: 'Learner navigation' })
			.getByRole('button', { name: 'Close navigation' }).click();
		const overflows = await learnerPage.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1);
		expect(overflows).toBe(false);
	});
});
