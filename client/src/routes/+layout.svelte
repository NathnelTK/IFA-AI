<script lang="ts">
  import '../app.css';
  import '@fontsource-variable/manrope';
  import '@fontsource-variable/newsreader';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { browser } from '$app/environment';
  import WorkspaceShell from '$lib/components/shells/WorkspaceShell.svelte';
  import PublicShell from '$lib/components/shells/PublicShell.svelte';
  import { initSession, session } from '$lib/stores/sessionStore';
  import { loadCourses, resetCourses } from '$lib/stores/coursesStore';
  import { loadDashboard, resetDashboard } from '$lib/stores/dashboardStore';
  import { hydratePreferences, initPreferences, watchSystemTheme, type ThemeMode, type AccentColor, type FontSize } from '$lib/stores/preferencesStore';
  import { settingsApi } from '$lib/api';
  import { closeSidebar } from '$lib/stores/uiStore';

  let stopPreferenceWatch: (() => void) | null = null;
  $: if (browser && !stopPreferenceWatch) {
    initPreferences();
    stopPreferenceWatch = watchSystemTheme();
  }

  $: currentPath = $page.url.pathname;
  $: isCustomSite = $page.route.id?.startsWith('/_site') ?? false;
  $: activeTab = currentPath === '/home' ? 'home' : currentPath.replace(/^\//, '');
  $: isAuthOrStandalone =
    currentPath === '/' ||
    isCustomSite ||
    currentPath.startsWith('/e/') ||
    currentPath.startsWith('/org/') ||
    currentPath.startsWith('/login') ||
    currentPath.startsWith('/register') ||
    currentPath.startsWith('/verify-email') ||
    currentPath.startsWith('/forgot-password') ||
    currentPath.startsWith('/reset-password') ||
    currentPath.startsWith('/apply/educator') ||
    currentPath.startsWith('/auth/callback') ||
    currentPath.startsWith('/certificates/verify/') ||
    currentPath.startsWith('/sso') ||
    currentPath === '/chat';
  $: isPublicBrowse = currentPath === '/marketplace' || /^\/courses\/[^/]+$/.test(currentPath) || currentPath.startsWith('/courses/join/');
  $: requiresAuthStandalone = currentPath.startsWith('/apply/educator');
  $: studioRoute = currentPath.startsWith('/studio');
  $: hasEducatorCapability = $session.roles.includes('Educator');
  $: educatorVerificationRequired = $session.educatorVerificationRequired;
  $: learnerOnlyRoute = currentPath.startsWith('/app')
    || currentPath.startsWith('/home')
    || currentPath.startsWith('/progress')
    || currentPath.startsWith('/recommendations')
    || currentPath.startsWith('/ai-tutor')
    || currentPath.startsWith('/my-skills');

  let lastPath = '';
  $: if (currentPath !== lastPath) {
    lastPath = currentPath;
    closeSidebar();
  }

  let sessionStarted = false;
  $: if (browser && !sessionStarted) {
    sessionStarted = true;
    void initSession();
  }

  $: if (browser && $session.ready && !$session.authenticated && ((!isAuthOrStandalone && !isPublicBrowse) || requiresAuthStandalone)) {
    void goto(`/login?redirect=${encodeURIComponent($page.url.pathname + $page.url.search)}`);
  }
  $: if (browser && $session.ready && $session.authenticated && studioRoute && !hasEducatorCapability) {
    void goto('/apply/educator', { replaceState: true });
  }
  $: if (browser && $session.ready && $session.authenticated && educatorVerificationRequired && !currentPath.startsWith('/apply/educator')) {
    void goto('/apply/educator', { replaceState: true });
  }
  $: if (browser && $session.ready && $session.authenticated && hasEducatorCapability && currentPath.startsWith('/apply/educator')) {
    void goto('/studio/courses', { replaceState: true });
  }
  $: if (browser && $session.ready && $session.authenticated && hasEducatorCapability && learnerOnlyRoute) {
    void goto('/studio/courses', { replaceState: true });
  }

  let hydratedFor: string | null = null;
  let appearanceFor: string | null = null;
  let storeIdentity = '';
  $: if (browser && $session.ready && storeIdentity !== `${$session.authenticated}:${$session.learnerId}`) {
    storeIdentity = `${$session.authenticated}:${$session.learnerId}`;
    hydratedFor = null;
    appearanceFor = null;
    resetCourses();
    resetDashboard();
  }
  $: if (browser && $session.authenticated && $session.learnerId && appearanceFor !== $session.learnerId) {
    appearanceFor = $session.learnerId;
    const learnerId = $session.learnerId;
    void settingsApi.get().then((settings) => {
      if ($session.learnerId !== learnerId) return;
      hydratePreferences({
        theme: (['light', 'dark', 'system'].includes(settings.theme) ? settings.theme : 'system') as ThemeMode,
        accentColor: (['pine', 'blue', 'purple', 'orange'].includes(settings.accentColor) ? settings.accentColor : 'pine') as AccentColor,
        fontSize: (['small', 'medium', 'large', 'extra-large'].includes(settings.fontSize) ? settings.fontSize : 'medium') as FontSize,
        compactMode: settings.compactMode,
        reducedMotion: settings.reducedMotion
      });
    }).catch(() => undefined);
  }
  $: if (browser && $session.authenticated && $session.learnerId && hydratedFor !== $session.learnerId) {
    hydratedFor = $session.learnerId;
    const learnerId = $session.learnerId;
    void (async () => {
      await loadCourses();
      if ($session.authenticated && $session.learnerId === learnerId) await loadDashboard();
    })();
  }
</script>

{#if requiresAuthStandalone && !$session.authenticated}
  <div class="min-h-screen flex items-center justify-center bg-ifa-bg text-ifa-text-secondary text-sm">
    <p>{$session.ready ? 'Redirecting to sign in…' : 'Loading Witforge…'}</p>
  </div>
{:else if isAuthOrStandalone}
  <slot />
{:else if !$session.authenticated && isPublicBrowse}
  <PublicShell><slot /></PublicShell>
{:else if !$session.authenticated}
  <div class="min-h-screen flex items-center justify-center bg-ifa-bg text-ifa-text-secondary text-sm">
    <p>{$session.ready ? 'Redirecting to sign in…' : 'Loading Witforge…'}</p>
  </div>
{:else}
  <WorkspaceShell {activeTab} mode={studioRoute ? 'studio' : 'learner'}>
    <slot />
  </WorkspaceShell>
{/if}
