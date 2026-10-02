<script lang="ts">
  import { Lightbulb, TrendingUp, ArrowRight, Target } from 'lucide-svelte';
  import { goto } from '$app/navigation';

  const recommendations = [
    {
      id: 'rec-1',
      type: 'course',
      title: 'Advanced Database Optimization',
      reason: 'Based on your SQL progress and quiz performance',
      confidence: 92,
      category: 'Data Science',
      bgColor: '#E07A5F15',
      textColor: '#E07A5F',
      icon: Lightbulb,
      href: '/courses/sql-developers'
    },
    {
      id: 'rec-2',
      type: 'skill',
      title: 'Unit Testing Mastery',
      reason: 'Your Testing skill is below target level',
      confidence: 88,
      category: 'Backend',
      bgColor: '#2A9D6815',
      textColor: '#2A9D68',
      icon: Target,
      href: '/courses/csharp-backend'
    },
    {
      id: 'rec-3',
      type: 'module',
      title: 'API Security Best Practices',
      reason: 'Next logical step in your C# journey',
      confidence: 85,
      category: 'Backend',
      bgColor: '#2A9D6815',
      textColor: '#2A9D68',
      icon: TrendingUp,
      href: '/courses/csharp-backend'
    }
  ];

  function handleAction(recommendation: any) {
    if (recommendation?.href) goto(recommendation.href);
  }
</script>

<div class="bg-gradient-to-r from-ifa-pine/10 to-emerald-50 rounded-2xl border border-ifa-pine/20 p-6">
  <div class="flex items-center gap-2 mb-4">
    <div class="w-8 h-8 rounded-lg bg-ifa-pine/20 flex items-center justify-center text-ifa-pine">
      <Lightbulb class="w-4 h-4" />
    </div>
    <h3 class="text-sm font-bold text-ifa-pine">IFA Recommendations</h3>
  </div>

  <div class="space-y-3">
    {#each recommendations as rec}
      <div class="bg-white rounded-xl border border-ifa-border p-4 hover:shadow-elevated transition">
        <div class="flex items-start gap-3">
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
            style="background-color: {rec.bgColor}; color: {rec.textColor};"
          >
            {#if rec.icon === Lightbulb}
              <Lightbulb class="w-5 h-5" />
            {:else if rec.icon === Target}
              <Target class="w-5 h-5" />
            {:else if rec.icon === TrendingUp}
              <TrendingUp class="w-5 h-5" />
            {:else}
              <Lightbulb class="w-5 h-5" />
            {/if}
          </div>

          <div class="flex-1">
            <div class="flex items-start justify-between mb-1">
              <h4 class="text-sm font-bold text-ifa-text-primary">{rec.title}</h4>
              <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-700">
                {rec.confidence}% match
              </span>
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
