<script lang="ts">
  import '../app.css';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { browser } from '$app/environment';
  import Sidebar from '$lib/components/Sidebar.svelte';
  import Header from '$lib/components/Header.svelte';
  import GlowCursor from '$lib/components/GlowCursor.svelte';
  import { initSession, session } from '$lib/stores/sessionStore';
  import { loadCourses } from '$lib/stores/coursesStore';
  import { loadDashboard } from '$lib/stores/dashboardStore';
  import { initVoxide } from '$lib/voxide';
  import { initPreferences, watchSystemTheme } from '$lib/stores/preferencesStore';
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
    currentPath.startsWith('/register');

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
  $: if (browser && $session.ready && !$session.authenticated && !isPublicPage) {
    void goto('/login');
  }

  // Hydrate courses + dashboard exactly once per signed-in learner, so data is
  // loaded immediately after sign-in as well as on reload.
  let hydratedFor: string | null = null;
  $: if (browser && $session.authenticated && $session.learnerId && hydratedFor !== $session.learnerId) {
    hydratedFor = $session.learnerId;
    void (async () => {
      await loadCourses();
      await loadDashboard();
    })();
  }
</script>

{#if isPublicPage}
  <!-- Landing / auth pages: no app shell -->
  <slot />
{:else if !$session.authenticated}
  <!-- Awaiting session or redirecting to /login -->
  <div class="min-h-screen flex items-center justify-center bg-ifa-bg text-ifa-text-secondary text-sm">
    <p>{$session.ready ? 'Redirecting to sign in…' : 'Loading IFA…'}</p>
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
