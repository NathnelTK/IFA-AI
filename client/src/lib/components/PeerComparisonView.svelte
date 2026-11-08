<script lang="ts">
  import { TrendingUp, TrendingDown, Users, X } from 'lucide-svelte';
  import { peerComparison } from '../stores/dashboardStore';

  export let onClose = () => {};
</script>

<div class="bg-ifa-card rounded-3xl border border-ifa-border p-5 shadow-card space-y-4">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div class="flex items-center gap-2">
      <div class="w-6 h-6 rounded-lg bg-purple-100 flex items-center justify-center text-purple-700">
        <Users class="w-3.5 h-3.5" />
      </div>
      <h3 class="text-xs font-bold text-ifa-text-primary tracking-tight">Peer Progress Comparison</h3>
    </div>
    <button
      type="button"
      on:click={onClose}
      aria-label="Close peer comparison"
      class="text-ifa-text-muted hover:text-ifa-pine transition"
    >
      <X class="w-4 h-4" />
    </button>
  </div>

  {#if !$peerComparison.enabled}
    <div class="rounded-2xl border border-ifa-border bg-ifa-card-muted p-5 text-center">
      <p class="text-sm font-semibold text-ifa-text-primary">Not enough peer data</p>
      <p class="mt-1 text-xs text-ifa-text-secondary">A comparison appears after at least one other active learner has persisted progress.</p>
    </div>
  {:else}
  <!-- Aggregate comparison -->
  <div class="p-4 rounded-2xl bg-gradient-to-r from-emerald-50 to-purple-50 border border-purple-100">
    <div class="flex items-center justify-between">
      <div class="text-center flex-1">
        <p class="text-[10px] uppercase font-bold text-emerald-700 tracking-wider mb-1">You</p>
        <p class="text-2xl font-black text-emerald-900">{$peerComparison.userProgress}%</p>
        <div class="flex items-center justify-center gap-1 mt-1">
          {#if ($peerComparison.userProgress ?? 0) > ($peerComparison.peerAverage ?? 0)}
            <TrendingUp class="w-3 h-3 text-emerald-600" />
            <span class="text-[9px] font-bold text-emerald-600">Leading</span>
          {:else}
            <TrendingDown class="w-3 h-3 text-amber-600" />
            <span class="text-[9px] font-bold text-amber-600">Behind</span>
          {/if}
        </div>
      </div>

      <div class="text-[11px] font-black text-ifa-text-muted px-3 py-1 bg-white/80 rounded-md shadow-soft">
        VS
      </div>

      <div class="text-center flex-1">
        <p class="text-[10px] uppercase font-bold text-purple-700 tracking-wider mb-1">Peer average</p>
        <p class="text-2xl font-black text-purple-900">{$peerComparison.peerAverage}%</p>
        <div class="flex items-center justify-center gap-1 mt-1">
          {#if ($peerComparison.peerAverage ?? 0) > ($peerComparison.userProgress ?? 0)}
            <TrendingUp class="w-3 h-3 text-purple-600" />
            <span class="text-[9px] font-bold text-purple-600">Leading</span>
          {:else}
            <TrendingDown class="w-3 h-3 text-amber-600" />
            <span class="text-[9px] font-bold text-amber-600">Behind</span>
          {/if}
        </div>
      </div>
    </div>
  </div>

  <div class="grid grid-cols-2 gap-2 text-center">
    <div class="rounded-xl bg-ifa-card-muted p-3"><p class="text-lg font-bold text-ifa-text-primary">{$peerComparison.rank ?? '—'}</p><p class="text-[10px] text-ifa-text-secondary">Rank of {$peerComparison.totalEnrolled}</p></div>
    <div class="rounded-xl bg-ifa-card-muted p-3"><p class="text-sm font-bold text-ifa-text-primary">{$peerComparison.paceStatus}</p><p class="text-[10px] text-ifa-text-secondary">Based on persisted course progress</p></div>
  </div>
  {/if}
</div>
