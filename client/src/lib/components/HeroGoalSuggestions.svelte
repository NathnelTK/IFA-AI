<script lang="ts">
  import { goto } from '$app/navigation';
  import { Target, ArrowRight, Zap, BookOpen, Code2, Database, RefreshCw, PlayCircle } from 'lucide-svelte';
  import { recommendations } from '$lib/stores/dashboardStore';
  import type { Recommendation } from '$lib/types';

  export let onSelectGoal: (goal: string) => void = () => {};

  const ICONS = [Code2, Database, BookOpen, PlayCircle, RefreshCw];

  function iconFor(index: number) {
    return ICONS[index % ICONS.length];
  }

  $: goals = $recommendations.slice(0, 3).map((rec: Recommendation, i: number) => ({
    id: rec.id,
    title: rec.title,
    description: rec.subtitle,
    actionUrl: rec.actionUrl,
    color: rec.color,
    estimatedTime: rec.type === 'review' ? 'Review' : rec.type === 'continue' ? 'Continue' : 'Practice',
    icon: iconFor(i)
  }));

  function handleSelectGoal(goal: { title: string; actionUrl?: string }) {
    onSelectGoal(goal.title);
    if (goal.actionUrl) void goto(goal.actionUrl);
  }
</script>

<div class="bg-ifa-bg-warm rounded-2xl border border-ifa-pine/20 p-6">
  <div class="flex items-center gap-2 mb-4">
    <div class="w-8 h-8 rounded-lg bg-ifa-pine/20 flex items-center justify-center text-ifa-pine">
      <Target class="w-4 h-4" />
    </div>
    <h3 class="text-sm font-bold text-ifa-pine">IFA Recommended Goals</h3>
  </div>

  {#if goals.length === 0}
    <p class="text-xs text-ifa-text-secondary">No recommendations yet — complete a lesson and IFA will tailor goals for you.</p>
  {:else}
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      {#each goals as goal}
        <button
          type="button"
          class="text-left bg-ifa-card rounded-xl border border-ifa-border p-4 hover:shadow-elevated transition cursor-pointer"
          on:click={() => handleSelectGoal(goal)}
        >
          <div class="flex items-start justify-between mb-3">
            <div
              class="w-10 h-10 rounded-lg flex items-center justify-center"
              style="background-color: {goal.color}15; color: {goal.color};"
            >
              <svelte:component this={goal.icon} class="w-5 h-5" />
            </div>
            <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-ifa-pine/10 text-ifa-pine">
              {goal.estimatedTime}
            </span>
          </div>

          <h4 class="text-sm font-bold text-ifa-text-primary mb-1 line-clamp-1">{goal.title}</h4>
          <p class="text-xs text-ifa-text-secondary mb-3 line-clamp-2">{goal.description}</p>

          <div class="flex items-center justify-between">
            <div class="flex items-center gap-1 text-xs text-ifa-text-muted">
              <Zap class="w-3 h-3" />
              <span>AI suggested</span>
            </div>
            <ArrowRight class="w-4 h-4 text-ifa-pine" />
          </div>
        </button>
      {/each}
    </div>
  {/if}

  <div class="mt-4 text-center">
    <button
      type="button"
      on:click={() => goto('/recommendations')}
      class="text-xs font-semibold text-ifa-pine hover:text-ifa-pine-dark transition"
    >
      View all recommendations →
    </button>
  </div>
</div>
