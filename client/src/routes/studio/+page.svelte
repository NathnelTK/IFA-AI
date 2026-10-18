<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import {
    Plus,
    Users,
    CheckCircle2,
    Globe2,
    Loader2,
    X,
    ArrowRight,
    ShieldAlert,
    GraduationCap,
    TrendingUp,
    FolderOpen,
    BookOpen
  } from 'lucide-svelte';
  import { educatorApi, type EducatorProjectDto, type EducatorDashboardDto, type EducatorEnrollmentDto } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';

  // Educator home — a different experience from the learner dashboard. The
  // educator's day revolves around creating courses and shepherding learners
  // in, so the page leads with studio actions, approvals and course health
  // instead of XP, streaks and recommendations.

  let projects: EducatorProjectDto[] = [];
  let dashboard: EducatorDashboardDto | null = null;
  let pending: EducatorEnrollmentDto[] = [];
  let loading = true;
  let error = '';
  let notice = '';
  let actingOn: string | null = null;

  let showNewProject = false;
  let newTitle = '';
  let newDescription = '';
  let creating = false;

  onMount(async () => {
    if ($session.role !== 'Educator') {
      loading = false;
      return;
    }
    try {
      const [projectList, dash, pendingList] = await Promise.all([
        educatorApi.projects(),
        educatorApi.dashboard(),
        educatorApi.enrollments('PendingApproval').catch(() => [])
      ]);
      projects = projectList;
      dashboard = dash;
      pending = pendingList;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load your studio.';
    } finally {
      loading = false;
    }
  });

  const greeting = () => {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 18) return 'Good afternoon';
    return 'Good evening';
  };

  $: activeProjects = projects.filter((p) => p.status !== 'Published');
  $: generatingCount = projects.filter((p) => p.status.toLowerCase() === 'generating').length;

  async function handleCreate() {
    if (!newTitle.trim() || creating) return;
    creating = true;
    error = '';
    try {
      const project = await educatorApi.createProject({
        title: newTitle.trim(),
        description: newDescription.trim()
      });
      showNewProject = false;
      await goto(`/studio/${project.id}`);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not create the project.';
    } finally {
      creating = false;
    }
  }

  async function handleDecision(enrollmentId: string, approve: boolean) {
    if (actingOn) return;
    actingOn = enrollmentId;
    notice = '';
    try {
      if (approve) await educatorApi.approveEnrollment(enrollmentId);
      else await educatorApi.rejectEnrollment(enrollmentId);
      pending = pending.filter((e) => e.enrollmentId !== enrollmentId);
      notice = approve ? 'Enrollment approved.' : 'Enrollment rejected.';
      dashboard = await educatorApi.dashboard().catch(() => dashboard);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not process the request.';
    } finally {
      actingOn = null;
    }
  }

  function statusBadge(status: string): string {
    switch (status?.toLowerCase()) {
      case 'readyforreview':
        return 'bg-emerald-100 text-emerald-800';
      case 'generating':
        return 'bg-blue-100 text-blue-800';
      case 'awaitingresolution':
        return 'bg-amber-100 text-amber-800';
      case 'published':
        return 'bg-ifa-pine text-white';
      case 'draft':
      default:
        return 'bg-ifa-card-muted text-ifa-text-secondary border border-ifa-border';
    }
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1400px] mx-auto">
  {#if $session.role !== 'Educator'}
    <div class="flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-800">
      <ShieldAlert class="w-5 h-5 shrink-0" />
      <span>The Educator Studio is only available to Educator accounts. Register as an Educator to build courses.</span>
    </div>
  {:else}
    <!-- Welcome header -->
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-ifa-text-primary">
          {greeting()}{$session.name ? `, ${$session.name.split(' ')[0]}` : ''}
        </h1>
        <p class="text-sm text-ifa-text-secondary mt-1">Here's what's happening across your courses today.</p>
      </div>
      <div class="flex items-center gap-2">
        <a
          href="/studio/dashboard"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-ifa-card border border-ifa-border text-ifa-text-secondary text-sm font-semibold hover:text-ifa-pine hover:border-ifa-pine/40 transition"
        >
          <TrendingUp class="w-4 h-4" />
          <span>Dashboard</span>
        </a>
        <button
          type="button"
          on:click={() => (showNewProject = true)}
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-ifa-pine text-white text-sm font-bold hover:bg-ifa-pine-light transition shadow-soft"
        >
          <Plus class="w-4 h-4" />
          <span>New course</span>
        </button>
      </div>
    </div>

    {#if error}
      <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>
    {/if}
    {#if notice}
      <p class="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800">{notice}</p>
    {/if}

    {#if loading}
      <div class="flex items-center justify-center py-16">
        <Loader2 class="w-6 h-6 text-ifa-pine animate-spin" />
      </div>
    {:else}
      <!-- Stats -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card flex items-center gap-4">
          <span class="w-10 h-10 rounded-xl bg-emerald-100 text-emerald-700 flex items-center justify-center">
            <Globe2 class="w-5 h-5" />
          </span>
          <div>
            <p class="text-2xl font-extrabold text-ifa-text-primary">{dashboard?.totals.publishedCourses ?? 0}</p>
            <p class="text-xs text-ifa-text-secondary">Published courses</p>
          </div>
        </div>
        <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card flex items-center gap-4">
          <span class="w-10 h-10 rounded-xl bg-blue-100 text-blue-700 flex items-center justify-center">
            <Users class="w-5 h-5" />
          </span>
          <div>
            <p class="text-2xl font-extrabold text-ifa-text-primary">{dashboard?.totals.enrollments ?? 0}</p>
            <p class="text-xs text-ifa-text-secondary">Total enrollments</p>
          </div>
        </div>
        <button
          type="button"
          on:click={() => goto('/studio/dashboard')}
          class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card flex items-center gap-4 text-left hover:border-amber-300 transition"
        >
          <span class="w-10 h-10 rounded-xl bg-amber-100 text-amber-700 flex items-center justify-center">
            <CheckCircle2 class="w-5 h-5" />
          </span>
          <div>
            <p class="text-2xl font-extrabold text-ifa-text-primary">{dashboard?.totals.pendingApprovals ?? 0}</p>
            <p class="text-xs text-ifa-text-secondary">Pending approvals</p>
          </div>
        </button>
        <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card flex items-center gap-4">
          <span class="w-10 h-10 rounded-xl bg-purple-100 text-purple-700 flex items-center justify-center">
            {#if generatingCount > 0}
              <Loader2 class="w-5 h-5 animate-spin" />
            {:else}
              <FolderOpen class="w-5 h-5" />
            {/if}
          </span>
          <div>
            <p class="text-2xl font-extrabold text-ifa-text-primary">{activeProjects.length}</p>
            <p class="text-xs text-ifa-text-secondary">
              {generatingCount > 0 ? `Active · ${generatingCount} generating` : 'Active projects'}
            </p>
          </div>
        </div>
      </div>

      <!-- Pending approvals (compact) -->
      {#if pending.length > 0}
        <section class="space-y-3">
          <div class="flex items-center justify-between">
            <h2 class="text-sm font-bold text-ifa-text-primary">Waiting for your approval</h2>
            <a href="/studio/dashboard" class="text-xs font-semibold text-ifa-pine hover:underline">View all</a>
          </div>
          <div class="bg-ifa-card rounded-2xl border border-amber-200 divide-y divide-ifa-border-light overflow-hidden">
            {#each pending.slice(0, 3) as enrollment (enrollment.enrollmentId)}
              <div class="flex flex-wrap items-center justify-between gap-3 p-4">
                <div class="min-w-0">
                  <p class="text-sm font-semibold text-ifa-text-primary truncate">{enrollment.learnerName}</p>
                  <p class="text-[11px] text-ifa-text-muted truncate">
                    {enrollment.courseTitle} · requested {new Date(enrollment.enrolledAt).toLocaleDateString()}
                  </p>
                </div>
                <div class="flex items-center gap-2">
                  <button
                    type="button"
                    on:click={() => handleDecision(enrollment.enrollmentId, true)}
                    disabled={actingOn !== null}
                    class="px-3 py-1.5 rounded-lg bg-ifa-pine text-white text-xs font-semibold hover:bg-ifa-pine-light transition disabled:opacity-50"
                  >
                    {#if actingOn === enrollment.enrollmentId}
                      <Loader2 class="w-3 h-3 animate-spin" />
                    {:else}
                      Approve
                    {/if}
                  </button>
                  <button
                    type="button"
                    on:click={() => handleDecision(enrollment.enrollmentId, false)}
                    disabled={actingOn !== null}
                    class="px-3 py-1.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary text-xs font-semibold hover:text-red-700 hover:border-red-200 transition disabled:opacity-50"
                  >
                    Reject
                  </button>
                </div>
              </div>
            {/each}
          </div>
        </section>
      {/if}

      <!-- Active projects -->
      <section class="space-y-3">
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-bold text-ifa-text-primary">Your projects</h2>
          <a href="/studio/projects" class="text-xs font-semibold text-ifa-pine hover:underline">All projects</a>
        </div>

        {#if projects.length === 0}
          <div class="text-center py-14 bg-ifa-card rounded-2xl border border-ifa-border">
            <GraduationCap class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
            <p class="text-sm font-semibold text-ifa-text-primary mb-1">Build your first course</p>
            <p class="text-xs text-ifa-text-secondary mb-5 max-w-sm mx-auto">
              Add links to your materials — PDFs, videos, websites, articles, slides — and IFA turns them into a
              structured course you review and publish.
            </p>
            <button
              type="button"
              on:click={() => (showNewProject = true)}
              class="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-ifa-pine text-white text-sm font-bold hover:bg-ifa-pine-light transition"
            >
              <Plus class="w-4 h-4" />
              <span>New course</span>
            </button>
          </div>
        {:else}
          <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {#each projects.slice(0, 6) as project (project.id)}
              <button
                type="button"
                on:click={() => goto(`/studio/${project.id}`)}
                class="text-left bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card hover:shadow-elevated hover:border-ifa-pine/40 transition group"
              >
                <div class="flex items-start justify-between gap-2 mb-2">
                  <h3 class="text-sm font-bold text-ifa-text-primary group-hover:text-ifa-pine transition">{project.title}</h3>
                  <span class="text-[10px] px-2 py-0.5 rounded-full font-semibold shrink-0 {statusBadge(project.status)}">
                    {project.status}
                  </span>
                </div>
                <p class="text-xs text-ifa-text-secondary line-clamp-2 mb-4">
                  {project.description || 'No description yet.'}
                </p>
                <div class="flex items-center justify-between text-[11px] text-ifa-text-muted">
                  <span>{project.sourceCount} sources</span>
                  <span class="flex items-center gap-1 font-semibold text-ifa-pine">
                    Open <ArrowRight class="w-3 h-3" />
                  </span>
                </div>
              </button>
            {/each}
          </div>
        {/if}
      </section>

      <!-- Quick links -->
      <section class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <a
          href="/studio/dashboard"
          class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card hover:border-ifa-pine/40 transition group"
        >
          <TrendingUp class="w-5 h-5 text-ifa-pine mb-2" />
          <p class="text-sm font-bold text-ifa-text-primary group-hover:text-ifa-pine transition">Studio dashboard</p>
          <p class="text-xs text-ifa-text-secondary mt-1">Traffic, progress and enrollment analytics.</p>
        </a>
        <a
          href="/studio/projects"
          class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card hover:border-ifa-pine/40 transition group"
        >
          <FolderOpen class="w-5 h-5 text-ifa-pine mb-2" />
          <p class="text-sm font-bold text-ifa-text-primary group-hover:text-ifa-pine transition">All projects</p>
          <p class="text-xs text-ifa-text-secondary mt-1">Every course build, draft to published.</p>
        </a>
        <a
          href="/marketplace"
          class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card hover:border-ifa-pine/40 transition group"
        >
          <BookOpen class="w-5 h-5 text-ifa-pine mb-2" />
          <p class="text-sm font-bold text-ifa-text-primary group-hover:text-ifa-pine transition">Marketplace</p>
          <p class="text-xs text-ifa-text-secondary mt-1">See how your published courses appear to learners.</p>
        </a>
      </section>
    {/if}

    <!-- New project modal -->
    {#if showNewProject}
      <div
        class="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
        role="dialog"
        aria-modal="true"
        aria-label="New course project"
        tabindex="-1"
        on:click={(e) => e.target === e.currentTarget && (showNewProject = false)}
        on:keydown={(e) => e.key === 'Escape' && (showNewProject = false)}
      >
        <div class="w-full max-w-md bg-ifa-card rounded-2xl border border-ifa-border shadow-2xl p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-bold text-ifa-text-primary">New course project</h3>
            <button type="button" on:click={() => (showNewProject = false)} class="p-1.5 text-ifa-text-muted hover:text-ifa-text-primary transition">
              <X class="w-5 h-5" />
            </button>
          </div>

          {#if error}
            <p role="alert" class="mb-3 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{error}</p>
          {/if}

          <div class="space-y-3">
            <div>
              <label for="project-title" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Title</label>
              <input
                id="project-title"
                type="text"
                bind:value={newTitle}
                placeholder="e.g. Introduction to Databases"
                class="w-full px-3 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine"
              />
            </div>
            <div>
              <label for="project-description" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Description</label>
              <textarea
                id="project-description"
                bind:value={newDescription}
                rows="3"
                placeholder="What should students learn? Who is it for?"
                class="w-full px-3 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine resize-none"
              ></textarea>
            </div>
            <button
              type="button"
              on:click={handleCreate}
              disabled={!newTitle.trim() || creating}
              class="w-full py-2.5 rounded-xl bg-ifa-pine text-white text-sm font-bold flex items-center justify-center gap-2 hover:bg-ifa-pine-light transition disabled:opacity-60"
            >
              {#if creating}
                <Loader2 class="w-4 h-4 animate-spin" />
                <span>Creating…</span>
              {:else}
                <Plus class="w-4 h-4" />
                <span>Create project</span>
              {/if}
            </button>
          </div>
        </div>
      </div>
    {/if}
  {/if}
</div>
