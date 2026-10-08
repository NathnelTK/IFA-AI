import { writable, get } from 'svelte/store';
import { browser } from '$app/environment';

/**
 * Appearance preferences store — the single source of truth for theme,
 * accent color, font size, compact mode and reduced motion.
 *
 * The Settings page edits these and they are applied immediately to the
 * <html> element (so every page reacts without a reload), mirrored to
 * localStorage so a refresh keeps them, and persisted to the backend
 * (`/api/settings`) by the Settings page on save.
 *
 * `system` theme follows the OS `prefers-color-scheme`; the resolved value is
 * applied as the `.dark` class on <html>.
 */
export type ThemeMode = 'light' | 'dark' | 'system';
export type AccentColor = 'pine' | 'blue' | 'purple' | 'orange';
export type FontSize = 'small' | 'medium' | 'large' | 'extra-large';

export interface Preferences {
	theme: ThemeMode;
	accentColor: AccentColor;
	fontSize: FontSize;
	compactMode: boolean;
	reducedMotion: boolean;
}

export const ACCENT_HEX: Record<AccentColor, string> = {
	pine: '#2A9D68',
	blue: '#3B82F6',
	purple: '#7C5CFC',
	orange: '#F59E0B'
};

/** Root font-size scale driven by the font-size preference. */
const FONT_SCALE: Record<FontSize, string> = {
	small: '15px',
	medium: '16px',
	large: '17.5px',
	'extra-large': '19px'
};

const STORAGE_KEY = 'ifa-preferences';

const defaults: Preferences = {
	theme: 'system',
	accentColor: 'pine',
	fontSize: 'medium',
	compactMode: false,
	reducedMotion: false
};

function readInitial(): Preferences {
	if (!browser) return { ...defaults };
	try {
		const raw = localStorage.getItem(STORAGE_KEY);
		if (!raw) return { ...defaults };
		const parsed = JSON.parse(raw) as Partial<Preferences>;
		return { ...defaults, ...parsed };
	} catch {
		return { ...defaults };
	}
}

export const preferences = writable<Preferences>(readInitial());

/** True when the OS asks for reduced motion, independent of the user toggle. */
export function prefersReducedMotion(): boolean {
	if (!browser) return false;
	return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

function resolveTheme(mode: ThemeMode): 'light' | 'dark' {
	if (mode === 'system') {
		if (!browser) return 'light';
		return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
	}
	return mode;
}

/** Push the current preferences onto <html> (classes, data attrs, CSS vars). */
export function applyPreferences(value: Preferences = get(preferences)): void {
	if (!browser) return;
	const root = document.documentElement;

	const resolved = resolveTheme(value.theme);
	root.classList.toggle('dark', resolved === 'dark');
	root.dataset.theme = value.theme;
	root.dataset.resolvedTheme = resolved;

	root.dataset.fontSize = value.fontSize;
	root.style.setProperty('--ifa-font-scale', FONT_SCALE[value.fontSize] ?? FONT_SCALE.medium);

	root.dataset.density = value.compactMode ? 'compact' : 'comfortable';

	// Effective reduced motion = explicit toggle OR the OS preference.
	const reduced = value.reducedMotion || prefersReducedMotion();
	root.dataset.reducedMotion = reduced ? 'true' : 'false';

	root.style.setProperty('--ifa-accent', ACCENT_HEX[value.accentColor] ?? ACCENT_HEX.pine);
	root.dataset.accent = value.accentColor;

	try {
		localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
	} catch {
		/* storage may be unavailable (private mode) — apply in-memory only */
	}
}

/** Merge a partial update, persist it, and re-apply to the document. */
export function updatePreferences(patch: Partial<Preferences>): void {
	preferences.update((current) => {
		const next = { ...current, ...patch };
		applyPreferences(next);
		return next;
	});
}

/** Replace all preferences (used when hydrating from the backend). */
export function hydratePreferences(patch: Partial<Preferences>): void {
	preferences.update((current) => {
		const next = { ...current, ...patch };
		applyPreferences(next);
		return next;
	});
}

let mediaBound = false;

/**
 * Keep `system` in sync with the OS. Bound once per page load; re-resolves the
 * applied theme whenever the color-scheme preference changes.
 */
export function watchSystemTheme(): () => void {
	if (!browser) return () => {};
	const mq = window.matchMedia('(prefers-color-scheme: dark)');
	const onChange = () => {
		if (get(preferences).theme === 'system') applyPreferences();
	};
	mq.addEventListener('change', onChange);
	mediaBound = true;
	return () => mq.removeEventListener('change', onChange);
}

/** Apply-once helper for the earliest client moment (layout onMount). */
export function initPreferences(): void {
	applyPreferences();
}
