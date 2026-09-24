<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { User, Settings, LogOut, ChevronDown, Layout, BookOpen, Trophy, Star } from 'lucide-svelte';

  export let isOpen = false;

  const dispatch = createEventDispatcher();

  const user = {
    name: 'Nathnel',
    email: 'nathnel@example.com',
    avatar: null,
    role: 'Learner'
  };

  const menuItems = [
    { label: 'My Learning', icon: Layout, href: '/my-learning' },
    { label: 'Courses', icon: BookOpen, href: '/courses' },
    { label: 'Skills', icon: Star, href: '/my-skills' },
    { label: 'Progress', icon: Trophy, href: '/progress' },
    { divider: true },
    { label: 'Settings', icon: Settings, href: '/settings' },
    { label: 'Sign Out', icon: LogOut, action: 'signout' }
  ];

  function handleClose() {
    isOpen = false;
    dispatch('close');
  }

  function handleItemClick(item: any) {
    if (item.action === 'signout') {
      console.log('Sign out');
    } else if (item.href) {
      console.log('Navigate to:', item.href);
    }
    handleClose();
  }
</script>

<div class="relative">
  <!-- Trigger Button -->
  <button
    type="button"
    on:click={() => isOpen = !isOpen}
    class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-ifa-card-muted transition"
  >
    <div class="w-8 h-8 rounded-full bg-ifa-pine flex items-center justify-center text-white font-semibold text-sm">
      {user.name.charAt(0)}
    </div>
    <span class="text-sm font-semibold text-ifa-text-primary hidden md:block">{user.name}</span>
    <ChevronDown class="w-4 h-4 text-ifa-text-muted" />
  </button>

  <!-- Dropdown Menu -->
  {#if isOpen}
    <div class="absolute right-0 mt-2 w-56 bg-ifa-card rounded-xl border border-ifa-border shadow-2xl overflow-hidden z-50">
      <!-- User Info -->
      <div class="px-4 py-3 border-b border-ifa-border">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-full bg-ifa-pine flex items-center justify-center text-white font-semibold">
            {user.name.charAt(0)}
          </div>
          <div>
            <h3 class="text-sm font-bold text-ifa-text-primary">{user.name}</h3>
            <p class="text-xs text-ifa-text-secondary">{user.email}</p>
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
              class="w-full flex items-center gap-3 px-4 py-2.5 text-left hover:bg-ifa-card-muted transition"
            >
              {#if item.icon === Layout}
                <Layout class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === BookOpen}
                <BookOpen class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === Star}
                <Star class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === Trophy}
                <Trophy class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === Settings}
                <Settings class="w-4 h-4 text-ifa-text-muted" />
              {:else if item.icon === LogOut}
                <LogOut class="w-4 h-4 text-red-500" />
              {:else}
                <User class="w-4 h-4 text-ifa-text-muted" />
              {/if}
              <span class="text-sm text-ifa-text-primary {item.action === 'signout' ? 'text-red-500' : ''}">{item.label}</span>
            </button>
          {/if}
        {/each}
      </div>
    </div>
  {/if}
</div>

<svelte:window on:click={(e) => {
  if (!e.target) return;
  const target = e.target as HTMLElement;
  if (!target.closest('.relative')) {
    isOpen = false;
  }
}} />
