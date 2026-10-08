<script lang="ts">
  import { ArrowRight, ChevronRight, Lightbulb } from 'lucide-svelte';
  import { recommendations } from '../stores/dashboardStore';
  import { getRecommendationIcon } from '$lib/icons';

  export let onSelectRecommendation = (actionUrl: string) => {};

  // Home page shows only a preview; the full list lives on /recommendations.
  const HOME_PREVIEW_COUNT = 3;
  $: previewRecommendations = $recommendations.slice(0, HOME_PREVIEW_COUNT);
</script>

<div class="bg-ifa-card rounded-3xl border border-ifa-border p-5 shadow-card space-y-3">
  <!-- Title & Action -->
  <div class="flex items-center justify-between">
    <div class="flex items-center gap-2">
      <div class="w-6 h-6 rounded-lg bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
        <Lightbulb class="w-3.5 h-3.5 text-emerald-700" />
      </div>
      <h3 class="text-xs font-bold text-ifa-text-primary tracking-tight">IFA Recommends</h3>
    </div>
    <a href="/recommendations" class="text-[11px] font-semibold text-ifa-text-muted hover:text-ifa-pine transition flex items-center gap-0.5">
      <span>View all</span>
      <ArrowRight class="w-3 h-3" />
    </a>
  </div>

  <!-- Recommendation Items -->
  <div class="space-y-2">
    {#each previewRecommendations as rec}
      {@const Icon = getRecommendationIcon(rec.type)}
      <button
        type="button"
        on:click={() => onSelectRecommendation(rec.actionUrl)}
        class="w-full text-left p-2.5 rounded-2xl border border-ifa-border hover:border-ifa-pine/30 bg-ifa-card-muted/50 hover:bg-white transition flex items-center justify-between group shadow-soft"
      >
        <div class="flex items-center gap-3">
          <!-- Icon Box -->
          <div
            class="w-8 h-8 rounded-xl flex items-center justify-center shrink-0"
            style="background-color: {rec.color}15; color: {rec.color};"
          >
            <svelte:component this={Icon} class="w-4 h-4" />
          </div>

          <div>
            <h5 class="text-xs font-bold text-ifa-text-primary group-hover:text-ifa-pine transition leading-tight">
              {rec.title}
            </h5>
            <p class="text-[10px] text-ifa-text-secondary mt-0.5">{rec.subtitle}</p>
          </div>
        </div>

        <ChevronRight class="w-3.5 h-3.5 text-ifa-text-muted group-hover:text-ifa-pine group-hover:translate-x-0.5 transition-all shrink-0 ml-2" />
      </button>
    {/each}
  </div>
</div>
