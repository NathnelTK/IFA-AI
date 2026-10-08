<script lang="ts">
  import { Search, Bell, Sun, Moon, Home } from 'lucide-svelte';
  import { userProfile } from '../stores/dashboardStore';
  import { goto } from '$app/navigation';
  import NotificationCenter from './NotificationCenter.svelte';
  import CommandPalette from './CommandPalette.svelte';
  import LearnerProfileMenu from './LearnerProfileMenu.svelte';
  import {
    notificationsOpen,
    commandPaletteOpen,
    theme,
    toggleTheme
  } from '$lib/stores/uiStore';

  // Global ⌘K / Ctrl+K opens the command palette from anywhere.
  function handleKeydown(e: KeyboardEvent) {
    if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault();
      commandPaletteOpen.set(true);
    }
  }

  function goToLanding() {
    goto('/');
  }
</script>

<svelte:window on:keydown={handleKeydown} />

<header class="h-20 px-8 flex items-center justify-between border-b border-ifa-border bg-ifa-bg/80 backdrop-blur-md sticky top-0 z-20">
  <!-- Greeting with Landing Link -->
  <div class="flex items-center gap-4">
    <button
      type="button"
      on:click={goToLanding}
      title="Go to Landing Page"
      class="w-8 h-8 rounded-lg bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine hover:bg-ifa-card-muted transition shadow-soft"
    >
      <Home class="w-4 h-4" />
    </button>
    <div>
      <h1 class="text-xl font-bold tracking-tight text-ifa-text-primary flex items-center gap-2">
        {$userProfile.greeting}
      </h1>
      <p class="text-xs text-ifa-text-secondary font-medium">{$userProfile.tagline}</p>
    </div>
  </div>

  <!-- Search & Profile Controls -->
  <div class="flex items-center gap-4">
    <!-- Search Bar (opens command palette) -->
    <button
      type="button"
      on:click={() => commandPaletteOpen.set(true)}
      class="relative w-72 text-left"
    >
      <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-ifa-text-muted">
        <Search class="w-4 h-4" />
      </div>
      <div
        class="w-full pl-10 pr-12 py-2 text-xs rounded-xl bg-ifa-card border border-ifa-border text-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine transition shadow-soft"
      >
        Search anything...
      </div>
      <div class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
        <kbd class="px-1.5 py-0.5 text-[10px] font-semibold text-ifa-text-muted bg-ifa-bg rounded border border-ifa-border">⌘K</kbd>
      </div>
    </button>

    <!-- Notification Bell -->
    <button
      type="button"
      on:click={() => notificationsOpen.set(true)}
      title="Notifications"
      class="w-9 h-9 rounded-xl bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine hover:bg-ifa-card-muted transition shadow-soft relative"
    >
      <Bell class="w-4 h-4" />
      <span class="absolute top-2 right-2 w-1.5 h-1.5 bg-ifa-accent-orange rounded-full"></span>
    </button>

    <!-- Theme Mode Toggle -->
    <button
      type="button"
      on:click={toggleTheme}
      title="Toggle {$theme === 'dark' ? 'light' : 'dark'} mode"
      class="w-9 h-9 rounded-xl bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine hover:bg-ifa-card-muted transition shadow-soft"
    >
      {#if $theme === 'dark'}
        <Moon class="w-4 h-4" />
      {:else}
        <Sun class="w-4 h-4" />
      {/if}
    </button>

    <!-- User Profile Dropdown (self-contained menu) -->
    <LearnerProfileMenu />
  </div>
</header>

<!-- Overlays driven by the UI store -->
<NotificationCenter isOpen={$notificationsOpen} on:close={() => notificationsOpen.set(false)} />
<CommandPalette isOpen={$commandPaletteOpen} on:close={() => commandPaletteOpen.set(false)} />
