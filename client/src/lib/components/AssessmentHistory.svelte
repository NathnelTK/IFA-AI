<script lang="ts">
  import { Award, Clock, ArrowRight } from 'lucide-svelte';

  export let assessments: any[] = [];
  export let onViewDetails = (id: string) => {};

  function getScoreColor(score: number) {
    if (score >= 90) return 'text-emerald-600 bg-emerald-100';
    if (score >= 70) return 'text-blue-600 bg-blue-100';
    return 'text-orange-600 bg-orange-100';
  }
</script>

<div class="space-y-3">
  {#each assessments as assessment}
    <div class="bg-ifa-card rounded-xl border border-ifa-border p-4 hover:shadow-elevated transition">
      <div class="flex items-start justify-between">
        <div class="flex-1">
          <div class="flex items-center gap-2 mb-2">
            {#if assessment.type === 'quiz'}
              <div class="w-8 h-8 rounded-lg flex items-center justify-center text-ifa-pine bg-emerald-100">
                <Clock class="w-4 h-4" />
              </div>
            {:else}
              <div class="w-8 h-8 rounded-lg flex items-center justify-center text-purple-600 bg-purple-100">
                <Award class="w-4 h-4" />
              </div>
            {/if}
            <div>
              <h4 class="text-sm font-bold text-ifa-text-primary">{assessment.title}</h4>
              <p class="text-xs text-ifa-text-secondary">{assessment.course}</p>
            </div>
          </div>
          <div class="text-right">
            <div class="text-2xl font-black {getScoreColor(assessment.score)} px-3 py-1 rounded-lg">
              {assessment.score}%
            </div>
          </div>
        </div>
      </div>

      <div class="flex items-center justify-between mt-3 pt-3 border-t border-ifa-border">
        <div class="flex items-center gap-4 text-xs text-ifa-text-muted">
          <div class="flex items-center gap-1">
            <Clock class="w-3 h-3" />
            <span>{assessment.duration}</span>
          </div>
          <span>{assessment.date}</span>
        </div>
        <button
          type="button"
          on:click={() => onViewDetails(assessment.id)}
          class="text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition flex items-center gap-1"
        >
          <span>View Details</span>
          <ArrowRight class="w-3 h-3" />
        </button>
      </div>
    </div>
  {/each}
</div>
