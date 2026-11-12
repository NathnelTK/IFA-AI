import { env } from '$env/dynamic/private';
import { error } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ request, params, fetch }) => {
	const host = (request.headers.get('host') ?? '').split(':')[0].toLowerCase();
	const path = params.path ? `/${params.path}` : '/';
	const apiBase = (env.API_INTERNAL_URL || 'http://localhost:5011').replace(/\/+$/, '');
	const response = await fetch(`${apiBase}/api/public-sites/resolve?host=${encodeURIComponent(host)}&path=${encodeURIComponent(path)}`);
	if (!response.ok) throw error(404, 'Site not found');
	return { site: await response.json() };
};
