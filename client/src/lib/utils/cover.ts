/**
 * Deterministic, dependency-free course cover art.
 *
 * Seeded courses ship a placeholder `ThumbnailUrl` of "/ifa.png" (which is not
 * actually served) and freshly generated courses may have no cover at all.
 * Rather than render a broken <img>, we synthesise a pleasant gradient cover
 * with the course initials as an inline SVG data-URI, keyed off the title so the
 * same course always gets the same art.
 */

const PALETTES: ReadonlyArray<readonly [string, string]> = [
	['#1B3D2F', '#2A9D68'],
	['#0F766E', '#22D3EE'],
	['#4338CA', '#7C5CFC'],
	['#B45309', '#F59E0B'],
	['#9D174D', '#F472B6'],
	['#1E3A8A', '#3B82F6'],
	['#3F6212', '#84CC16'],
	['#7C2D12', '#FB7185']
];

function hash(input: string): number {
	let h = 0;
	for (let i = 0; i < input.length; i++) {
		h = (h << 5) - h + input.charCodeAt(i);
		h |= 0;
	}
	return Math.abs(h);
}

function initials(title: string): string {
	const words = title.trim().split(/\s+/).filter(Boolean);
	if (words.length === 0) return 'IFA';
	if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
	return (words[0][0] + words[1][0]).toUpperCase();
}

/** An inline SVG data-URI gradient cover derived from a course title. */
export function courseCover(title: string): string {
	const t = title && title.trim() ? title : 'IFA Course';
	const [from, to] = PALETTES[hash(t) % PALETTES.length];
	const label = initials(t);
	const angle = hash(t) % 360;
	const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="360" viewBox="0 0 640 360">
  <defs>
    <linearGradient id="g" gradientTransform="rotate(${angle})">
      <stop offset="0%" stop-color="${from}"/>
      <stop offset="100%" stop-color="${to}"/>
    </linearGradient>
  </defs>
  <rect width="640" height="360" fill="url(#g)"/>
  <circle cx="540" cy="70" r="150" fill="#ffffff" opacity="0.08"/>
  <circle cx="90" cy="320" r="120" fill="#000000" opacity="0.08"/>
  <text x="50%" y="52%" dominant-baseline="central" text-anchor="middle" font-family="'Segoe UI',system-ui,sans-serif" font-size="150" font-weight="700" fill="#ffffff" fill-opacity="0.92">${label}</text>
</svg>`;
	return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

/**
 * True when a thumbnail string is a real, loadable image URL — not empty and not
 * the unserved "/ifa.png" placeholder the seeders emit.
 */
export function hasRealCover(url?: string | null): boolean {
	if (!url) return false;
	const u = url.trim();
	if (!u || u === '/ifa.png') return false;
	return u.startsWith('http://') || u.startsWith('https://') || u.startsWith('data:');
}

/** The best cover to show: the real one when present, otherwise generated art. */
export function resolveCover(url: string | null | undefined, title: string): string {
	return hasRealCover(url) ? (url as string) : courseCover(title);
}
