/**
 * Backend API configuration.
 *
 * `VITE_API_URL` is read from the build/dev environment (see client/.env.example).
 * Vite inlines every `VITE_*` variable into the browser bundle, so this must only
 * ever hold a public base URL — never a provider key or a JWT secret. Those live in
 * the repository-root `.env` and are read by the backend only.
 *
 * Defaults to the API launch profile in src/IFA.API/Properties/launchSettings.json.
 */
const DEFAULT_API_URL = 'http://localhost:5011';

function normalizeBaseUrl(url: string | undefined): string {
	const trimmed = url?.trim();
	return trimmed ? trimmed.replace(/\/+$/, '') : DEFAULT_API_URL;
}

export const API_BASE_URL = normalizeBaseUrl(import.meta.env.VITE_API_URL);
