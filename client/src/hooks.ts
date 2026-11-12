import type { Reroute } from '@sveltejs/kit';

const firstPartyHosts = new Set([
	'localhost',
	'127.0.0.1',
	'witforge.com',
	'www.witforge.com',
	'app.witforge.com',
	'auth.witforge.com'
]);

export const reroute: Reroute = ({ url }) => {
	const host = url.hostname.toLowerCase();
	if (firstPartyHosts.has(host) || host.endsWith('.vercel.app')) return url.pathname;
	if (url.pathname === '/auth/callback') return url.pathname;
	if (url.pathname.startsWith('/_app')
		|| url.pathname.startsWith('/api')
		|| url.pathname.startsWith('/_site')
		|| url.pathname === '/favicon.ico'
		|| url.pathname === '/robots.txt') return url.pathname;
	return `/_site${url.pathname === '/' ? '' : url.pathname}`;
};
