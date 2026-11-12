import { env } from '$env/dynamic/private';
import { error } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, url, fetch }) => {
	const code = url.searchParams.get('code') ?? '';
	const apiBase = (env.API_INTERNAL_URL || 'http://localhost:5011').replace(/\/+$/, '');
	const response = await fetch(`${apiBase}/api/certificates/verify/${encodeURIComponent(params.serial)}?code=${encodeURIComponent(code)}`);
	if (!response.ok) throw error(404, 'Certificate not found');
	return { certificate: await response.json() };
};
