<script lang="ts">
  import { Search, Bell, Sun, ChevronDown } from 'lucide-svelte';
  import { userProfile } from '../stores/dashboardStore';
</script>

<header class="h-20 px-8 flex items-center justify-between border-b border-ifa-border bg-ifa-bg/80 backdrop-blur-md sticky top-0 z-20">
  <!-- Greeting -->
  <div>
    <h1 class="text-xl font-bold tracking-tight text-ifa-text-primary flex items-center gap-2">
      {$userProfile.greeting}
    </h1>
    <p class="text-xs text-ifa-text-secondary font-medium">{$userProfile.tagline}</p>
  </div>

  <!-- Search & Profile Controls -->
  <div class="flex items-center gap-4">
    <!-- Search Bar -->
    <div class="relative w-72">
      <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-ifa-text-muted">
        <Search class="w-4 h-4" />
      </div>
      <input
        type="text"
        placeholder="Search anything..."
        class="w-full pl-10 pr-12 py-2 text-xs rounded-xl bg-ifa-card border border-ifa-border text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine transition shadow-soft"
      />
      <div class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
        <kbd class="px-1.5 py-0.5 text-[10px] font-semibold text-ifa-text-muted bg-ifa-bg rounded border border-ifa-border">⌘K</kbd>
      </div>
    </div>

    <!-- Notification Bell -->
    <button class="w-9 h-9 rounded-xl bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine hover:bg-ifa-card-muted transition shadow-soft relative">
      <Bell class="w-4 h-4" />
      <span class="absolute top-2 right-2 w-1.5 h-1.5 bg-ifa-accent-orange rounded-full"></span>
    </button>

    <!-- Theme Mode Toggle -->
    <button class="w-9 h-9 rounded-xl bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine hover:bg-ifa-card-muted transition shadow-soft">
      <Sun class="w-4 h-4" />
    </button>

    <!-- User Profile Dropdown -->
    <div class="flex items-center gap-2.5 pl-2 cursor-pointer group">
      <img
        src="https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&auto=format&fit=crop&q=80"
        alt="Nathnel"
        class="w-9 h-9 rounded-full object-cover border border-ifa-border group-hover:ring-2 group-hover:ring-ifa-pine/30 transition"
      />
      <div class="text-left hidden sm:block">
        <p class="text-xs font-semibold text-ifa-text-primary leading-tight">Nathnel</p>
        <p class="text-[11px] text-ifa-text-muted">Learner</p>
      </div>
      <ChevronDown class="w-3.5 h-3.5 text-ifa-text-muted group-hover:text-ifa-pine transition" />
    </div>
  </div>
</header>
