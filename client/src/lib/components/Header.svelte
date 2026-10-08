<script lang="ts">
  import { Search, Bell, Sun, Moon, Home, Menu } from 'lucide-svelte';
  import { userProfile } from '../stores/dashboardStore';
  import { goto } from '$app/navigation';
  import NotificationCenter from './NotificationCenter.svelte';
  import CommandPalette from './CommandPalette.svelte';
  import LearnerProfileMenu from './LearnerProfileMenu.svelte';
  import {
    notificationsOpen,
    commandPaletteOpen,
    sidebarOpen,
    toggleSidebar,
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

<header class="h-16 md:h-20 px-4 md:px-8 flex items-center justify-between gap-3 border-b border-ifa-border bg-ifa-bg/80 backdrop-blur-md sticky top-0 z-20">
  <!-- Left: menu / landing / greeting -->
  <div class="flex items-center gap-2 md:gap-4 min-w-0">
    <!-- Mobile menu (opens the sidebar drawer) -->
    <button
      type="button"
      on:click={toggleSidebar}
      aria-label="Open menu"
      aria-expanded={$sidebarOpen}
      title="Menu"
      class="md:hidden w-9 h-9 rounded-lg bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine hover:bg-ifa-card-muted transition shadow-soft shrink-0"
    >
      <Menu class="w-4 h-4" />
    </button>

    <button
      type="button"
      on:click={goToLanding}
      title="Go to Landing Page"
      class="hidden sm:flex w-8 h-8 rounded-lg bg-ifa-card border border-ifa-border items-center justify-center text-ifa-text-secondary hover:text-ifa-pine hover:bg-ifa-card-muted transition shadow-soft shrink-0"
    >
      <Home class="w-4 h-4" />
    </button>

    <div class="min-w-0">
      <h1 class="text-base md:text-xl font-bold tracking-tight text-ifa-text-primary flex items-center gap-2 truncate">
        {$userProfile.greeting}
      </h1>
      <p class="hidden sm:block text-xs text-ifa-text-secondary font-medium truncate">{$userProfile.tagline}</p>
    </div>
  </div>

  <!-- Right: search + controls -->
  <div class="flex items-center gap-2 md:gap-4 shrink-0">
    <!-- Full search bar (md+) -->
    <button
      type="button"
      on:click={() => commandPaletteOpen.set(true)}
      class="relative w-72 text-left hidden md:block"
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

    <!-- Compact search (mobile) -->
    <button
      type="button"
      on:click={() => commandPaletteOpen.set(true)}
      aria-label="Search"
      title="Search"
      class="md:hidden w-9 h-9 rounded-xl bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine transition shadow-soft"
    >
      <Search class="w-4 h-4" />
    </button>

    <!-- Notification Bell -->
    <button
      type="button"
      on:click={() => notificationsOpen.set(true)}
      title="Notifications"
      aria-label="Notifications"
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
      aria-label="Toggle theme"
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
