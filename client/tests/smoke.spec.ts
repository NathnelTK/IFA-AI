import { APP_ORIGIN, expect, test } from './fixtures';

test.describe('frontend smoke', () => {
	test('anonymous visitor can open the landing page and sign-in screen', async ({ anonymousPage }) => {
		await anonymousPage.goto('/');
		await expect(anonymousPage).toHaveTitle(/Witforge · Build, teach and learn with proof/);
		await expect(
			anonymousPage.getByRole('heading', { level: 1, name: /Turn what you know into what people can become/ })
		).toBeVisible();

		await anonymousPage
			.getByRole('navigation')
			.getByRole('link', { name: 'Sign in', exact: true })
			.click();
		await expect(anonymousPage).toHaveURL(`${APP_ORIGIN}/login`);
		await expect(anonymousPage.getByRole('heading', { name: 'Welcome back.', exact: true })).toBeVisible();
	});

	test('anonymous visitor is redirected away from a protected route', async ({ anonymousPage }) => {
		await anonymousPage.goto('/app');

		await expect(anonymousPage).toHaveURL(/\/login\?redirect=%2Fapp$/);
		await expect(anonymousPage.getByRole('heading', { name: 'Welcome back.', exact: true })).toBeVisible();
	});

	test('registration validates fields live and offers educator onboarding', async ({ anonymousPage }) => {
		await anonymousPage.goto('/register');
		await anonymousPage.getByText('Teach', { exact: true }).click();
		await expect(anonymousPage.getByText(/continues to educator verification/)).toBeVisible();

		const name = anonymousPage.getByLabel('Full name');
		await name.focus();
		await name.blur();
		await expect(anonymousPage.getByText('Enter your full name.')).toBeVisible();
		await name.fill('Ada Educator');
		await expect(anonymousPage.getByText('Enter your full name.')).toHaveCount(0);

		const email = anonymousPage.getByLabel('Email', { exact: true });
		await email.fill('ada@invalid');
		await email.blur();
		await expect(anonymousPage.getByText(/complete email address/)).toBeVisible();

		await anonymousPage.getByLabel('Password', { exact: true }).fill('short');
		await anonymousPage.getByLabel('Password', { exact: true }).blur();
		await expect(anonymousPage.getByText('Use at least 8 characters.')).toBeVisible();

		await anonymousPage.getByLabel('Confirm password').fill('different');
		await anonymousPage.getByLabel('Confirm password').blur();
		await expect(anonymousPage.getByText('The passwords do not match.')).toBeVisible();
	});

	test('sign-in displays the server retry window after rate limiting', async ({ anonymousPage }) => {
		await anonymousPage.goto('/login');
		await anonymousPage.getByLabel('Email address').fill('ada@example.test');
		await anonymousPage.getByLabel('Password').fill('valid-password');
		await anonymousPage.getByRole('button', { name: 'Sign in' }).click();

		await expect(anonymousPage.getByRole('alert')).toContainText('Too many requests. Try again in 30 seconds.');
		await expect(anonymousPage.getByRole('button', { name: /Try again in 30s/ })).toBeDisabled();
	});

	test('learner fixture opens the learner dashboard', async ({ learnerPage }) => {
		await learnerPage.goto('/app');

		await expect(learnerPage).toHaveURL(`${APP_ORIGIN}/app`);
		await expect(learnerPage.getByRole('heading', { name: /Make the next hour count/ })).toBeVisible();
		await expect(learnerPage.getByRole('link', { name: 'My courses' })).toBeVisible();
		await expect(learnerPage.getByRole('complementary', { name: 'Studio navigation' })).toHaveCount(0);
	});

	test('educator fixture opens the educator studio', async ({ educatorPage }) => {
		await educatorPage.goto('/studio/courses');

		await expect(educatorPage).toHaveURL(`${APP_ORIGIN}/studio/courses`);
		await expect(educatorPage.getByRole('heading', { level: 1, name: /Courses are the center of gravity/ })).toBeVisible();
		await expect(educatorPage.getByText('No courses match')).toBeVisible();
		await expect(educatorPage.getByRole('link', { name: 'Courses', exact: true })).toBeVisible();
	});
});
