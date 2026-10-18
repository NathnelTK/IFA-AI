<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { Plus, FolderOpen, Loader2, X, ArrowRight, ShieldAlert } from 'lucide-svelte';
  import { educatorApi, type EducatorProjectDto } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';

  let projects: EducatorProjectDto[] = [];
  let loading = true;
  let error = '';
  let showNewProject = false;
  let newTitle = '';
  let newDescription = '';
  let creating = false;

  onMount(async () => {
    await loadProjects();
  });

  async function loadProjects() {
    loading = true;
    error = '';
    try {
      projects = await educatorApi.projects();
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load your projects.';
    } finally {
      loading = false;
    }
  }

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

  function statusBadge(status: string): string {
    switch (status?.toLowerCase()) {
      case 'readyforreview':
      case 'published':
        return 'bg-emerald-100 text-emerald-800';
      case 'generating':
        return 'bg-blue-100 text-blue-800';
      case 'awaitingresolution':
        return 'bg-amber-100 text-amber-800';
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
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-ifa-text-primary">Projects</h1>
        <p class="text-sm text-ifa-text-secondary mt-1">
          Every course build — sources, conflicts, blueprint and generation.
        </p>
      </div>
      <button
        type="button"
        on:click={() => (showNewProject = true)}
        class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-ifa-pine text-white text-sm font-bold hover:bg-ifa-pine-light transition shadow-soft"
      >
        <Plus class="w-4 h-4" />
        <span>New project</span>
      </button>
    </div>

    {#if error}
      <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>
    {/if}

    {#if loading}
      <div class="flex items-center justify-center py-16">
        <Loader2 class="w-6 h-6 text-ifa-pine animate-spin" />
      </div>
    {:else if projects.length === 0}
      <div class="text-center py-16 bg-ifa-card rounded-2xl border border-ifa-border">
        <FolderOpen class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
        <p class="text-ifa-text-secondary">No projects yet. Create one and start adding sources.</p>
      </div>
    {:else}
      <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        {#each projects as project (project.id)}
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
