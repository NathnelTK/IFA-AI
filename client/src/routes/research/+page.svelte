<script lang="ts">
  import { onMount } from 'svelte';
  import { BookOpen, Search, Youtube, ExternalLink, Image as ImageIcon, BarChart3, Loader2 } from 'lucide-svelte';
  import { researchApi, coursesApi, type ResearchPackageDto, type CourseSummaryDto } from '$lib/api';
  import { initSession } from '$lib/stores/sessionStore';

  let myCourses: CourseSummaryDto[] = [];
  let selectedCourseId = '';
  let results: ResearchPackageDto | null = null;

  let searchQuery = '';
  let loading = false;      // ad-hoc "research again"
  let courseLoading = false; // loading a course's existing research
  let error = '';

  $: academicSources = results?.sources.filter((s) => s.sourceType === 'Academic') ?? [];
  $: videoSources = results?.sources.filter((s) => s.sourceType === 'Video') ?? [];
  $: imageSources = results?.sources.filter((s) => s.sourceType === 'Image' || s.sourceType === 'Graph') ?? [];

  onMount(async () => {
    try {
      await initSession();
      myCourses = await coursesApi.listMine().catch(() => []);
      if (myCourses.length > 0) {
        selectedCourseId = myCourses[0].id;
        await loadCourseResearch();
      }
    } catch {
      /* no-op */
    }
  });

  async function loadCourseResearch() {
    if (!selectedCourseId) return;
    courseLoading = true;
    error = '';
    try {
      results = await researchApi.forCourse(selectedCourseId);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load research for this course.';
    } finally {
      courseLoading = false;
    }
  }

  async function searchResearch() {
    const topic = searchQuery.trim();
    if (!topic || loading) return;
    loading = true;
    error = '';
    try {
      // Attaches to the selected course so it is added to the research the
      // system uses to (re)generate that course's modules.
      results = await researchApi.conduct(topic, selectedCourseId || undefined);
      searchQuery = '';
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
      These are the sources IFA used to build your courses — papers, videos, images and diagrams.
      You can run more research to add to what powers a course's modules.
    </p>
  </div>

  <!-- Course picker: show the research used for a specific course -->
  {#if myCourses.length > 0}
    <div class="flex flex-col sm:flex-row sm:items-center gap-3">
      <label for="course-select" class="text-sm font-semibold text-ifa-text-primary">Research for</label>
      <select
        id="course-select"
        bind:value={selectedCourseId}
        on:change={loadCourseResearch}
        class="flex-1 max-w-md px-4 py-2.5 rounded-lg bg-ifa-card border border-ifa-border text-sm text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      >
        {#each myCourses as course}
          <option value={course.id}>{course.title}</option>
        {/each}
      </select>
      {#if courseLoading}
        <span class="flex items-center gap-1 text-xs text-ifa-text-muted"><Loader2 class="w-3.5 h-3.5 animate-spin" /> Loading…</span>
      {/if}
    </div>
  {/if}

  <!-- Run new research (adds to the selected course's sources) -->
  <form class="flex gap-3" on:submit|preventDefault={searchResearch}>
    <input
      type="search"
      bind:value={searchQuery}
      placeholder="Research again — e.g. clean architecture patterns"
      aria-label="Research topic"
      class="flex-1 px-4 py-3 rounded-xl bg-ifa-card border border-ifa-border text-sm text-ifa-text-primary"
    />
    <button
      type="submit"
      disabled={loading || !searchQuery.trim()}
      class="px-5 py-3 bg-ifa-pine text-white rounded-xl text-sm font-semibold flex items-center gap-2 disabled:opacity-50"
    >
      {#if loading}<Loader2 class="w-4 h-4 animate-spin" />{:else}<Search class="w-4 h-4" />{/if}
      {loading ? 'Researching…' : 'Research'}
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

      <div class="flex flex-wrap gap-x-6 gap-y-1 text-sm text-ifa-text-secondary">
        <span>{academicSources.length} papers</span>
        <span>{videoSources.length} videos</span>
        <span>{imageSources.length} images &amp; diagrams</span>
      </div>

      {#if results.sources.length === 0}
        <p class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
          No sources yet. Run a research query above to populate this course's sources.
        </p>
      {:else}
        <!-- Images & diagrams the system embeds in lessons -->
        {#if imageSources.length > 0}
          <div>
            <h3 class="text-sm font-bold text-ifa-text-primary mb-3 flex items-center gap-2">
              <ImageIcon class="w-4 h-4 text-ifa-pine" /> Images &amp; Diagrams
            </h3>
            <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
              {#each imageSources as source (source.id)}
                <a
                  href={source.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  class="group bg-ifa-card rounded-xl border border-ifa-border overflow-hidden hover:shadow-elevated transition"
                >
                  <div class="aspect-video bg-black/5 overflow-hidden">
                    <img src={source.url} alt={source.title} loading="lazy" class="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                  </div>
                  <div class="p-2.5 flex items-center gap-1.5">
                    {#if source.sourceType === 'Graph'}
                      <BarChart3 class="w-3.5 h-3.5 text-ifa-accent-purple shrink-0" />
                    {:else}
                      <ImageIcon class="w-3.5 h-3.5 text-ifa-pine shrink-0" />
                    {/if}
                    <span class="text-[11px] text-ifa-text-secondary line-clamp-1">{source.title}</span>
                  </div>
                </a>
              {/each}
            </div>
          </div>
        {/if}

        <!-- Papers & videos -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {#each [...academicSources, ...videoSources] as source (source.id)}
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
