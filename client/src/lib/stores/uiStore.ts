import { writable, get } from 'svelte/store';
import { browser } from '$app/environment';
import { preferences, updatePreferences, applyPreferences, type ThemeMode } from './preferencesStore';

/**
 * UI store — cross-component overlay state and theme.
 *
 * The Header triggers (bell, search, avatar) and the global ⌘K shortcut all
 * flip the same flags here, and the overlay components (NotificationCenter,
 * CommandPalette, LearnerProfileMenu) read them — so any trigger opens the
 * right panel from anywhere.
 *
 * Theme now lives in the preferences store (theme/accent/font/compact/motion);
 * this module re-exposes a light/dark view of it for the header toggle so the
 * two never drift apart.
 */
export const commandPaletteOpen = writable(false);
export const notificationsOpen = writable(false);
export const profileMenuOpen = writable(false);

export function openCommandPalette() {
  commandPaletteOpen.set(true);
}
export function toggleNotifications() {
  notificationsOpen.update((v) => !v);
}
export function toggleProfileMenu() {
  profileMenuOpen.update((v) => !v);
}

export type Theme = 'light' | 'dark';

/** The currently *resolved* light/dark value (system collapses to one of them). */
export const theme = writable<Theme>(
  browser ? (document.documentElement.classList.contains('dark') ? 'dark' : 'light') : 'light'
);

if (browser) {
  // Keep the light/dark view in sync with the full preferences store.
  preferences.subscribe(() => {
    const resolved: Theme = document.documentElement.classList.contains('dark') ? 'dark' : 'light';
    theme.set(resolved);
  });
}

/** Explicitly set the theme mode (light | dark | system) and re-apply. */
export function setTheme(value: ThemeMode) {
  updatePreferences({ theme: value });
}

/** Flip between light and dark from the header (system resolves first). */
export function toggleTheme() {
  const current: Theme = get(theme);
  updatePreferences({ theme: current === 'dark' ? 'light' : 'dark' });
}

/** Re-apply the stored preferences (safe to call before hydration). */
export function applyTheme(_value?: Theme) {
  applyPreferences();
}
