import { writable } from 'svelte/store';
import { browser } from '$app/environment';

/**
 * UI store — cross-component overlay state and theme.
 *
 * The Header triggers (bell, search, avatar) and the global ⌘K shortcut all
 * flip the same flags here, and the overlay components (NotificationCenter,
 * CommandPalette, LearnerProfileMenu) read them — so any trigger opens the
 * right panel from anywhere.
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

function readInitialTheme(): Theme {
  if (!browser) return 'light';
  const stored = localStorage.getItem('ifa-theme');
  if (stored === 'light' || stored === 'dark') return stored;
  return 'light'; // Default to light mode
}

export const theme = writable<Theme>(readInitialTheme());

/** Apply the theme to <html> and persist it. Safe to call before hydration. */
export function applyTheme(value: Theme) {
  if (!browser) return;
  document.documentElement.classList.toggle('dark', value === 'dark');
  localStorage.setItem('ifa-theme', value);
}

export function toggleTheme() {
  theme.update((current) => {
    const next: Theme = current === 'dark' ? 'light' : 'dark';
    applyTheme(next);
    return next;
  });
}

export function setTheme(value: Theme) {
  theme.set(value);
  applyTheme(value);
}
