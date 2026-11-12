<script lang="ts">
  import { Bell, Menu, Moon, Search, Sun } from 'lucide-svelte';
  import { page } from '$app/stores';
  import NotificationCenter from './NotificationCenter.svelte';
  import CommandPalette from './CommandPalette.svelte';
  import LearnerProfileMenu from './LearnerProfileMenu.svelte';
  import { notificationsApi } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';
  import {
    notificationsOpen,
    commandPaletteOpen,
    sidebarOpen,
    toggleSidebar,
    theme,
    toggleTheme
  } from '$lib/stores/uiStore';

  let unreadCount = 0;
  let notificationIdentity = '';

  $: path = $page.url.pathname;
  $: context = path.startsWith('/studio')
    ? 'Educator Studio'
    : path.startsWith('/app')
      ? 'Learning workspace'
      : 'Witforge';
  $: pageTitle = path === '/app' ? 'Overview'
    : path.includes('/courses') ? 'Courses'
      : path.includes('/progress') ? 'Progress'
        : path.includes('/certificates') ? 'Certificates'
          : path.includes('/analytics') ? 'Analytics'
            : path.includes('/team') ? 'Team'
              : path.includes('/site') ? 'Public site'
                : path.includes('/settings') ? 'Settings'
                  : path.includes('/research') ? 'Research'
                    : 'Workspace';

  $: if (!$session.authenticated) {
    notificationIdentity = '';
    unreadCount = 0;
  }
  $: if ($session.authenticated && $session.learnerId && notificationIdentity !== $session.learnerId) {
    notificationIdentity = $session.learnerId;
    const identity = notificationIdentity;
    void notificationsApi.list(true)
      .then((items) => { if (identity === notificationIdentity) unreadCount = items.length; })
      .catch(() => { if (identity === notificationIdentity) unreadCount = 0; });
  }

  function keydown(event: KeyboardEvent) {
    if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      commandPaletteOpen.set(true);
    }
  }
</script>

<svelte:window on:keydown={keydown} />

<header class="app-header">
  <div class="header-context">
    <button id="mobile-menu-trigger" type="button" on:click={toggleSidebar} aria-label="Open navigation" aria-expanded={$sidebarOpen} class="header-icon mobile-trigger"><Menu /></button>
    <div><span>{context}</span><strong>{pageTitle}</strong></div>
  </div>

  <button type="button" on:click={() => commandPaletteOpen.set(true)} class="command-trigger">
    <Search />
    <span>Search courses, lessons, people…</span>
    <kbd>⌘ K</kbd>
  </button>

  <div class="header-actions">
    <button type="button" on:click={() => commandPaletteOpen.set(true)} aria-label="Search" class="header-icon compact-search"><Search /></button>
    <button type="button" on:click={() => notificationsOpen.set(true)} aria-label={unreadCount ? `Notifications, ${unreadCount} unread` : 'Notifications'} class="header-icon">
      <Bell />{#if unreadCount}<i>{unreadCount > 9 ? '9+' : unreadCount}</i>{/if}
    </button>
    <button type="button" on:click={toggleTheme} aria-label="Toggle theme" class="header-icon">{#if $theme === 'dark'}<Moon />{:else}<Sun />{/if}</button>
    <LearnerProfileMenu />
  </div>
</header>

<NotificationCenter isOpen={$notificationsOpen} on:change={(event) => (unreadCount = event.detail.unreadCount)} on:close={() => notificationsOpen.set(false)} />
<CommandPalette isOpen={$commandPaletteOpen} on:close={() => commandPaletteOpen.set(false)} />

<style>
  .app-header{position:sticky;top:0;z-index:20;display:grid;grid-template-columns:1fr minmax(18rem,31rem) 1fr;align-items:center;gap:1rem;min-height:4.65rem;padding:0 1.6rem;border-bottom:1px solid var(--wf-border);background:color-mix(in srgb,var(--wf-surface-canvas) 88%,transparent);backdrop-filter:blur(18px)}
  .header-context{display:flex;align-items:center;gap:.8rem;min-width:0}.header-context>div{display:flex;flex-direction:column;min-width:0}.header-context span{color:var(--wf-text-muted);font-size:.55rem;font-weight:800;letter-spacing:.13em;text-transform:uppercase}.header-context strong{margin-top:.15rem;overflow:hidden;font-family:'Newsreader Variable';font-size:1.25rem;font-weight:520;letter-spacing:-.025em;text-overflow:ellipsis;white-space:nowrap}
  .command-trigger{display:grid;grid-template-columns:1rem 1fr auto;align-items:center;gap:.7rem;width:100%;padding:.65rem .75rem;border:1px solid var(--wf-border);border-radius:999px;color:var(--wf-text-muted);background:var(--wf-surface-raised);font-size:.68rem;text-align:left;box-shadow:0 10px 30px -24px #17221c}.command-trigger :global(svg){width:1rem}.command-trigger kbd{padding:.18rem .4rem;border:1px solid var(--wf-border);border-radius:999px;background:var(--wf-surface-muted);font-family:inherit;font-size:.54rem;font-weight:800}
  .header-actions{display:flex;justify-content:flex-end;align-items:center;gap:.45rem}.header-icon{position:relative;display:grid;width:2.35rem;height:2.35rem;place-items:center;border:1px solid transparent;border-radius:50%;color:var(--wf-text-muted);transition:color .18s,border-color .18s,background .18s}.header-icon:hover{color:var(--wf-text);border-color:var(--wf-border);background:var(--wf-surface-raised)}.header-icon :global(svg){width:1rem}.header-icon i{position:absolute;top:-.15rem;right:-.1rem;display:grid;min-width:1.05rem;height:1.05rem;place-items:center;padding:0 .18rem;border:2px solid var(--wf-surface-canvas);border-radius:999px;color:#17221c;background:#c7f464;font-size:.48rem;font-style:normal;font-weight:900}.mobile-trigger,.compact-search{display:none}
  @media(max-width:900px){.app-header{grid-template-columns:1fr auto}.command-trigger{display:none}.compact-search{display:grid}}
  @media(max-width:767px){.app-header{min-height:4.25rem;padding:0 .9rem}.mobile-trigger{display:grid}.header-context strong{font-size:1.05rem}.header-context span{font-size:.49rem}.header-actions{gap:.15rem}}
</style>
