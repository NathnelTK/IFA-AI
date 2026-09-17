<script lang="ts">
  import { ArrowRight, Terminal, Database, Network, KeyRound, CheckCircle2, Users } from 'lucide-svelte';
  import { overallProgress, skillsList, peerComparison } from '../stores/dashboardStore';

  const iconMap: Record<string, any> = {
    Terminal,
    Database,
    Network,
    KeyRound,
    CheckCircle2
  };

  // Donut SVG circumference calculation
  const radius = 42;
  const circumference = 2 * Math.PI * radius;
  $: strokeDashoffset = circumference - ($overallProgress / 100) * circumference;
</script>

<div class="bg-ifa-card rounded-3xl border border-ifa-border p-5 shadow-card space-y-4">
  <!-- Title & Action -->
  <div class="flex items-center justify-between">
    <div class="flex items-center gap-2">
      <div class="w-2 h-2 rounded-full bg-ifa-pine"></div>
      <h3 class="text-xs font-bold text-ifa-text-primary tracking-tight">Your Learning Overview</h3>
    </div>
    <div class="flex items-center gap-2">
      <!-- Peer Comparison Mode Toggle -->
      <button
        type="button"
        on:click={() => ($peerComparison.enabled = !$peerComparison.enabled)}
        title="Toggle peer comparison"
        class="text-[10px] font-semibold px-2 py-0.5 rounded-full border transition flex items-center gap-1 {$peerComparison.enabled
          ? 'bg-ifa-accent-purple text-white border-transparent'
          : 'bg-ifa-card-muted text-ifa-text-secondary border-ifa-border hover:text-ifa-pine'}"
      >
        <Users class="w-3 h-3" />
        <span>{$peerComparison.enabled ? 'Peer Mode' : 'Compare'}</span>
      </button>

      <a href="/progress" class="text-[11px] font-semibold text-ifa-text-muted hover:text-ifa-pine transition flex items-center gap-0.5">
        <span>View all</span>
        <ArrowRight class="w-3 h-3" />
      </a>
    </div>
  </div>

  {#if $peerComparison.enabled}
    <!-- Peer Comparison Summary Card -->
    <div class="p-3 rounded-2xl bg-gradient-to-r from-emerald-50 to-purple-50 border border-purple-100 flex items-center justify-between">
      <div class="text-left">
        <p class="text-[10px] uppercase font-bold text-emerald-700 tracking-wider">You (Nathnel)</p>
        <p class="text-lg font-black text-emerald-900">{$peerComparison.userProgress}%</p>
      </div>
      <div class="text-[11px] font-black text-ifa-text-muted px-2 py-0.5 bg-white/80 rounded-md shadow-soft">
        VS
      </div>
      <div class="text-right">
        <p class="text-[10px] uppercase font-bold text-purple-700 tracking-wider">Peer ({$peerComparison.peerName})</p>
        <p class="text-lg font-black text-purple-900">{$peerComparison.peerProgress}%</p>
      </div>
    </div>
  {/if}

  <!-- Donut Chart & Overall Score -->
  <div class="flex flex-col items-center justify-center py-2">
    <div class="relative w-28 h-28 flex items-center justify-center">
      <svg class="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
        <!-- Background Track -->
        <circle
          cx="50"
          cy="50"
          r={radius}
          class="text-[#EFECE6] stroke-current"
          stroke-width="8"
          fill="transparent"
        />
        <!-- Active Progress Stroke -->
        <circle
          cx="50"
          cy="50"
          r={radius}
          class="text-emerald-700 stroke-current transition-all duration-1000 ease-out"
          stroke-width="8"
          stroke-linecap="round"
          fill="transparent"
          stroke-dasharray={circumference}
          stroke-dashoffset={strokeDashoffset}
        />
      </svg>

      <!-- Center Percentage -->
      <div class="absolute flex flex-col items-center justify-center text-center">
        <span class="text-2xl font-black text-ifa-pine tracking-tight">{$overallProgress}%</span>
      </div>
    </div>
    <span class="text-[11px] font-semibold text-ifa-text-secondary mt-1 tracking-wide">Overall Progress</span>
  </div>

  <!-- Skill Breakdown List -->
  <div class="space-y-2.5 pt-1">
    {#each $skillsList as skill}
      {@const Icon = iconMap[skill.iconName] || Terminal}
      <div class="flex items-center justify-between text-xs">
        <div class="flex items-center gap-2.5">
          <div
            class="w-6 h-6 rounded-md flex items-center justify-center"
            style="background-color: {skill.color}15; color: {skill.color};"
          >
            <svelte:component this={Icon} class="w-3.5 h-3.5" />
          </div>
          <span class="font-semibold text-ifa-text-primary text-[11px]">{skill.name}</span>
          {#if skill.isWeakArea}
            <span class="text-[9px] font-bold px-1.5 py-0.2 rounded bg-red-100 text-red-700">Weak Area</span>
          {/if}
        </div>
        <span class="font-bold text-[11px] text-ifa-text-secondary">{skill.percentage}%</span>
      </div>
    {/each}
  </div>
</div>
