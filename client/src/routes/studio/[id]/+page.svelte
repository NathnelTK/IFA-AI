<script lang="ts">
  import { onDestroy, onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import {
    Loader2,
    ArrowLeft,
    Plus,
    Trash2,
    Eye,
    EyeOff,
    Sparkles,
    AlertTriangle,
    FileText,
    Youtube,
    Globe,
    BookOpen,
    Check,
    RefreshCw,
    Image as ImageIcon,
    ExternalLink,
    Rocket,
    ShieldAlert,
    X,
    CircleDashed,
    Circle,
    CircleCheck,
    CircleX
  } from 'lucide-svelte';
  import { educatorApi, type EducatorProjectDetailDto, type EducatorSourceDto, type EducatorConflictDto, type EducatorGenerationStatusDto, type CourseBlueprint } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';
  import CourseEditor from '$lib/components/studio/CourseEditor.svelte';

  export let id: string;

  let project: EducatorProjectDetailDto | null = null;
  let loading = true;
  let error = '';
  let notice = '';
  let busy = ''; // which action is running, e.g. 'analyze', 'generate-blueprint', source ids…

  // Source panel state
  let sourceUrl = '';
  let suggestions: EducatorSourceDto[] = [];
  let suggesting = false;

  // Conflict resolution state
  let conflictSelections: Record<string, string> = {};
  let conflictNotes: Record<string, string> = {};

  // Blueprint state
  let blueprint: CourseBlueprint | null = null;

  // Generation progress state
  let status: EducatorGenerationStatusDto | null = null;
  let pollTimer: number | null = null;

  // Cover state
  let coverUrlInput = '';
  let coverCandidates: Array<{ url: string; title: string }> = [];
  let findingCover = false;
  let savedCoverUrl = '';

  // Publish state
  let publishPaid = false;
  let publishPrice = '';
  let publishCurrency = 'ETB';
  let published = false;

  $: unresolvedConflicts = project?.conflicts.filter((c) => !c.resolution) ?? [];

  onMount(loadProject);

  onDestroy(() => {
    if (pollTimer !== null) window.clearInterval(pollTimer);
  });

  async function loadProject() {
    error = '';
    try {
      project = await educatorApi.project(id);
      blueprint = parseBlueprint(project.blueprint?.json ?? null);
      // If generation already started, resume polling.
      if (project.generationJobs.length > 0 && !isTerminalGeneration(project)) {
        startPolling();
      } else if (project.generationJobs.length > 0) {
        await loadStatus();
      }
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load the project.';
    } finally {
      loading = false;
    }
  }

  function parseBlueprint(json: string | null): CourseBlueprint | null {
    if (!json) return null;
    try {
      return JSON.parse(json) as CourseBlueprint;
    } catch {
      return null;
    }
  }

  function isTerminalGeneration(p: EducatorProjectDetailDto): boolean {
    const statuses = p.generationJobs.map((j) => j.status?.toLowerCase());
    return statuses.every((s) => s === 'done' || s === 'completed' || s === 'succeeded' || s === 'failed');
  }

  /* ------------------------------------------------------------------ */
  /* Sources                                                             */
  /* ------------------------------------------------------------------ */

  function sourceIcon(type: string) {
    const t = type?.toLowerCase() ?? '';
    if (t.includes('video') || t.includes('youtube')) return Youtube;
    if (t.includes('web') || t.includes('site') || t.includes('url')) return Globe;
    return FileText;
  }

  async function handleAddSource() {
    const url = sourceUrl.trim();
    if (!url || busy) return;
    busy = 'add-source';
    error = '';
    try {
      // The server auto-detects the type (PDF/video/website/article/slides).
      const source = await educatorApi.addSource(id, { url });
      sourceUrl = '';
      project?.sources.push(source);
      notice = `Source added — detected as ${source.type || 'link'}.`;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not add the source.';
    } finally {
      busy = '';
    }
  }

  async function handleDeleteSource(sourceId: string) {
    if (busy) return;
    busy = sourceId;
    error = '';
    try {
      await educatorApi.deleteSource(id, sourceId);
      if (project) {
        project.sources = project.sources.filter((s) => s.id !== sourceId);
        project.sources = [...project.sources];
      }
      suggestions = suggestions.filter((s) => s.id !== sourceId);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not remove the source.';
    } finally {
      busy = '';
    }
  }

  async function handleToggleIncluded(source: EducatorSourceDto) {
    if (busy) return;
    busy = source.id;
    error = '';
    try {
      const updated = await educatorApi.setSourceIncluded(id, source.id, !source.included);
      if (project) {
        project.sources = project.sources.map((s) => (s.id === source.id ? { ...s, included: updated.included } : s));
      }
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not update the source.';
    } finally {
      busy = '';
    }
  }

  async function handleSuggest() {
    if (suggesting) return;
    suggesting = true;
    error = '';
    try {
      // Suggestions are saved server-side with addedBy:"Ai" — refresh after.
      const res = await educatorApi.suggestSources(id);
      suggestions = res.suggestions ?? [];
      if (project) {
        const detail = await educatorApi.project(id);
        project.sources = detail.sources;
      }
      notice = suggestions.length
        ? `${suggestions.length} AI suggestions added — review them below.`
        : 'No new suggestions right now.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not get AI suggestions.';
    } finally {
      suggesting = false;
    }
  }

  /* ------------------------------------------------------------------ */
  /* Conflicts                                                           */
  /* ------------------------------------------------------------------ */

  async function handleAnalyze() {
    if (busy) return;
    busy = 'analyze';
    error = '';
    try {
      const res = await educatorApi.analyze(id);
      if (project) project.conflicts = res.conflicts;
      notice = res.conflictCount
        ? `${res.conflictCount} conflict${res.conflictCount === 1 ? '' : 's'} found — resolve them to unlock blueprint generation.`
        : 'No conflicts found — you can generate the blueprint.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not analyze the sources.';
    } finally {
      busy = '';
    }
  }

  async function handleResolve(conflict: EducatorConflictDto, resolution: 'Keep' | 'Remove' | 'Merge' | 'ManualNote', manualNote?: string) {
    if (busy) return;
    busy = conflict.id;
    error = '';
    try {
      const updated = await educatorApi.resolveConflict(id, conflict.id, {
        resolution,
        ...(resolution === 'ManualNote' && manualNote ? { manualNote } : {})
      });
      if (project) {
        project.conflicts = project.conflicts.map((c) => (c.id === conflict.id ? updated : c));
      }
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not resolve the conflict.';
    } finally {
      busy = '';
    }
  }

  /* ------------------------------------------------------------------ */
  /* Blueprint                                                           */
  /* ------------------------------------------------------------------ */

  async function handleGenerateBlueprint() {
    if (busy) return;
    busy = 'generate-blueprint';
    error = '';
    notice = '';
    try {
      const res = await educatorApi.generateBlueprint(id);
      blueprint = parseBlueprint(res.blueprintJson) ?? blueprint;
      if (project && res.blueprintJson) {
        project.blueprint = { json: res.blueprintJson, approvedAt: null };
      }
      notice = 'Blueprint ready — review it before approving.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not generate the blueprint.';
    } finally {
      busy = '';
    }
  }

  async function handleApproveBlueprint() {
    if (busy) return;
    busy = 'approve';
    error = '';
    notice = '';
    try {
      const res = await educatorApi.approveBlueprint(id);
      if (project) {
        project.courseId = res.courseId;
        project.generationJobs = res.jobs ?? [];
      }
      startPolling();
      notice = 'Approved — modules are generating in the background.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not approve the blueprint.';
    } finally {
      busy = '';
    }
  }

  /* ------------------------------------------------------------------ */
  /* Generation progress                                                 */
  /* ------------------------------------------------------------------ */

  function startPolling() {
    if (pollTimer !== null) window.clearInterval(pollTimer);
    void loadStatus();
    pollTimer = window.setInterval(() => void loadStatus(), 4000);
  }

  function stopPolling() {
    if (pollTimer !== null) {
      window.clearInterval(pollTimer);
      pollTimer = null;
    }
  }

  async function loadStatus() {
    try {
      const res = await educatorApi.generationStatus(id);
      status = res;
      const lower = res.status?.toLowerCase() ?? '';
      const allDone = res.totalCount > 0 && res.completedCount >= res.totalCount;
      const anyFailed = res.jobs.some((j) => j.status?.toLowerCase() === 'failed');
      if (allDone || lower.includes('ready') || lower.includes('completed') || lower.includes('failed') || anyFailed) {
        stopPolling();
        if (res.courseId && project && !project.courseId) project.courseId = res.courseId;
      }
    } catch {
      /* transient — keep polling */
    }
  }

  async function handleRegenerateBlueprint() {
    // No per-module retry endpoint exists — the fallback is regenerating the
    // blueprint and approving again.
    await loadProject();
    notice = 'Reloaded. Regenerate the blueprint to retry failed modules.';
  }

  /* ------------------------------------------------------------------ */
  /* Cover image                                                         */
  /* ------------------------------------------------------------------ */

  async function handleSaveCover() {
    const url = coverUrlInput.trim();
    if (!url || busy || !courseIdValue) return;
    busy = 'cover';
    error = '';
    try {
      const res = await educatorApi.setCourseCover(courseIdValue, url);
      savedCoverUrl = res.coverImageUrl;
      notice = 'Cover image saved.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not save the cover image.';
    } finally {
      busy = '';
    }
  }

  async function handleFindCover() {
    if (!courseIdValue || findingCover) return;
    findingCover = true;
    error = '';
    try {
      const res = await educatorApi.autoCourseCover(courseIdValue);
      coverCandidates = res.candidates ?? [];
      if (coverCandidates.length === 0) notice = 'No candidates found — paste an image link instead.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not find cover candidates.';
    } finally {
      findingCover = false;
    }
  }

  async function handlePickCover(url: string) {
    if (!courseIdValue || busy) return;
    busy = 'cover';
    error = '';
    try {
      const res = await educatorApi.autoCourseCover(courseIdValue, url);
      savedCoverUrl = res.coverImageUrl ?? url;
      notice = 'Cover image applied.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not apply the cover image.';
    } finally {
      busy = '';
    }
  }

  /* ------------------------------------------------------------------ */
  /* Publish                                                             */
  /* ------------------------------------------------------------------ */

  async function handlePublish() {
    if (!courseIdValue || busy) return;
    const price = Number(publishPrice);
    if (publishPaid && (!publishPrice.trim() || Number.isNaN(price) || price <= 0)) {
      error = 'Enter a valid price in ETB for a paid course.';
      return;
    }
    busy = 'publish';
    error = '';
    notice = '';
    try {
      await educatorApi.publishCourse(courseIdValue, publishPaid
        ? { isPaid: true, price, currency: publishCurrency }
        : { isPaid: false });
      published = true;
      notice = 'Course published — it is now available in the marketplace.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not publish the course.';
    } finally {
      busy = '';
    }
  }

  $: courseIdValue = status?.courseId ?? project?.courseId ?? '';

  $: generationActive = status
    ? !['readyforreview', 'completed', 'failed', 'ready for review'].includes((status.status ?? '').toLowerCase()) &&
      status.completedCount < status.totalCount
    : false;

  $: generationDone = status ? status.totalCount > 0 && status.completedCount >= status.totalCount : false;

  function jobIcon(jobStatus: string) {
    const s = jobStatus?.toLowerCase() ?? '';
    if (s === 'running') return Loader2;
    if (s === 'done' || s === 'completed' || s === 'succeeded') return CircleCheck;
    if (s === 'failed') return CircleX;
    return Circle;
  }

  function statusBadge(value: string): string {
    switch (value?.toLowerCase()) {
      case 'readyforreview':
      case 'ready for review':
        return 'bg-emerald-100 text-emerald-800';
      case 'generating':
        return 'bg-blue-100 text-blue-800';
      case 'conflicts':
        return 'bg-amber-100 text-amber-800';
      default:
        return 'bg-ifa-card-muted text-ifa-text-secondary border border-ifa-border';
    }
  }
</script>

<svelte:head>
  <title>{project ? `${project.title} · Studio` : 'Studio project'}</title>
</svelte:head>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  {#if $session.role !== 'Educator'}
    <div class="flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-800">
      <ShieldAlert class="w-5 h-5 shrink-0" />
      <span>The Educator Studio is only available to Educator accounts.</span>
    </div>
  {:else}
    <!-- Header -->
    <div class="flex items-start justify-between gap-4">
      <div class="min-w-0">
        <button
          type="button"
          on:click={() => goto('/studio')}
          class="flex items-center gap-1.5 text-xs font-semibold text-ifa-text-secondary hover:text-ifa-text-primary transition mb-1.5"
        >
          <ArrowLeft class="w-3.5 h-3.5" />
          All projects
        </button>
        {#if project}
          <h1 class="text-2xl font-bold text-ifa-text-primary truncate">{project.title}</h1>
          <p class="text-sm text-ifa-text-secondary mt-1 line-clamp-1">{project.description}</p>
          <div class="flex items-center gap-2 mt-2">
            <span class="text-[10px] px-2 py-0.5 rounded-full font-semibold {statusBadge(project.status)}">{project.status}</span>
            {#if project.conflicts.length > 0}
              <span class="text-[10px] px-2 py-0.5 rounded-full font-semibold bg-amber-100 text-amber-800">
                {unresolvedConflicts.length} unresolved of {project.conflicts.length} conflicts
              </span>
            {/if}
          </div>
        {:else}
          <div class="h-8 w-64 rounded-xl bg-ifa-card-muted animate-pulse"></div>
        {/if}
      </div>
      {#if courseIdValue}
        <a
          href={`/courses/${courseIdValue}`}
          class="shrink-0 flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs font-semibold text-ifa-pine hover:bg-ifa-card-muted transition"
        >
          <BookOpen class="w-3.5 h-3.5" />
          View course
        </a>
      {/if}
    </div>

    {#if error}
      <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>
    {/if}
    {#if notice}
      <p class="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800">{notice}</p>
    {/if}

    {#if loading}
      <div class="flex items-center justify-center py-20">
        <Loader2 class="w-6 h-6 text-ifa-pine animate-spin" />
      </div>
    {:else if project}
      <div class="grid grid-cols-1 lg:grid-cols-[minmax(0,5fr)_minmax(0,6fr)] gap-6 items-start">
        <!-- ================= LEFT: Sources panel ================= -->
        <section class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="text-sm font-bold text-ifa-text-primary">Sources ({project.sources.length})</h2>
            <button
              type="button"
              on:click={handleSuggest}
              disabled={suggesting}
              class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-ifa-pine/10 text-ifa-pine text-[11px] font-bold hover:bg-ifa-pine/20 transition disabled:opacity-50"
            >
              {#if suggesting}
                <Loader2 class="w-3.5 h-3.5 animate-spin" />
              {:else}
                <Sparkles class="w-3.5 h-3.5" />
              {/if}
              Get AI suggestions
            </button>
          </div>

          <!-- Add link input -->
          <div class="flex items-center gap-2">
            <input
              type="url"
              bind:value={sourceUrl}
              on:keydown={(e) => e.key === 'Enter' && handleAddSource()}
              placeholder="Paste a link — PDF, video, website, article or slides…"
              class="flex-1 px-3 py-2 rounded-xl bg-ifa-card-muted border border-ifa-border text-xs text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine"
            />
            <button
              type="button"
              on:click={handleAddSource}
              disabled={!sourceUrl.trim() || busy !== ''}
              class="px-3 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold flex items-center gap-1 hover:bg-ifa-pine-light transition disabled:opacity-50"
            >
              {#if busy === 'add-source'}
                <Loader2 class="w-3.5 h-3.5 animate-spin" />
              {:else}
                <Plus class="w-3.5 h-3.5" />
              {/if}
              Add
            </button>
          </div>
          <p class="text-[10px] text-ifa-text-muted">The type (PDF, video, website, article, slides) is detected automatically.</p>

          <!-- Source rows -->
          <div class="space-y-2 max-h-[480px] overflow-y-auto pr-1">
            {#each project.sources as source (source.id)}
              <div class="p-3 rounded-xl border {source.included ? 'border-ifa-border bg-ifa-card-muted/40' : 'border-ifa-border-light bg-ifa-card-muted/20 opacity-70'}">
                <div class="flex items-start gap-2.5">
                  <span class="w-7 h-7 rounded-lg bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-pine shrink-0">
                    <svelte:component this={sourceIcon(source.type)} class="w-3.5 h-3.5" />
                  </span>
                  <div class="min-w-0 flex-1">
                    <div class="flex items-center gap-1.5 flex-wrap">
                      <a
                        href={source.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        class="text-xs font-semibold text-ifa-text-primary hover:text-ifa-pine truncate max-w-full"
                      >
                        {source.title || source.url}
                      </a>
                      {#if source.type}
                        <span class="text-[9px] px-1.5 py-0.5 rounded-full bg-ifa-pine/10 text-ifa-pine font-bold uppercase">{source.type}</span>
                      {/if}
                      {#if source.addedBy === 'Ai'}
                        <span class="text-[9px] px-1.5 py-0.5 rounded-full bg-purple-100 text-purple-700 font-bold">AI</span>
                      {/if}
                    </div>
                    {#if source.description}
                      <p class="text-[11px] text-ifa-text-secondary line-clamp-1 mt-0.5">{source.description}</p>
                    {/if}
                  </div>

                  <div class="flex items-center gap-1 shrink-0">
                    <!-- Status indicator -->
                    {#if (source.status ?? '').toLowerCase() === 'extracted'}
                      <span class="text-[10px] font-semibold text-emerald-700 flex items-center gap-1" title="Extracted">
                        <CircleCheck class="w-3.5 h-3.5" /> Extracted
                      </span>
                    {:else if (source.status ?? '').toLowerCase() === 'failed'}
                      <span
                        class="text-[10px] font-semibold text-red-700 flex items-center gap-1 cursor-help"
                        title={source.failureReason || 'Extraction failed'}
                      >
                        <AlertTriangle class="w-3.5 h-3.5" /> Failed
                      </span>
                    {:else}
                      <span class="text-[10px] font-semibold text-amber-700 flex items-center gap-1" title="Waiting for extraction">
                        <Loader2 class="w-3 h-3 animate-spin" /> Pending
                      </span>
                    {/if}

                    <button
                      type="button"
                      on:click={() => handleToggleIncluded(source)}
                      disabled={busy !== ''}
                      title={source.included ? 'Exclude from course' : 'Include in course'}
                      class="p-1.5 rounded-lg text-ifa-text-muted hover:text-ifa-pine hover:bg-ifa-card transition disabled:opacity-40"
                    >
                      {#if source.included}
                        <Eye class="w-3.5 h-3.5" />
                      {:else}
                        <EyeOff class="w-3.5 h-3.5" />
                      {/if}
                    </button>
                    <button
                      type="button"
                      on:click={() => handleDeleteSource(source.id)}
                      disabled={busy !== ''}
                      title="Delete source"
                      class="p-1.5 rounded-lg text-ifa-text-muted hover:text-red-600 hover:bg-red-50 transition disabled:opacity-40"
                    >
                      {#if busy === source.id}
                        <Loader2 class="w-3.5 h-3.5 animate-spin" />
                      {:else}
                        <Trash2 class="w-3.5 h-3.5" />
                      {/if}
                    </button>
                  </div>
                </div>
              </div>
            {:else}
              <p class="text-xs text-ifa-text-muted py-6 text-center">No sources yet — paste a link above or ask the AI for suggestions.</p>
            {/each}
          </div>

          {#if suggestions.length > 0}
            <div class="rounded-xl border border-purple-200 bg-purple-50/60 p-3 space-y-2">
              <p class="text-[11px] font-bold uppercase tracking-wider text-purple-700">AI suggestions (saved to sources)</p>
              {#each suggestions as suggestion (suggestion.id)}
                <div class="flex items-center justify-between gap-2">
                  <a
                    href={suggestion.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    class="text-xs text-ifa-text-primary hover:text-ifa-pine truncate"
                  >
                    {suggestion.title || suggestion.url}
                  </a>
                  <button
                    type="button"
                    on:click={() => handleDeleteSource(suggestion.id)}
                    disabled={busy !== ''}
                    title="Remove suggestion"
                    class="p-1 rounded text-purple-500 hover:text-red-600 transition shrink-0"
                  >
                    <X class="w-3.5 h-3.5" />
                  </button>
                </div>
              {/each}
            </div>
          {/if}
        </section>

        <!-- ================= RIGHT: Assistant / analysis panel ================= -->
        <div class="space-y-6">
          <!-- Conflicts / analysis -->
          <section class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-bold text-ifa-text-primary flex items-center gap-2">
                <AlertTriangle class="w-4 h-4 text-amber-600" />
                Analysis &amp; conflicts
              </h2>
              <button
                type="button"
                on:click={handleAnalyze}
                disabled={busy !== ''}
                class="px-3 py-1.5 rounded-lg bg-ifa-pine text-white text-[11px] font-bold flex items-center gap-1.5 hover:bg-ifa-pine-light transition disabled:opacity-50"
              >
                {#if busy === 'analyze'}
                  <Loader2 class="w-3.5 h-3.5 animate-spin" />
                {:else}
                  <RefreshCw class="w-3.5 h-3.5" />
                {/if}
                Analyze sources
              </button>
            </div>

            {#if project.conflicts.length === 0}
              <p class="text-xs text-ifa-text-muted py-3">
                Run "Analyze sources" to check for duplicate, conflicting or unrelated material.
              </p>
            {:else}
              <div class="space-y-3">
                {#each project.conflicts as conflict (conflict.id)}
                  <div class="rounded-xl border {conflict.resolution ? 'border-emerald-200 bg-emerald-50/40' : 'border-amber-200 bg-amber-50/50'} p-3.5 space-y-2.5">
                    <div class="flex items-center gap-2">
                      <span class="text-[10px] px-2 py-0.5 rounded-full font-bold uppercase {conflict.kind?.toLowerCase() === 'duplicate'
                        ? 'bg-blue-100 text-blue-800'
                        : conflict.kind?.toLowerCase() === 'unrelated'
                          ? 'bg-gray-200 text-gray-700'
                          : 'bg-amber-200 text-amber-900'}">
                        {conflict.kind}
                      </span>
                      {#if conflict.resolution}
                        <span class="text-[10px] px-2 py-0.5 rounded-full font-bold bg-emerald-200 text-emerald-900">
                          Resolved: {conflict.resolution}{conflict.manualNote ? ` — "${conflict.manualNote}"` : ''}
                        </span>
                      {/if}
                    </div>
                    <p class="text-xs text-ifa-text-secondary leading-relaxed">{conflict.explanation}</p>

                    {#if !conflict.resolution}
                      <!-- Suggested resolutions as selectable options -->
                      <div class="flex flex-wrap gap-1.5">
                        {#each conflict.suggestedResolutions as suggestion (suggestion)}
                          <button
                            type="button"
                            on:click={() => {
                              conflictSelections = { ...conflictSelections, [conflict.id]: suggestion };
                              const map = suggestion.toLowerCase() === 'keep' ? 'Keep'
                                : suggestion.toLowerCase() === 'remove' ? 'Remove'
                                : suggestion.toLowerCase() === 'merge' ? 'Merge' : 'ManualNote';
                              void handleResolve(conflict, map as 'Keep' | 'Remove' | 'Merge' | 'ManualNote');
                            }}
                            disabled={busy !== ''}
                            class="px-2.5 py-1 rounded-lg border border-ifa-border bg-ifa-card text-[11px] font-semibold text-ifa-text-primary hover:border-ifa-pine hover:text-ifa-pine transition disabled:opacity-50"
                          >
                            {suggestion}
                          </button>
                        {/each}
                        <button
                          type="button"
                          on:click={() => handleResolve(conflict, 'Keep')}
                          disabled={busy !== ''}
                          class="px-2.5 py-1 rounded-lg border border-ifa-border bg-ifa-card text-[11px] font-semibold text-ifa-text-primary hover:border-ifa-pine hover:text-ifa-pine transition disabled:opacity-50"
                        >
                          Keep both
                        </button>
                      </div>

                      <!-- Manual note -->
                      <div class="flex items-center gap-1.5">
                        <input
                          type="text"
                          bind:value={conflictNotes[conflict.id]}
                          placeholder="Or add a manual note…"
                          class="flex-1 px-2.5 py-1.5 rounded-lg bg-ifa-card border border-ifa-border text-[11px] text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine"
                        />
                        <button
                          type="button"
                          on:click={() => handleResolve(conflict, 'ManualNote', conflictNotes[conflict.id] ?? '')}
                          disabled={busy !== '' || !(conflictNotes[conflict.id] ?? '').trim()}
                          class="px-2.5 py-1.5 rounded-lg bg-ifa-pine text-white text-[11px] font-bold hover:bg-ifa-pine-light transition disabled:opacity-50"
                        >
                          {#if busy === conflict.id}
                            <Loader2 class="w-3 h-3 animate-spin" />
                          {:else}
                            Note it
                          {/if}
                        </button>
                      </div>
                    {/if}
                  </div>
                {/each}
              </div>
            {/if}
          </section>

          <!-- Blueprint -->
          <section class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-bold text-ifa-text-primary">Blueprint</h2>
              <button
                type="button"
                on:click={handleGenerateBlueprint}
                disabled={busy !== '' || unresolvedConflicts.length > 0}
                title={unresolvedConflicts.length > 0 ? 'Resolve all conflicts first' : 'Generate a course blueprint'}
                class="px-3 py-1.5 rounded-lg bg-ifa-pine text-white text-[11px] font-bold flex items-center gap-1.5 hover:bg-ifa-pine-light transition disabled:opacity-50"
              >
                {#if busy === 'generate-blueprint'}
                  <Loader2 class="w-3.5 h-3.5 animate-spin" />
                {:else}
                  <Sparkles class="w-3.5 h-3.5" />
                {/if}
                Generate blueprint
              </button>
            </div>

            {#if unresolvedConflicts.length > 0}
              <p class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-[11px] text-amber-800">
                Resolve {unresolvedConflicts.length} conflict{unresolvedConflicts.length === 1 ? '' : 's'} above to enable blueprint generation.
              </p>
            {/if}

            {#if blueprint}
              <div class="space-y-2.5 max-h-[420px] overflow-y-auto pr-1">
                <p class="text-[11px] text-ifa-text-muted">
                  {blueprint.title} · {blueprint.category} · {blueprint.estimatedDuration}
                </p>
                {#each blueprint.modules ?? [] as mod, i (i)}
                  <div class="rounded-xl border border-ifa-border bg-ifa-card-muted/40 p-3.5 space-y-2">
                    <div class="flex items-start justify-between gap-2">
                      <div>
                        <p class="text-xs font-bold text-ifa-text-primary">Module {i + 1}: {mod.title}</p>
                        <p class="text-[11px] text-ifa-text-secondary mt-0.5">{mod.description}</p>
                      </div>
                      <span class="text-[10px] font-mono text-ifa-text-muted shrink-0">{mod.estimatedHours}h</span>
                    </div>
                    {#if mod.chapters?.length}
                      <ul class="space-y-1 pl-1">
                        {#each mod.chapters as chapter, ci (ci)}
                          <li class="text-[11px] text-ifa-text-secondary flex items-start gap-1.5">
                            <Check class="w-3 h-3 text-ifa-pine shrink-0 mt-0.5" />
                            <span>
                              <span class="font-semibold text-ifa-text-primary">{chapter.title}</span>
                              {#if chapter.summary} — {chapter.summary}{/if}
                              {#if chapter.sourceUrls?.length}
                                {#each chapter.sourceUrls as sUrl, si (si)}
                                  <a href={sUrl} target="_blank" rel="noopener noreferrer" class="inline-flex items-center gap-0.5 text-ifa-pine hover:underline ml-1">
                                    source{#if chapter.sourceUrls.length > 1}&nbsp;{si + 1}{/if}
                                    <ExternalLink class="w-2.5 h-2.5" />
                                  </a>
                                {/each}
                              {/if}
                            </span>
                          </li>
                        {/each}
                      </ul>
                    {/if}
                    {#if mod.sources?.length}
                      <div class="flex flex-wrap gap-1.5">
                        {#each mod.sources as src, si (si)}
                          <a
                            href={src.url}
                            target="_blank"
                            rel="noopener noreferrer"
                            class="text-[9px] px-1.5 py-0.5 rounded-full bg-ifa-pine/10 text-ifa-pine font-semibold hover:bg-ifa-pine/20 transition max-w-[240px] truncate"
                            title={src.title}
                          >
                            {src.title || src.url}
                          </a>
                        {/each}
                      </div>
                    {/if}
                  </div>
                {/each}
              </div>

              <button
                type="button"
                on:click={handleApproveBlueprint}
                disabled={busy !== '' || !!project.blueprint?.approvedAt}
                class="w-full py-2.5 rounded-xl bg-ifa-pine text-white text-xs font-bold flex items-center justify-center gap-2 hover:bg-ifa-pine-light transition disabled:opacity-60"
              >
                {#if busy === 'approve'}
                  <Loader2 class="w-3.5 h-3.5 animate-spin" />
                {:else}
                  <Rocket class="w-3.5 h-3.5" />
                {/if}
                {project.blueprint?.approvedAt ? 'Blueprint approved' : 'Approve & Generate'}
              </button>
            {:else}
              <p class="text-xs text-ifa-text-muted py-3">
                {project.blueprint?.json ? 'Blueprint could not be parsed — regenerate it.' : 'No blueprint yet. Generate one once your sources are ready.'}
              </p>
            {/if}
          </section>

          <!-- Generation progress -->
          {#if status}
            <section class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
              <div class="flex items-center justify-between">
                <h2 class="text-sm font-bold text-ifa-text-primary flex items-center gap-2">
                  {#if generationActive}
                    <Loader2 class="w-4 h-4 text-ifa-pine animate-spin" />
                  {:else}
                    <Rocket class="w-4 h-4 text-ifa-pine" />
                  {/if}
                  Generation progress
                </h2>
                <span class="text-[10px] font-bold {generationDone ? 'text-emerald-700' : 'text-ifa-text-muted'}">
                  {status.completedCount}/{status.totalCount} modules
                </span>
              </div>

              <!-- Overall progress bar -->
              <div class="h-2 rounded-full bg-ifa-card-muted overflow-hidden">
                <div
                  class="h-full rounded-full bg-ifa-pine transition-all duration-500"
                  style="width: {status.totalCount ? Math.round((status.completedCount / status.totalCount) * 100) : 0}%"
                ></div>
              </div>

              <!-- Per-module rows -->
              <div class="space-y-1.5">
                {#each status.jobs as job, i (job.moduleIndex ?? i)}
                  {@const Icon = jobIcon(job.status)}
                  <div class="flex items-center justify-between gap-2 px-3 py-2 rounded-lg bg-ifa-card-muted/40">
                    <span class="text-xs font-semibold text-ifa-text-primary flex items-center gap-2 min-w-0">
                      {#if (job.status ?? '').toLowerCase() === 'running'}
                        <Loader2 class="w-3.5 h-3.5 text-ifa-pine animate-spin shrink-0" />
                      {:else}
                        <svelte:component this={Icon} class="w-3.5 h-3.5 shrink-0 {job.status?.toLowerCase() === 'failed' ? 'text-red-600' : (job.status ?? '').toLowerCase() === 'done' || (job.status ?? '').toLowerCase() === 'completed' || (job.status ?? '').toLowerCase() === 'succeeded' ? 'text-emerald-600' : 'text-ifa-text-muted'}" />
                      {/if}
                      <span class="truncate">Module {job.moduleIndex + 1}: {job.moduleTitle}</span>
                    </span>
                    <span class="text-[10px] font-bold uppercase shrink-0 {job.status?.toLowerCase() === 'failed' ? 'text-red-600' : (job.status ?? '').toLowerCase() === 'running' ? 'text-ifa-pine' : (job.status ?? '').toLowerCase() === 'done' || (job.status ?? '').toLowerCase() === 'completed' || (job.status ?? '').toLowerCase() === 'succeeded' ? 'text-emerald-700' : 'text-ifa-text-muted'}">
                      {job.status}
                    </span>
                  </div>
                  {#if job.error}
                    <p class="px-3 text-[11px] text-red-700">{job.error}</p>
                  {/if}
                {/each}
              </div>

              {#if status.jobs.some((j) => j.status?.toLowerCase() === 'failed')}
                <button
                  type="button"
                  on:click={handleRegenerateBlueprint}
                  class="w-full py-2 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary text-xs font-semibold hover:text-ifa-text-primary transition flex items-center justify-center gap-1.5"
                >
                  <RefreshCw class="w-3.5 h-3.5" />
                  Regenerate blueprint (retry failed modules)
                </button>
              {/if}
            </section>
          {/if}

          <!-- Ready for review: cover + publish + edit course -->
          {#if courseIdValue}
            <!-- Cover image step -->
            <section class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
              <h2 class="text-sm font-bold text-ifa-text-primary flex items-center gap-2">
                <ImageIcon class="w-4 h-4 text-ifa-pine" />
                Cover image
              </h2>
              <div class="flex items-center gap-2">
                <input
                  type="url"
                  bind:value={coverUrlInput}
                  placeholder="Add image link — https://…/cover.jpg"
                  class="flex-1 px-3 py-2 rounded-xl bg-ifa-card-muted border border-ifa-border text-xs text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine"
                />
                <button
                  type="button"
                  on:click={handleSaveCover}
                  disabled={!coverUrlInput.trim() || busy !== ''}
                  class="px-3 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition disabled:opacity-50"
                >
                  Save
                </button>
                <button
                  type="button"
                  on:click={handleFindCover}
                  disabled={busy !== ''}
                  class="px-3 py-2 rounded-xl bg-ifa-pine/10 text-ifa-pine text-xs font-bold hover:bg-ifa-pine/20 transition flex items-center gap-1.5 disabled:opacity-50"
                >
                  {#if findingCover}
                    <Loader2 class="w-3.5 h-3.5 animate-spin" />
                  {:else}
                    <Sparkles class="w-3.5 h-3.5" />
                  {/if}
                  Find one for me
                </button>
              </div>

              {#if savedCoverUrl}
                <div class="flex items-center gap-3">
                  <img src={savedCoverUrl} alt="Course cover" class="w-28 h-16 object-cover rounded-lg border border-ifa-border" />
                  <span class="text-[10px] text-emerald-700 font-semibold flex items-center gap-1">
                    <CircleCheck class="w-3.5 h-3.5" /> Cover saved
                  </span>
                </div>
              {/if}

              {#if coverCandidates.length > 0}
                <div>
                  <p class="text-[11px] font-semibold text-ifa-text-secondary mb-1.5">Pick a candidate:</p>
                  <div class="grid grid-cols-3 sm:grid-cols-4 gap-2">
                    {#each coverCandidates as candidate (candidate.url)}
                      <button
                        type="button"
                        on:click={() => handlePickCover(candidate.url)}
                        class="rounded-lg overflow-hidden border-2 {savedCoverUrl === candidate.url ? 'border-ifa-pine' : 'border-ifa-border'} hover:border-ifa-pine transition"
                      >
                        <img src={candidate.url} alt={candidate.title} loading="lazy" class="w-full aspect-video object-cover" />
                        <span class="block text-[9px] text-ifa-text-muted truncate px-1 py-0.5">{candidate.title}</span>
                      </button>
                    {/each}
                  </div>
                </div>
              {/if}
            </section>

            <!-- Publish -->
            <section class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
              <h2 class="text-sm font-bold text-ifa-text-primary flex items-center gap-2">
                <Globe class="w-4 h-4 text-ifa-pine" />
                Publish
              </h2>

              {#if published}
                <p class="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800 flex items-center gap-1.5">
                  <CircleCheck class="w-3.5 h-3.5" />
                  Published — learners can find it in the marketplace.
                </p>
              {:else}
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  <button
                    type="button"
                    on:click={() => {
                      publishPaid = false;
                      void handlePublish();
                    }}
                    disabled={busy !== ''}
                    class="py-2.5 rounded-xl bg-ifa-pine text-white text-xs font-bold hover:bg-ifa-pine-light transition disabled:opacity-50"
                  >
                    Publish free
                  </button>
                  <div class="flex items-center gap-1.5">
                    <input
                      type="number"
                      min="1"
                      bind:value={publishPrice}
                      placeholder="Price (ETB)"
                      class="flex-1 px-3 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-xs text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine w-24"
                      on:input={() => (publishPaid = true)}
                    />
                    <button
                      type="button"
                      on:click={() => {
                        publishPaid = true;
                        void handlePublish();
                      }}
                      disabled={busy !== '' || !publishPrice.trim()}
                      class="px-3 py-2.5 rounded-xl bg-ifa-pine/10 text-ifa-pine text-xs font-bold hover:bg-ifa-pine/20 transition disabled:opacity-50 shrink-0"
                    >
                      {#if busy === 'publish'}
                        <Loader2 class="w-3.5 h-3.5 animate-spin" />
                      {:else}
                        Publish paid
                      {/if}
                    </button>
                  </div>
                </div>
                <p class="text-[10px] text-ifa-text-muted">
                  Paid courses check out with Telebirr / Chapa in Ethiopian Birr (ETB).
                </p>
              {/if}
            </section>

            <!-- Edit course -->
            <CourseEditor courseId={courseIdValue} />
          {:else if status && generationDone}
            <p class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
              Generation finished but the course id is not available yet — reload the page to continue.
            </p>
          {/if}
        </div>
      </div>
    {/if}
  {/if}
</div>
