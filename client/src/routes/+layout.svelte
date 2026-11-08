<script lang="ts">
  import '../app.css';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { browser } from '$app/environment';
  import Sidebar from '$lib/components/Sidebar.svelte';
  import Header from '$lib/components/Header.svelte';
  import GlowCursor from '$lib/components/GlowCursor.svelte';
  import { initSession, session } from '$lib/stores/sessionStore';
  import { loadCourses, resetCourses } from '$lib/stores/coursesStore';
  import { loadDashboard, resetDashboard } from '$lib/stores/dashboardStore';
  import { initVoxide } from '$lib/voxide';
  import { hydratePreferences, initPreferences, watchSystemTheme, type ThemeMode, type AccentColor, type FontSize } from '$lib/stores/preferencesStore';
  import { settingsApi } from '$lib/api';
  import { closeSidebar } from '$lib/stores/uiStore';

  // Apply the stored appearance preferences as early as possible on the client,
  // and keep `system` theme in sync with the OS.
  let stopSystemWatch: (() => void) | null = null;
  $: if (browser && !stopSystemWatch) {
    initPreferences();
    stopSystemWatch = watchSystemTheme();
  }

  $: currentPath = $page.url.pathname;
  $: activeTab = currentPath === '/home' ? 'home' : currentPath.replace('/', '');

  // Close the mobile navigation drawer whenever the route changes.
  let lastPath = '';
  $: if (currentPath !== lastPath) {
    lastPath = currentPath;
    closeSidebar();
  }
  // Pages that render without the authenticated app shell.
  $: isPublicPage =
    currentPath === '/' ||
    currentPath.startsWith('/login') ||
    currentPath.startsWith('/register') ||
    // The full-screen authoring routes render their own sign-in prompt.
    currentPath === '/chat' ||
    currentPath === '/studio/create';
  $: isPublicBrowse = currentPath === '/marketplace' || /^\/courses\/[^/]+$/.test(currentPath) || currentPath.startsWith('/courses/join/');

  // Bootstrap the API session once per page load (client side only).
  let sessionStarted = false;
  $: if (browser && !sessionStarted) {
    sessionStarted = true;
    void initSession();
  }

  // Initialize the Voxide voice assistant once, after sign-in (needs the browser
  // + a publishable key; it no-ops when the key is absent).
  let voxideStarted = false;
  $: if (browser && $session.authenticated && !voxideStarted) {
    voxideStarted = true;
    void initVoxide();
  }

  // Redirect unauthenticated visitors away from protected pages once the
  // session bootstrap has settled.
  $: if (browser && $session.ready && !$session.authenticated && !isPublicPage && !isPublicBrowse) {
    void goto(`/login?redirect=${encodeURIComponent($page.url.pathname + $page.url.search)}`);
  }

  // Keep workspace routes aligned with the account's single selected role.
  $: if (browser && $session.ready && $session.authenticated) {
    const isEducator = $session.role === 'Educator';
    const educatorOnlyRoute = currentPath.startsWith('/studio') && currentPath !== '/studio/create';
    const learnerOnlyRoute =
      currentPath === '/home' ||
      currentPath === '/courses' ||
      currentPath.startsWith('/recommendations') ||
      currentPath.startsWith('/ai-tutor') ||
      currentPath.startsWith('/my-skills') ||
      currentPath.startsWith('/progress');

    if (educatorOnlyRoute && !isEducator) {
      void goto('/home', { replaceState: true });
    } else if (learnerOnlyRoute && isEducator) {
      void goto('/studio', { replaceState: true });
    }
  }

  // Hydrate learner-only stores once per signed-in learner.
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
  $: if (
    browser &&
    $session.authenticated &&
    $session.role !== 'Educator' &&
    $session.learnerId &&
    hydratedFor !== $session.learnerId
  ) {
    hydratedFor = $session.learnerId;
    const learnerId = $session.learnerId;
    void (async () => {
      await loadCourses();
      if ($session.authenticated && $session.learnerId === learnerId) await loadDashboard();
    })();
  }
</script>

{#if isPublicPage}
  <!-- Landing / auth pages: no app shell -->
  <slot />
{:else if !$session.authenticated && isPublicBrowse}
  <div class="min-h-screen bg-ifa-bg text-ifa-text-primary">
    <header class="flex flex-wrap items-center justify-between gap-3 px-6 py-4 border-b border-ifa-border bg-ifa-card">
      <a href="/" class="font-bold text-lg text-ifa-pine">Witforge</a>
      <nav aria-label="Public navigation" class="flex items-center gap-4 text-sm">
        <a href="/marketplace" class="font-semibold text-ifa-text-secondary">Browse Courses</a>
        <a href={`/login?redirect=${encodeURIComponent($page.url.pathname + $page.url.search)}`} class="px-4 py-2 rounded-xl bg-ifa-pine text-white font-semibold">Sign In</a>
      </nav>
    </header>
    <main><slot /></main>
  </div>
{:else if !$session.authenticated}
  <!-- Awaiting session or redirecting to /login -->
  <div class="min-h-screen flex items-center justify-center bg-ifa-bg text-ifa-text-secondary text-sm">
    <p>{$session.ready ? 'Redirecting to sign in…' : 'Loading Witforge…'}</p>
  </div>
{:else}
  <!-- App layout with sidebar -->
  <div class="flex min-h-screen bg-ifa-bg text-ifa-text-primary antialiased selection:bg-emerald-100 selection:text-emerald-900">
    <!-- Fixed Left Sidebar -->
    <Sidebar {activeTab} />

    <!-- Main Content Area -->
    <div class="flex-1 flex flex-col min-w-0">
      <Header />
      <main class="flex-1 overflow-y-auto">
        <slot />
      </main>
    </div>
  </div>
{/if}

<!-- Ambient glowing cursor (auto-disabled on touch + reduced motion) -->
<GlowCursor />
