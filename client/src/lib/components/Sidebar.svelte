<script lang="ts">
  import {
    Home,
    GraduationCap,
    BookOpen,
    Globe2,
    Bot,
    CheckCircle2,
    Search,
    TrendingUp,
    Settings,
    Lightbulb
  } from 'lucide-svelte';

  export let activeTab = 'home';

  const navItems = [
    { id: 'home', label: 'Home', icon: Home, href: '/home' },
    { id: 'my-learning', label: 'My Learning', icon: GraduationCap, href: '/my-learning' },
    { id: 'courses', label: 'Courses', icon: BookOpen, href: '/courses' },
    { id: 'marketplace', label: 'Marketplace', icon: Globe2, href: '/marketplace' },
    { id: 'recommendations', label: 'Recommendations', icon: Lightbulb, href: '/recommendations' },
    { id: 'ai-tutor', label: 'AI Tutor', icon: Bot, href: '/ai-tutor' },
    { id: 'my-skills', label: 'My Skills', icon: CheckCircle2, href: '/my-skills' },
    { id: 'research', label: 'Research', icon: Search, href: '/research' },
    { id: 'progress', label: 'Progress', icon: TrendingUp, href: '/progress' },
    { id: 'settings', label: 'Settings', icon: Settings, href: '/settings' },
  ];
</script>

<aside class="w-64 min-h-screen bg-ifa-sidebar border-r border-ifa-border flex flex-col justify-between p-5 select-none shrink-0">
  <div>
    <!-- Logo & Brand -->
    <div class="flex items-center gap-3 px-3 py-2 mb-6">
      <div class="w-8 h-8 rounded-lg bg-ifa-pine flex items-center justify-center text-white shadow-sm">
        <GraduationCap class="w-4 h-4 text-emerald-300" />
      </div>
      <div>
        <div class="flex items-center gap-1.5 font-bold text-xl tracking-tight text-ifa-pine">
          <span>✦ IFA</span>
        </div>
        <p class="text-[11px] text-ifa-text-secondary font-medium tracking-wide">Your AI Learning Companion</p>
      </div>
    </div>

    <!-- Navigation List -->
    <nav class="space-y-1">
      {#each navItems as item}
        {@const isActive = activeTab === item.id}
        <a
          href={item.href}
          class="flex items-center gap-3.5 px-4 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 {isActive
            ? 'bg-ifa-pine text-white shadow-sm font-semibold'
            : 'text-ifa-text-secondary hover:text-ifa-pine hover:bg-black/5'}"
        >
          <svelte:component this={item.icon} class="w-4 h-4 {isActive ? 'text-emerald-300' : 'text-ifa-text-muted'}" />
          <span>{item.label}</span>
        </a>
      {/each}
    </nav>
  </div>

  <!-- Bottom Promotional / Inspirational Card -->
  <div class="relative overflow-hidden rounded-2xl p-4 bg-gradient-to-b from-[#EAE4D9] to-[#DFD8CC] border border-ifa-border shadow-soft">
    <!-- Mountain illustration silhouette -->
    <div class="absolute inset-0 opacity-20 pointer-events-none bg-[radial-gradient(#1B3D2F_1px,transparent_1px)] [background-size:12px_12px]"></div>
    
    <div class="relative z-10">
      <div class="h-10 flex items-center mb-1">
        <svg viewBox="0 0 120 40" class="w-full h-8 text-ifa-pine/30 fill-current">
          <polygon points="10,35 35,15 60,35" />
          <polygon points="45,35 75,10 105,35" />
          <polygon points="70,35 90,20 115,35" />
        </svg>
      </div>
      <p class="text-xs font-semibold text-ifa-pine leading-tight">Better Learning.<br />Bigger Dreams.</p>
      <div class="mt-2.5 flex items-center gap-1.5">
        <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
        <span class="text-[10px] text-ifa-text-secondary font-medium">IFA AI Active</span>
      </div>
    </div>
  </div>
</aside>
