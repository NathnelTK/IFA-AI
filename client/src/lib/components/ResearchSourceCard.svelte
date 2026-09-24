<script lang="ts">
  import { ExternalLink, Plus, BookOpen, FileText } from 'lucide-svelte';

  export let source: any;
  export let onAddToPath = () => {};
  export let onOpen = () => {};

  function getTypeIcon(type: string) {
    switch (type) {
      case 'academic':
        return BookOpen;
      case 'documentation':
        return FileText;
      default:
        return ExternalLink;
    }
  }

  function getTypeColor(type: string) {
    switch (type) {
      case 'academic':
        return 'text-ifa-pine bg-emerald-100';
      case 'documentation':
        return 'text-blue-600 bg-blue-100';
      default:
        return 'text-ifa-text-secondary bg-gray-100';
    }
  }

  function getCredibilityColor(credibility: string) {
    switch (credibility) {
      case 'high':
        return 'text-emerald-600 bg-emerald-50';
      case 'medium':
        return 'text-yellow-600 bg-yellow-50';
      default:
        return 'text-gray-600 bg-gray-50';
    }
  }
</script>

<div class="bg-ifa-card rounded-xl border border-ifa-border p-4 hover:shadow-elevated transition">
  <div class="flex items-start justify-between mb-3">
    <div class="flex-1">
      <div class="flex items-center gap-2 mb-2">
        {#if source.type === 'academic'}
          <div class="w-6 h-6 rounded flex items-center justify-center text-ifa-pine bg-emerald-100">
            <BookOpen class="w-3 h-3" />
          </div>
        {:else if source.type === 'documentation'}
          <div class="w-6 h-6 rounded flex items-center justify-center text-blue-600 bg-blue-100">
            <FileText class="w-3 h-3" />
          </div>
        {:else}
          <div class="w-6 h-6 rounded flex items-center justify-center text-ifa-text-secondary bg-gray-100">
            <ExternalLink class="w-3 h-3" />
          </div>
        {/if}
        <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-ifa-card-muted text-ifa-text-secondary uppercase">
          {source.type}
        </span>
      </div>
      <h4 class="text-sm font-bold text-ifa-text-primary line-clamp-2">{source.title}</h4>
      {#if source.authors}
        <p class="text-xs text-ifa-text-secondary mt-1">{source.authors} ({source.year})</p>
      {/if}
    </div>
    <div class="px-2 py-1 rounded-full text-[10px] font-bold {getCredibilityColor(source.credibility)}">
      {source.credibility}
    </div>
  </div>

  {#if source.abstract}
    <p class="text-xs text-ifa-text-secondary mb-3 line-clamp-3">{source.abstract}</p>
  {:else if source.description}
    <p class="text-xs text-ifa-text-secondary mb-3 line-clamp-3">{source.description}</p>
  {/if}

  <div class="flex gap-2">
    <button
      type="button"
      on:click={onAddToPath}
      class="flex-1 py-2 bg-ifa-pine text-white rounded-lg text-xs font-semibold flex items-center justify-center gap-1.5 hover:bg-emerald-800 transition"
    >
      <Plus class="w-3 h-3" />
      <span>Add to Path</span>
    </button>
    <button
      type="button"
      on:click={onOpen}
      class="px-3 py-2 border border-ifa-border rounded-lg text-xs font-semibold text-ifa-text-secondary hover:bg-ifa-card-muted transition"
    >
      <ExternalLink class="w-3 h-3" />
    </button>
  </div>
</div>
