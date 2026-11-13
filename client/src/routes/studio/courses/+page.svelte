<script lang="ts">
  import { onMount } from 'svelte';
  import { Search, Plus, BookOpen, ArrowRight, RefreshCw } from 'lucide-svelte';
  import { ApiError, educatorApi, studioCoursesApi } from '$lib/api';
  import StatusBadge from '$lib/components/ui/StatusBadge.svelte';
  import EmptyState from '$lib/components/ui/EmptyState.svelte';
  import ErrorState from '$lib/components/ui/ErrorState.svelte';
  import LoadingSkeleton from '$lib/components/ui/LoadingSkeleton.svelte';
  import { reveal } from '$lib/motion/premiumMotion';
  import { session } from '$lib/stores/sessionStore';

  type CourseRow = {
    id: string;
    courseId?: string | null;
    title: string;
    description?: string;
    mode?: string;
    state: string;
    updatedAt: string;
    coverImageUrl?: string;
    isPublished?: boolean;
    legacy?: boolean;
  };

  let courses: CourseRow[] = [];
  let loading = true;
  let error = '';
  let query = '';
  let stateFilter = 'All';
  let loadedWorkspace = '';

  $: filtered = courses.filter((course) =>
    (stateFilter === 'All' || course.state === stateFilter)
    && `${course.title} ${course.description ?? ''}`.toLowerCase().includes(query.trim().toLowerCase())
  );

  onMount(load);
  $: if ($session.activeWorkspaceId && $session.activeWorkspaceId !== loadedWorkspace && !loading) void load();

  async function load() {
    loading = true;
    error = '';
    try {
      const workspaceId = $session.activeWorkspaceId;
      loadedWorkspace = workspaceId ?? '';
      courses = await studioCoursesApi.list(workspaceId);
    } catch (cause) {
      if (cause instanceof ApiError && cause.status === 404) {
        const legacy = await educatorApi.projects();
        courses = legacy.map((project) => ({
          id: project.id,
          courseId: project.courseId,
          title: project.title,
          description: project.description,
          state: project.status,
          updatedAt: project.updatedAt,
          legacy: true
        }));
      } else {
        error = cause instanceof Error ? cause.message : 'Studio courses could not be loaded.';
      }
    } finally {
      loading = false;
    }
  }

  function tone(state: string): 'neutral' | 'success' | 'warning' | 'danger' | 'info' {
    if (state === 'Published' || state === 'PublishReady' || state === 'ReadyForReview') return 'success';
    if (state === 'Failed' || state === 'GenerationFailed') return 'danger';
    if (state === 'Generating' || state === 'Analyzing' || state === 'Queued' || state === 'Running') return 'info';
    if (state === 'AwaitingApproval' || state === 'BlueprintReady') return 'warning';
    return 'neutral';
  }
</script>

<svelte:head><title>Studio courses · Witforge</title></svelte:head>

<div class="studio-shell">
  <header class="studio-heading" use:reveal>
    <div>
      <p class="wf-kicker">Educator Studio</p>
      <h1 class="wf-display">Courses are the <em>center of gravity.</em></h1>
      <p>Build, review and release from one dependable inventory. Processing state stays visible; edited work stays yours.</p>
    </div>
    <a href="/studio/create"><Plus />New course</a>
  </header>

  <div class="studio-summary" use:reveal={{ delay: .08 }}>
    <div><strong class="wf-display">{courses.length}</strong><span>Total courses</span></div>
    <div><strong class="wf-display">{courses.filter((course) => course.state === 'Published').length}</strong><span>Published</span></div>
    <div><strong class="wf-display">{courses.filter((course) => ['Generating','Analyzing','Queued','Running'].includes(course.state)).length}</strong><span>In process</span></div>
    <div class="filter-cell">
      <label><Search /><span class="sr-only">Search courses</span><input bind:value={query} placeholder="Find a course" /></label>
      <select bind:value={stateFilter} aria-label="Filter courses by state"><option>All</option>{#each Array.from(new Set(courses.map((course) => course.state))).sort() as state}<option value={state}>{state}</option>{/each}</select>
    </div>
  </div>

  <div class="course-inventory">
    {#if loading}
      <LoadingSkeleton rows={4} label="Loading Studio courses" />
    {:else if error}
      <ErrorState message={error}><button on:click={load} class="inline-flex items-center gap-2 rounded-lg border border-red-300 px-3 py-2 text-sm font-bold"><RefreshCw class="h-4 w-4" />Retry</button></ErrorState>
    {:else if filtered.length === 0}
      <EmptyState title="No courses match" description={courses.length === 0 ? 'Create a manual draft, start from a template, or queue an AI-assisted outline.' : 'Adjust the search or state filter.'}>
        {#if courses.length === 0}<a href="/studio/create" class="rounded-xl bg-ifa-pine px-4 py-2.5 text-sm font-bold text-white">Create the first course</a>{/if}
      </EmptyState>
    {:else}
      <div class="course-grid">
        {#each filtered as course, index (course.id)}
          <article class:featured={index === 0} class="course-tile" use:reveal={{ delay: Math.min(index, 5) * .05 }}>
            <a href={`/studio/courses/${course.id}/edit`} class="course-poster poster-{index % 4}">
              <span>{String(index + 1).padStart(2, '0')} / {course.mode ?? (course.legacy ? 'Legacy' : 'Manual')}</span>
              <strong class="wf-display">{course.title.slice(0, 2).toUpperCase()}</strong>
              <BookOpen />
            </a>
            <div class="course-tile-body">
              <div><StatusBadge label={course.state} tone={tone(course.state)} /><span>{new Date(course.updatedAt).toLocaleDateString()}</span></div>
              <h2 class="wf-display">{course.title}</h2>
              <p>{course.description || 'A saved course draft ready for curriculum work.'}</p>
              <a href={`/studio/courses/${course.id}/edit`}>Open workspace <ArrowRight /></a>
            </div>
          </article>
        {/each}
      </div>
    {/if}
  </div>
</div>

<style>
  .studio-shell{width:min(100%,94rem);margin:0 auto;padding:clamp(1.5rem,3vw,3rem)}.studio-heading{display:flex;align-items:end;justify-content:space-between;gap:3rem;padding:1.5rem 0 3.5rem}.studio-heading .wf-kicker{color:var(--wf-text-muted)}.studio-heading h1{max-width:65rem;margin:1rem 0 .8rem;font-size:clamp(3.5rem,6.4vw,7.4rem);font-weight:420;line-height:.84;letter-spacing:-.06em}.studio-heading h1 em{color:#176b4a;font-weight:420}.studio-heading p:last-child{max-width:48rem;color:var(--wf-text-muted);font-size:.78rem;line-height:1.7}.studio-heading>a{display:inline-flex;align-items:center;gap:.6rem;flex-shrink:0;padding:.85rem 1.1rem;border-radius:999px;color:#17221c;background:#c7f464;font-size:.68rem;font-weight:850}.studio-heading>a :global(svg){width:.95rem}
  .studio-summary{display:grid;grid-template-columns:.55fr .55fr .55fr 2fr;border:1px solid var(--wf-border);background:var(--wf-surface-raised)}.studio-summary>div{display:flex;min-height:6.5rem;flex-direction:column;justify-content:center;padding:1rem 1.3rem;border-right:1px solid var(--wf-border)}.studio-summary strong{font-size:2.5rem;font-weight:450;line-height:.9}.studio-summary span{margin-top:.4rem;color:var(--wf-text-muted);font-size:.55rem;font-weight:800;text-transform:uppercase}.filter-cell{display:grid!important;grid-template-columns:1fr auto;align-items:center;gap:.7rem;border-right:0!important}.filter-cell label{display:grid;grid-template-columns:1rem 1fr;align-items:center;gap:.6rem;padding:.65rem .8rem;border:1px solid var(--wf-border);border-radius:999px;background:var(--wf-surface-muted)}.filter-cell label :global(svg){width:.9rem}.filter-cell input{min-width:0;background:transparent;font-size:.68rem;outline:none}.filter-cell select{padding:.65rem;border:1px solid var(--wf-border);border-radius:999px;background:var(--wf-surface-raised);font-size:.65rem}
  .course-inventory{margin-top:1rem}.course-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:1rem}.course-tile{display:grid;grid-template-rows:auto 1fr;border:1px solid var(--wf-border);background:var(--wf-surface-raised);transition:transform .3s cubic-bezier(.22,1,.36,1),box-shadow .3s}.course-tile:hover{transform:translateY(-4px);box-shadow:0 28px 70px -48px #17221c}.course-poster{position:relative;display:flex;aspect-ratio:1.65;flex-direction:column;justify-content:space-between;padding:1.1rem;color:#17221c}.course-poster>span{font-size:.55rem;font-weight:850;letter-spacing:.12em;text-transform:uppercase}.course-poster strong{font-size:4.2rem;font-style:italic;font-weight:430;line-height:.75}.course-poster :global(svg){position:absolute;right:1rem;bottom:1rem;width:1.2rem}.poster-0{background:#c7f464}.poster-1{background:#ff735d}.poster-2{color:#f3f0e8;background:#3d63dd}.poster-3{background:#eae5d9}.course-tile-body{display:flex;min-height:14rem;flex-direction:column;padding:1.25rem}.course-tile-body>div{display:flex;align-items:center;justify-content:space-between}.course-tile-body>div>span{color:var(--wf-text-muted);font-size:.54rem}.course-tile h2{margin-top:1.3rem;font-size:1.7rem;font-weight:480;line-height:1.02}.course-tile p{display:-webkit-box;margin-top:.65rem;overflow:hidden;color:var(--wf-text-muted);font-size:.64rem;line-height:1.6;line-clamp:2;-webkit-box-orient:vertical;-webkit-line-clamp:2}.course-tile-body>a{display:inline-flex;align-items:center;gap:.5rem;margin-top:auto;padding-top:1.2rem;font-size:.63rem;font-weight:850}.course-tile-body>a :global(svg){width:.85rem}
  @media(max-width:1080px){.course-grid{grid-template-columns:1fr 1fr}.studio-summary{grid-template-columns:repeat(3,1fr)}.filter-cell{grid-column:1/-1;border-top:1px solid var(--wf-border)}}@media(max-width:700px){.studio-shell{padding:1rem}.studio-heading{display:block}.studio-heading h1{font-size:4rem}.studio-heading>a{margin-top:1.5rem}.studio-summary{grid-template-columns:1fr 1fr}.studio-summary>div:nth-child(3){border-top:1px solid var(--wf-border)}.filter-cell{display:flex!important;flex-direction:column;align-items:stretch}.course-grid{grid-template-columns:1fr}}
</style>
