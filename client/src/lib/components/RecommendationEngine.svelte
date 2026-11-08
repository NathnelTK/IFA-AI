<script lang="ts">
  import { Lightbulb, TrendingUp, ArrowRight, Target } from 'lucide-svelte';
  import { goto } from '$app/navigation';

  export let recommendations: Array<{
    id: string;
    type: 'course' | 'skill' | 'module';
    title: string;
    reason: string;
    category: string;
    href: string;
  }> = [];

  function handleAction(recommendation: (typeof recommendations)[number]) {
    if (recommendation?.href) goto(recommendation.href);
  }
</script>

<div class="bg-gradient-to-r from-ifa-pine/10 to-emerald-50 rounded-2xl border border-ifa-pine/20 p-6">
  <div class="flex items-center gap-2 mb-4">
    <div class="w-8 h-8 rounded-lg bg-ifa-pine/20 flex items-center justify-center text-ifa-pine">
      <Lightbulb class="w-4 h-4" />
    </div>
    <h3 class="text-sm font-bold text-ifa-pine">Witforge Recommendations</h3>
  </div>

  <div class="space-y-3">
    {#each recommendations as rec}
      <div class="bg-white rounded-xl border border-ifa-border p-4 hover:shadow-elevated transition">
        <div class="flex items-start gap-3">
          <div class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0 bg-ifa-pine/10 text-ifa-pine">
            {#if rec.type === 'course'}
              <Lightbulb class="w-5 h-5" />
            {:else if rec.type === 'skill'}
              <Target class="w-5 h-5" />
            {:else if rec.type === 'module'}
              <TrendingUp class="w-5 h-5" />
            {:else}
              <Lightbulb class="w-5 h-5" />
            {/if}
          </div>

          <div class="flex-1">
            <div class="flex items-start justify-between mb-1">
              <h4 class="text-sm font-bold text-ifa-text-primary">{rec.title}</h4>
            </div>
            <p class="text-xs text-ifa-text-secondary mb-2">{rec.reason}</p>
            <div class="flex items-center justify-between">
              <span class="text-[10px] text-ifa-text-muted uppercase">{rec.category}</span>
              <button
                type="button"
                on:click={() => handleAction(rec)}
                class="text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition flex items-center gap-1"
              >
                <span>Start</span>
                <ArrowRight class="w-3 h-3" />
              </button>
            </div>
          </div>
        </div>
      </div>
    {/each}
  </div>

  {#if recommendations.length === 0}
    <p class="py-3 text-center text-xs text-ifa-text-secondary">No assessment-backed recommendations yet.</p>
  {/if}

  <div class="mt-4 text-center">
    <button
      type="button"
      on:click={() => goto('/courses')}
      class="text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition"
    >
      Browse all courses →
    </button>
  </div>
</div>
