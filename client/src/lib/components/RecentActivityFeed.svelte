<script lang="ts">
  import { ArrowRight, CheckSquare2, BookMarked, Search, Mic, Sparkles } from 'lucide-svelte';
  import { recentActivities } from '../stores/dashboardStore';
</script>

<div class="bg-ifa-card rounded-3xl border border-ifa-border p-5 shadow-card space-y-3">
  <!-- Title & Action -->
  <div class="flex items-center justify-between">
    <div class="flex items-center gap-2">
      <div class="w-6 h-6 rounded-lg bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
        <Sparkles class="w-3.5 h-3.5 text-emerald-700" />
      </div>
      <h3 class="text-xs font-bold text-ifa-text-primary tracking-tight">Recent Activity</h3>
    </div>
    <a href="/activity" class="text-[11px] font-semibold text-ifa-text-muted hover:text-ifa-pine transition flex items-center gap-0.5">
      <span>View all</span>
      <ArrowRight class="w-3 h-3" />
    </a>
  </div>

  <!-- Activity Items -->
  <div class="space-y-2.5">
    {#each $recentActivities as act}
      <div class="flex items-center justify-between text-xs py-1 border-b border-ifa-border-light last:border-0">
        <div class="flex items-center gap-3">
          <div class="w-7 h-7 rounded-xl bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-pine shrink-0">
            {#if act.iconType === 'quiz'}
              <CheckSquare2 class="w-3.5 h-3.5 text-emerald-600" />
            {:else if act.iconType === 'module'}
              <BookMarked class="w-3.5 h-3.5 text-ifa-accent-amber" />
            {:else if act.iconType === 'research'}
              <Search class="w-3.5 h-3.5 text-ifa-accent-purple" />
            {:else}
              <Mic class="w-3.5 h-3.5 text-ifa-accent-orange" />
            {/if}
          </div>

          <div>
            <p class="text-xs font-bold text-ifa-text-primary leading-tight">{act.title}</p>
            <p class="text-[10px] text-ifa-text-secondary mt-0.5">{act.detail}</p>
          </div>
        </div>

        <span class="text-[10px] font-mono text-ifa-text-muted shrink-0">{act.timeAgo}</span>
      </div>
    {/each}
  </div>
</div>
