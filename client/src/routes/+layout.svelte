<script lang="ts">
  import '../app.css';
  import Sidebar from '$lib/components/Sidebar.svelte';
  import Header from '$lib/components/Header.svelte';
  import { page } from '$app/stores';

  $: currentPath = $page.url.pathname;
  $: activeTab = currentPath === '/home' ? 'home' : currentPath.replace('/', '');
  $: isLandingPage = currentPath === '/';
</script>

{#if isLandingPage}
  <!-- Landing page layout - no sidebar -->
  <slot />
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