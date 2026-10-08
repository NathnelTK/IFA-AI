<script lang="ts">
  import { Clock, CheckCircle, Loader2 } from 'lucide-svelte';

  export let researchHistory: any[] = [];
  export let onSelect = (id: string) => {};
</script>

<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
  {#each researchHistory as research}
    <button
      type="button"
      class="bg-ifa-card rounded-xl border border-ifa-border p-4 hover:shadow-elevated transition text-left w-full"
      on:click={() => onSelect(research.id)}
    >
      <div class="flex items-start justify-between mb-3">
        <div class="flex-1">
          <h3 class="text-sm font-bold text-ifa-text-primary line-clamp-2">{research.title}</h3>
          <p class="text-xs text-ifa-text-secondary mt-1">{research.courseTitle}</p>
        </div>
        {#if research.status === 'completed'}
          <div class="w-8 h-8 rounded-full flex items-center justify-center text-emerald-600 bg-emerald-100">
            <CheckCircle class="w-4 h-4" />
          </div>
        {:else if research.status === 'in-progress'}
          <div class="w-8 h-8 rounded-full flex items-center justify-center text-blue-600 bg-blue-100">
            <Loader2 class="w-4 h-4 animate-spin" />
          </div>
        {:else}
          <div class="w-8 h-8 rounded-full flex items-center justify-center text-gray-600 bg-gray-100">
            <Clock class="w-4 h-4" />
          </div>
        {/if}
      </div>

      <div class="flex items-center gap-4 text-xs text-ifa-text-secondary mb-3">
        <div class="flex items-center gap-1">
          <span class="font-semibold">{research.scholarxivCount}</span>
          <span>Academic</span>
        </div>
        <div class="flex items-center gap-1">
          <span class="font-semibold">{research.webCount}</span>
          <span>Web</span>
        </div>
        <div class="flex items-center gap-1">
          <span class="font-semibold">{research.youtubeCount}</span>
          <span>YouTube</span>
        </div>
      </div>

      <div class="flex items-center gap-1 text-[10px] text-ifa-text-muted">
        <Clock class="w-3 h-3" />
        <span>{research.timestamp}</span>
      </div>
    </button>
  {/each}
</div>
