<script lang="ts">
  import { BookOpen, Search, Youtube, ExternalLink } from 'lucide-svelte';
  import { researchApi, type ResearchPackageDto } from '$lib/api';

  let searchQuery = '';
  let results: ResearchPackageDto | null = null;
  let loading = false;
  let error = '';

  $: academicSources = results?.sources.filter((source) => source.sourceType === 'Academic') ?? [];
  $: videoSources = results?.sources.filter((source) => source.sourceType === 'Video') ?? [];

  async function searchResearch() {
    const topic = searchQuery.trim();
    if (!topic || loading) return;

    loading = true;
    error = '';
    results = null;
    try {
      results = await researchApi.conduct(topic);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Research request failed.';
    } finally {
      loading = false;
    }
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1400px] mx-auto">
  <div>
    <h1 class="text-2xl font-bold text-ifa-text-primary">Research</h1>
    <p class="text-sm text-ifa-text-secondary mt-1">
      Search ScholarXiv for academic sources and YouTube for learning resources. Results come from the configured providers.
    </p>
  </div>

  <form class="flex gap-3" on:submit|preventDefault={searchResearch}>
    <input
      type="search"
      bind:value={searchQuery}
      placeholder="Try: Grade 12 physics mechanics"
      aria-label="Research topic"
      class="flex-1 px-4 py-3 rounded-xl bg-ifa-card border border-ifa-border text-sm text-ifa-text-primary"
    />
    <button
      type="submit"
      disabled={loading || !searchQuery.trim()}
      class="px-5 py-3 bg-ifa-pine text-white rounded-xl text-sm font-semibold flex items-center gap-2 disabled:opacity-50"
    >
      <Search class="w-4 h-4" />
      {loading ? 'Searching…' : 'Research'}
    </button>
  </form>

  {#if error}
    <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>
  {/if}

  {#if results}
    <section class="space-y-5">
      <div>
        <h2 class="text-lg font-bold text-ifa-text-primary">{results.topic}</h2>
        <p class="text-sm text-ifa-text-secondary mt-2">{results.summary}</p>
      </div>

      <div class="flex gap-6 text-sm text-ifa-text-secondary">
        <span>{academicSources.length} academic results</span>
        <span>{videoSources.length} video results</span>
      </div>

      {#if results.sources.length === 0}
        <p class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
          No sources were returned. Check the ScholarXiv API key and try another search phrase.
        </p>
      {:else}
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {#each results.sources as source (source.id)}
            <article class="bg-ifa-card rounded-xl border border-ifa-border p-5">
              <div class="flex items-center gap-2 text-xs uppercase font-semibold text-ifa-text-secondary mb-2">
                {#if source.sourceType === 'Video'}
                  <Youtube class="w-4 h-4 text-red-600" />
                {:else}
                  <BookOpen class="w-4 h-4 text-ifa-pine" />
                {/if}
                {source.sourceType}
              </div>
              <h3 class="text-sm font-bold text-ifa-text-primary">{source.title}</h3>
              {#if source.authors || source.publishedYear}
                <p class="text-xs text-ifa-text-secondary mt-1">
                  {source.authors}{source.publishedYear ? ` · ${source.publishedYear}` : ''}
                </p>
              {/if}
              {#if source.snippet}
                <p class="text-sm text-ifa-text-secondary mt-3">{source.snippet}</p>
              {/if}
              {#if source.url}
                <a
                  href={source.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  class="inline-flex items-center gap-1 mt-4 text-xs font-semibold text-ifa-pine"
                >
                  Open source <ExternalLink class="w-3 h-3" />
                </a>
              {/if}
            </article>
          {/each}
        </div>
      {/if}
    </section>
  {/if}
</div>
