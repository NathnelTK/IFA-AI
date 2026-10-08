<script lang="ts">
  import { ArrowRight, TrendingUp, TrendingDown, Users, X } from 'lucide-svelte';
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
      class="text-ifa-text-muted hover:text-ifa-pine transition"
    >
      <X class="w-4 h-4" />
    </button>
  </div>

  <!-- Head-to-Head Comparison -->
  <div class="p-4 rounded-2xl bg-gradient-to-r from-emerald-50 to-purple-50 border border-purple-100">
    <div class="flex items-center justify-between">
      <div class="text-center flex-1">
        <p class="text-[10px] uppercase font-bold text-emerald-700 tracking-wider mb-1">You ({$peerComparison.user})</p>
        <p class="text-2xl font-black text-emerald-900">{$peerComparison.userProgress}%</p>
        <div class="flex items-center justify-center gap-1 mt-1">
          {#if $peerComparison.userProgress > $peerComparison.peerProgress}
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
        <p class="text-[10px] uppercase font-bold text-purple-700 tracking-wider mb-1">Peer ({$peerComparison.peerName})</p>
        <p class="text-2xl font-black text-purple-900">{$peerComparison.peerProgress}%</p>
        <div class="flex items-center justify-center gap-1 mt-1">
          {#if $peerComparison.peerProgress > $peerComparison.userProgress}
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

  <!-- Skill-by-Skill Comparison -->
  <div class="space-y-2">
    <p class="text-[10px] font-bold text-ifa-text-secondary uppercase tracking-wider">Skill Breakdown</p>
    {#each $peerComparison.skillComparison as skill}
      <div class="flex items-center gap-3">
        <div class="flex-1">
          <div class="flex items-center justify-between mb-1">
            <span class="text-[11px] font-semibold text-ifa-text-primary">{skill.name}</span>
            <span class="text-[10px] font-bold text-ifa-text-secondary">{skill.userPercentage}% vs {skill.peerPercentage}%</span>
          </div>
          <div class="flex gap-1">
            <div class="flex-1 h-1.5 rounded-full bg-emerald-200">
              <div class="h-full rounded-full bg-emerald-600" style="width: {skill.userPercentage}%"></div>
            </div>
            <div class="flex-1 h-1.5 rounded-full bg-purple-200">
              <div class="h-full rounded-full bg-purple-600" style="width: {skill.peerPercentage}%"></div>
            </div>
          </div>
        </div>
      </div>
    {/each}
  </div>

  <!-- Actions -->
  <div class="flex gap-2 pt-2">
    <button
      type="button"
      class="flex-1 text-[11px] font-semibold px-3 py-2 rounded-xl bg-ifa-pine text-white hover:bg-emerald-800 transition"
    >
      Share Progress
    </button>
    <button
      type="button"
      class="flex-1 text-[11px] font-semibold px-3 py-2 rounded-xl border border-ifa-border bg-ifa-card-muted hover:bg-white transition"
    >
      Invite Peer
    </button>
  </div>
</div>
