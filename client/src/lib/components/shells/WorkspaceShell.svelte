<script lang="ts">
  import Sidebar from '$lib/components/Sidebar.svelte';
  import Header from '$lib/components/Header.svelte';

  export let activeTab: string;
  export let mode: 'learner' | 'studio' = 'learner';
</script>

<a href="#main-content" class="fixed left-3 top-3 z-[100] -translate-y-20 rounded-lg bg-ifa-pine px-4 py-2 text-sm font-bold text-white focus:translate-y-0">
  Skip to content
</a>

<div class="flex min-h-screen bg-ifa-bg text-ifa-text-primary antialiased selection:bg-[#C7F464] selection:text-[#17221C]">
  <Sidebar {activeTab} {mode} />
  <div class="flex min-w-0 flex-1 flex-col">
    <Header />
    <main id="main-content" tabindex="-1" class="flex-1 overflow-x-hidden overflow-y-auto">
      <slot />
    </main>
  </div>
</div>
