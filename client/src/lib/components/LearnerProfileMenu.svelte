<script lang="ts">
  import { goto } from '$app/navigation';
  import { User, Settings, LogOut, ChevronDown, Layout, BookOpen, Trophy, Target } from 'lucide-svelte';
  import { session, signOut } from '$lib/stores/sessionStore';

  const menuItems = [
    { label: 'My Courses', icon: BookOpen, href: '/courses' },
    { label: 'Skills', icon: Target, href: '/my-skills' },
    { label: 'Progress', icon: Trophy, href: '/progress' },
    { divider: true },
    { label: 'Settings', icon: Settings, href: '/settings' },
    { label: 'Sign Out', icon: LogOut, action: 'signout' }
  ];

  let isOpen = false;

  $: displayName = $session.name || $session.email?.split('@')[0] || 'Learner';
  $: initial = displayName.charAt(0).toUpperCase();

  function handleItemClick(item: any) {
    isOpen = false;
    if (item.action === 'signout') {
      signOut();
      void goto('/login');
    } else if (item.href) {
      void goto(item.href);
    }
  }
</script>

<div class="relative">
  <!-- Trigger Button -->
  <button
    type="button"
    on:click={() => (isOpen = !isOpen)}
    class="ifa-menu-trigger flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-ifa-card-muted transition"
  >
    {#if $session.avatarUrl}
      <img src={$session.avatarUrl} alt="" class="w-8 h-8 rounded-full object-cover" />
    {:else}
      <div class="w-8 h-8 rounded-full bg-ifa-pine flex items-center justify-center text-white font-semibold text-sm">
        {initial}
      </div>
    {/if}
    <span class="text-sm font-semibold text-ifa-text-primary hidden md:block">{displayName}</span>
    <ChevronDown class="w-4 h-4 text-ifa-text-muted" />
  </button>

  <!-- Dropdown Menu -->
  {#if isOpen}
    <div class="absolute right-0 mt-2 w-56 bg-ifa-card rounded-xl border border-ifa-border shadow-2xl overflow-hidden z-50">
      <!-- User Info -->
      <div class="px-4 py-3 border-b border-ifa-border">
        <div class="flex items-center gap-3">
          {#if $session.avatarUrl}
            <img src={$session.avatarUrl} alt="" class="w-10 h-10 rounded-full object-cover" />
          {:else}
            <div class="w-10 h-10 rounded-full bg-ifa-pine flex items-center justify-center text-white font-semibold">
              {initial}
            </div>
          {/if}
          <div class="min-w-0">
            <h3 class="text-sm font-bold text-ifa-text-primary truncate">{displayName}</h3>
            <p class="text-xs text-ifa-text-secondary truncate">{$session.email ?? 'Learner'}</p>
          </div>
        </div>
      </div>

      <!-- Menu Items -->
      <div class="py-1">
        {#each menuItems as item}
          {#if item.divider}
            <div class="border-t border-ifa-border my-1"></div>
          {:else}
            <button
              type="button"
              on:click={() => handleItemClick(item)}
              class="ifa-menu-row w-full flex items-center gap-3 px-4 py-2.5 text-left hover:bg-ifa-card-muted transition"
            >
              {#if item.icon === Layout}
                <Layout class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === BookOpen}
                <BookOpen class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === Target}
                <Target class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === Trophy}
                <Trophy class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === Settings}
                <Settings class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === LogOut}
                <LogOut class="w-4 h-4 text-red-500" />
              {:else}
                <User class="w-4 h-4 text-ifa-text-muted" />
              {/if}
              <span class="text-sm {item.action === 'signout' ? 'text-red-600' : 'text-ifa-text-primary'}">{item.label}</span>
            </button>
          {/if}
        {/each}
      </div>
    </div>
  {/if}
</div>

<svelte:window
  on:click={(e) => {
    if (!e.target) return;
    const target = e.target as HTMLElement;
    if (!target.closest('.relative')) isOpen = false;
  }}
/>
