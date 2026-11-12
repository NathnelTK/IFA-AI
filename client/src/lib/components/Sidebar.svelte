<script lang="ts">
  import {
    Award,
    BarChart3,
    BookOpen,
    Bot,
    CheckCircle2,
    Globe2,
    Home,
    Lightbulb,
    Plus,
    Search,
    Settings,
    Sparkles,
    TrendingUp,
    Users,
    X
  } from 'lucide-svelte';
  import { tick } from 'svelte';
  import { sidebarOpen, closeSidebar } from '$lib/stores/uiStore';
  import { session, setActiveWorkspace } from '$lib/stores/sessionStore';

  export let activeTab = 'home';
  export let mode: 'learner' | 'studio' = 'learner';

  let closeButton: HTMLButtonElement;
  let wasOpen = false;
  let selectedWorkspace = '';
  $: isStudio = mode === 'studio';
  $: if ($session.workspaces?.length
    && (!selectedWorkspace || !$session.workspaces.some((workspace) => workspace.id === selectedWorkspace))) {
    selectedWorkspace = typeof localStorage === 'undefined'
      ? $session.workspaces[0].id
      : localStorage.getItem('witforge.activeWorkspace') ?? $session.workspaces[0].id;
  }
  $: if ($sidebarOpen && !wasOpen) void tick().then(() => closeButton?.focus());
  $: wasOpen = $sidebarOpen;

  const learnerNav = [
    { id: 'home', label: 'Overview', icon: Home, href: '/app' },
    { id: 'courses', label: 'My courses', icon: BookOpen, href: '/app/courses' },
    { id: 'marketplace', label: 'Discover', icon: Globe2, href: '/marketplace' },
    { id: 'recommendations', label: 'For you', icon: Lightbulb, href: '/recommendations' },
    { id: 'ai-tutor', label: 'Study assistant', icon: Bot, href: '/ai-tutor' },
    { id: 'my-skills', label: 'Skills', icon: CheckCircle2, href: '/my-skills' },
    { id: 'research', label: 'Research', icon: Search, href: '/research' },
    { id: 'progress', label: 'Progress', icon: TrendingUp, href: '/app/progress' },
    { id: 'app/certificates', label: 'Certificates', icon: Award, href: '/app/certificates' }
  ];

  const studioNav = [
    { id: 'studio', label: 'Courses', icon: BookOpen, href: '/studio/courses' },
    { id: 'studio-create', label: 'Create course', icon: Plus, href: '/studio/create' },
    { id: 'studio-team', label: 'Team', icon: Users, href: '/studio/team' },
    { id: 'studio-site', label: 'Public site', icon: Globe2, href: '/studio/site' },
    { id: 'studio-analytics', label: 'Analytics', icon: BarChart3, href: '/studio/analytics' },
    { id: 'research', label: 'Research', icon: Search, href: '/research' }
  ];

  $: navItems = isStudio ? studioNav : learnerNav;
  $: activeId = !isStudio
    ? activeTab === 'app' ? 'home'
      : activeTab === 'app/courses' ? 'courses'
        : activeTab === 'app/progress' ? 'progress'
          : activeTab
    : activeTab === 'studio' || activeTab === 'studio/courses' ? 'studio'
      : activeTab === 'studio/create' ? 'studio-create'
        : activeTab === 'studio/team' ? 'studio-team'
          : activeTab === 'studio/site' ? 'studio-site'
            : activeTab === 'studio/analytics' ? 'studio-analytics'
              : activeTab.startsWith('studio/') ? 'studio' : activeTab;

  function dismiss(restoreFocus = false) {
    closeSidebar();
    if (restoreFocus) document.getElementById('mobile-menu-trigger')?.focus();
  }

  function changeWorkspace(event: Event) {
    selectedWorkspace = (event.currentTarget as HTMLSelectElement).value;
    setActiveWorkspace(selectedWorkspace);
  }

  function keydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && $sidebarOpen) dismiss(true);
  }
</script>

<svelte:window on:keydown={keydown} />

{#if $sidebarOpen}
  <button type="button" aria-label="Close navigation" on:click={() => dismiss(true)} class="fixed inset-0 z-40 bg-[#101511]/60 backdrop-blur-sm md:hidden"></button>
{/if}

<aside
  aria-label={isStudio ? 'Studio navigation' : 'Learner navigation'}
  class="sidebar-shell {$sidebarOpen ? 'translate-x-0' : '-translate-x-full'}"
>
  <div class="sidebar-main">
    <div class="sidebar-brand">
      <a href={isStudio ? '/studio/courses' : '/app'} class="brand-lockup">
        <span class="brand-mark"><i></i><i></i><i></i></span>
        <span><strong>Witforge</strong><small>{isStudio ? 'Educator Studio' : 'Learning space'}</small></span>
      </a>
      <button bind:this={closeButton} type="button" aria-label="Close navigation" on:click={() => dismiss(true)} class="close-button md:hidden"><X /></button>
    </div>

    {#if isStudio && $session.workspaces?.length}
      <div class="workspace-field">
        <label for="workspace-switcher">Active workspace</label>
        <select id="workspace-switcher" value={selectedWorkspace} on:change={changeWorkspace}>
          {#each $session.workspaces as workspace}<option value={workspace.id}>{workspace.name}</option>{/each}
        </select>
      </div>
    {/if}

    <p class="nav-kicker">{isStudio ? 'Build and operate' : 'Learn and progress'}</p>
    <nav class="sidebar-nav">
      {#each navItems as item}
        {@const active = activeId === item.id}
        <a href={item.href} on:click={closeSidebar} aria-current={active ? 'page' : undefined} class:active>
          <svelte:component this={item.icon} />
          <span>{item.label}</span>
          {#if active}<i></i>{/if}
        </a>
      {/each}
    </nav>
  </div>

  <div class="sidebar-foot">
    {#if isStudio}
      <a href="/app"><Sparkles /><span><strong>Switch to learning</strong><small>Open your learner workspace</small></span></a>
    {:else if $session.roles.includes('Educator')}
      <a href="/studio/courses"><Sparkles /><span><strong>Open Studio</strong><small>Manage courses and teams</small></span></a>
    {/if}
    <a href="/settings" class="settings-link"><Settings /><span>Settings</span></a>
    <p>Build carefully. Learn deeply.</p>
  </div>
</aside>

<style>
  .sidebar-shell{position:fixed;inset:0 auto 0 0;z-index:50;display:flex;width:17.25rem;flex-direction:column;justify-content:space-between;overflow-y:auto;padding:1.2rem;color:#e8ede9;background:#17221c;border-right:1px solid #344139;transition:transform .25s cubic-bezier(.22,1,.36,1)}
  .sidebar-main{min-height:0}.sidebar-brand{display:flex;align-items:center;justify-content:space-between;padding:.45rem .4rem 1.7rem}.brand-lockup{display:flex;align-items:center;gap:.75rem}.brand-lockup>span:last-child{display:flex;flex-direction:column}.brand-lockup strong{font-size:.95rem;letter-spacing:-.03em}.brand-lockup small{margin-top:.1rem;color:#8f9b92;font-size:.55rem;font-weight:700;letter-spacing:.1em;text-transform:uppercase}
  .brand-mark{display:inline-flex;align-items:end;gap:2px;width:2rem;height:2rem;padding:6px;border:1px solid #77837a;border-radius:50%}.brand-mark i{display:block;width:3px;border-radius:2px;background:#c7f464}.brand-mark i:nth-child(1){height:35%}.brand-mark i:nth-child(2){height:82%}.brand-mark i:nth-child(3){height:58%}.close-button{display:grid;width:2rem;height:2rem;place-items:center;border:1px solid #465249;border-radius:50%;color:#b8c0ba}.close-button :global(svg){width:1rem}
  .workspace-field{margin-bottom:1.6rem;padding:.8rem;border:1px solid #3c4941;background:#202a23}.workspace-field label,.nav-kicker{display:block;color:#8f9b92;font-size:.56rem;font-weight:800;letter-spacing:.13em;text-transform:uppercase}.workspace-field select{width:100%;margin-top:.5rem;color:#f3f0e8;background:transparent;font-size:.72rem;font-weight:700;outline:none}.workspace-field option{color:#17221c;background:#fbfaf6}.nav-kicker{padding:0 .65rem .6rem}
  .sidebar-nav{display:flex;flex-direction:column;gap:.15rem}.sidebar-nav a{position:relative;display:grid;grid-template-columns:1.25rem 1fr auto;align-items:center;gap:.75rem;min-height:2.65rem;padding:.55rem .7rem;border-radius:.35rem;color:#aeb8b1;font-size:.72rem;font-weight:700;transition:color .18s,background .18s}.sidebar-nav a:hover{color:#f3f0e8;background:#202a23}.sidebar-nav a.active{color:#17221c;background:#c7f464}.sidebar-nav :global(svg){width:1.05rem}.sidebar-nav a i{width:.35rem;height:.35rem;border-radius:50%;background:#17221c}
  .sidebar-foot{display:flex;flex-direction:column;gap:.55rem;padding-top:2rem}.sidebar-foot>a{display:flex;align-items:center;gap:.7rem;padding:.8rem;border:1px solid #3c4941;background:#202a23}.sidebar-foot>a :global(svg){width:1.05rem;color:#c7f464}.sidebar-foot>a span{display:flex;flex-direction:column}.sidebar-foot>a strong{font-size:.65rem}.sidebar-foot>a small{margin-top:.12rem;color:#8f9b92;font-size:.54rem}.sidebar-foot .settings-link{border:0;background:transparent;color:#aeb8b1;font-size:.65rem}.sidebar-foot .settings-link :global(svg){color:#8f9b92}.sidebar-foot>p{padding:0 .8rem;color:#aeb8b1;font-family:'Newsreader Variable';font-size:.7rem;font-style:italic}
  @media(min-width:768px){.sidebar-shell{position:sticky;top:0;z-index:auto;flex-shrink:0;min-height:100vh;transform:none}}
</style>
