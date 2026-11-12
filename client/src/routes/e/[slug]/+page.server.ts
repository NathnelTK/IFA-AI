import { env } from '$env/dynamic/private';
import { error, redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, fetch }) => {
	const apiBase = (env.API_INTERNAL_URL || 'http://localhost:5011').replace(/\/+$/, '');
	const response = await fetch(`${apiBase}/api/public-sites/educators/${encodeURIComponent(params.slug)}`, { redirect: 'manual' });
	if (response.status === 301 || response.status === 308) throw redirect(308, response.headers.get('location') ?? '/');
	if (!response.ok) throw error(404, 'Educator not found');
	return { profile: await response.json() };
};
