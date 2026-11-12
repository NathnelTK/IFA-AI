<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { ArrowLeft, BookOpen, FileText, Mic2, Sparkles, LayoutTemplate, Loader2, X } from 'lucide-svelte';
  import { ApiError, educatorApi, studioCoursesApi, type CourseTemplateDto } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';
  import AssetUploader from '$lib/components/assets/AssetUploader.svelte';
  import { reveal } from '$lib/motion/premiumMotion';

  type Mode = 'Manual' | 'Prompt' | 'Materials' | 'Template' | 'Voice';

  const modes: Array<{ id: Mode; label: string; description: string; icon: typeof BookOpen }> = [
    { id: 'Manual', label: 'Manual', description: 'Start with an editable module and lesson.', icon: BookOpen },
    { id: 'Prompt', label: 'Prompt', description: 'Queue an AI-assisted course outline.', icon: Sparkles },
    { id: 'Materials', label: 'Materials', description: 'Ground a draft in links and clean uploads.', icon: FileText },
    { id: 'Template', label: 'Template', description: 'Copy a deterministic curriculum snapshot.', icon: LayoutTemplate },
    { id: 'Voice', label: 'Voice brief', description: 'Use a reviewed transcript as the same structured prompt.', icon: Mic2 }
  ];

  let mode: Mode = 'Manual';
  let workspaceId = '';
  let title = '';
  let description = '';
  let prompt = '';
  let category = 'General';
  let targetAudience = 'All Learners';
  let objectives = '';
  let sourceUrls = '';
  let templateId = '';
  let templates: CourseTemplateDto[] = [];
  let assetIds: string[] = [];
  let submitting = false;
  let error = '';

  $: if (!workspaceId && $session.workspaces?.[0]) workspaceId = $session.workspaces[0].id;

  onMount(async () => {
    try {
      templates = await educatorApi.templates();
    } catch {
      templates = [];
    }
  });

  function addAsset(assetId: string) {
    if (!assetIds.includes(assetId)) assetIds = [...assetIds, assetId];
  }

  async function createCourse() {
    error = '';
    if (!workspaceId) {
      error = 'Choose a verified educator workspace.';
      return;
    }
    if (mode === 'Template' && !templateId) {
      error = 'Choose a course template.';
      return;
    }
    if ((mode === 'Prompt' || mode === 'Voice') && !prompt.trim()) {
      error = 'Describe the course you want to create.';
      return;
    }
    const urls = sourceUrls.split('\n').map((value) => value.trim()).filter(Boolean);
    if (mode === 'Materials' && urls.length === 0 && assetIds.length === 0) {
      error = 'Add at least one source link or clean uploaded material.';
      return;
    }

    submitting = true;
    try {
      const created = await studioCoursesApi.create({
        mode,
        workspaceId,
        title: title.trim(),
        description: description.trim(),
        prompt: prompt.trim(),
        category,
        targetAudience: targetAudience.trim(),
        learningObjectives: objectives.split('\n').map((value) => value.trim()).filter(Boolean),
        templateId: mode === 'Template' ? templateId : undefined,
        sourceUrls: mode === 'Materials' ? urls : []
      });
      for (const assetId of assetIds) {
        await studioCoursesApi.attachAssetSource(created.id, assetId);
      }
      if (mode === 'Materials' && urls.length === 0 && assetIds.length > 0) {
        try { await studioCoursesApi.analyze(created.id); } catch { /* extraction may still be finishing */ }
      }
      await goto(`/studio/courses/${created.id}/edit`);
    } catch (cause) {
      if (cause instanceof ApiError && cause.status === 404) {
        await goto('/studio/legacy-create', { replaceState: true });
        return;
      }
      error = cause instanceof Error ? cause.message : 'The course draft could not be created.';
    } finally {
      submitting = false;
    }
  }
</script>

<svelte:head><title>Create course · Witforge Studio</title></svelte:head>

<div class="create-shell">
  <a href="/studio/courses" class="inline-flex items-center gap-2 text-sm font-semibold text-ifa-text-secondary hover:text-ifa-pine"><ArrowLeft class="h-4 w-4" />Back to courses</a>
  <div class="mt-6 grid gap-8 lg:grid-cols-[minmax(0,1fr)_20rem]">
    <main use:reveal>
      <p class="wf-kicker">New course draft</p>
      <h1 class="wf-display create-title">Choose how to <em>begin.</em></h1>
      <p class="create-intro">The starting method changes. The saved course model, review controls and publishing standards do not.</p>
      <div class="mode-grid">
        {#each modes as option}
          <button
            type="button"
            on:click={() => (mode = option.id)}
            aria-pressed={mode === option.id}
            class:active={mode === option.id}
            class="mode-card"
          >
            <svelte:component this={option.icon} class="h-5 w-5 text-ifa-pine" />
            <span class="mt-3 block font-bold text-ifa-text-primary">{option.label}</span>
            <span class="mt-1 block text-xs leading-relaxed text-ifa-text-secondary">{option.description}</span>
          </button>
        {/each}
      </div>

      <form class="create-form space-y-5" on:submit|preventDefault={createCourse}>
        <div class="grid gap-5 sm:grid-cols-2">
          <label class="text-sm font-semibold">Workspace
            <select bind:value={workspaceId} required class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal">
              {#each $session.workspaces ?? [] as workspace}
                <option value={workspace.id}>{workspace.name}</option>
              {/each}
            </select>
          </label>
          <label class="text-sm font-semibold">Category
            <input bind:value={category} maxlength="100" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" />
          </label>
        </div>

        {#if mode === 'Template'}
          <label class="block text-sm font-semibold">Template
            <select bind:value={templateId} required class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal">
              <option value="">Choose a template</option>
              {#each templates as template}<option value={template.id}>{template.title} · {template.category}</option>{/each}
            </select>
          </label>
        {/if}

        <label class="block text-sm font-semibold">Course title <span class="font-normal text-ifa-text-muted">{mode === 'Prompt' || mode === 'Voice' ? '(optional)' : ''}</span>
          <input bind:value={title} maxlength="200" required={mode === 'Manual'} class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" />
        </label>
        <label class="block text-sm font-semibold">Description
          <textarea bind:value={description} maxlength="2000" rows="3" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal"></textarea>
        </label>

        {#if mode === 'Prompt' || mode === 'Voice'}
          <label class="block text-sm font-semibold">{mode === 'Voice' ? 'Reviewed voice transcript' : 'Course brief'}
            <textarea bind:value={prompt} maxlength="8000" rows="7" required class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" placeholder="Describe the learning outcome, scope, and constraints."></textarea>
          </label>
        {/if}

        {#if mode === 'Materials'}
          <label class="block text-sm font-semibold">Public source links <span class="font-normal text-ifa-text-muted">(one per line)</span>
            <textarea bind:value={sourceUrls} rows="5" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" placeholder="https://…"></textarea>
          </label>
          {#if workspaceId}
            <AssetUploader purpose="CourseMaterial" {workspaceId} label="Upload PDF, PPTX, text, or image material" on:complete={(event) => addAsset(event.detail.assetId)} />
          {/if}
          {#if assetIds.length > 0}
            <div class="flex flex-wrap gap-2">
              {#each assetIds as assetId}
                <span class="inline-flex items-center gap-1 rounded-full bg-ifa-card-muted px-3 py-1 text-xs">Clean asset {assetId.slice(0, 8)} <button type="button" aria-label="Remove asset" on:click={() => (assetIds = assetIds.filter((id) => id !== assetId))}><X class="h-3 w-3" /></button></span>
              {/each}
            </div>
          {/if}
        {/if}

        <div class="grid gap-5 sm:grid-cols-2">
          <label class="text-sm font-semibold">Target audience
            <input bind:value={targetAudience} maxlength="300" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" />
          </label>
          <label class="text-sm font-semibold">Learning objectives <span class="font-normal text-ifa-text-muted">(one per line)</span>
            <textarea bind:value={objectives} rows="4" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal"></textarea>
          </label>
        </div>

        {#if error}<p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-800">{error}</p>{/if}
        <button disabled={submitting} class="create-submit">
          {#if submitting}<Loader2 class="h-4 w-4 animate-spin" />{/if}
          {submitting ? 'Creating saved draft…' : 'Create saved draft'}
        </button>
      </form>
    </main>

    <aside class="process-note" use:reveal={{ delay: .14 }}>
      <span>03 / Process</span><h2 class="wf-display">What happens next</h2>
      <ol class="mt-4 space-y-4 text-sm text-ifa-text-secondary">
        <li><strong class="text-ifa-text-primary">1. Saved immediately.</strong> Every mode creates a relational course and authoring project.</li>
        <li><strong class="text-ifa-text-primary">2. Processing is durable.</strong> Analysis, extraction, and generation run in leased worker jobs.</li>
        <li><strong class="text-ifa-text-primary">3. You approve.</strong> AI outlines never publish themselves or overwrite edited content.</li>
      </ol>
    </aside>
  </div>
</div>

<style>
  .create-shell{width:min(100%,84rem);margin:0 auto;padding:clamp(1.2rem,3vw,3rem)}.create-shell>a{font-size:.65rem}.create-shell>.mt-6{margin-top:2.5rem}.create-shell main>.wf-kicker{color:var(--wf-text-muted)}.create-title{max-width:48rem;margin:1rem 0 .7rem;font-size:clamp(3.5rem,6vw,6.8rem);font-weight:430;line-height:.84;letter-spacing:-.055em}.create-title em{color:#176b4a;font-weight:430}.create-intro{max-width:45rem;color:var(--wf-text-muted);font-size:.75rem;line-height:1.7}.mode-grid{display:grid;grid-template-columns:repeat(5,1fr);gap:1px;margin-top:3rem;border:1px solid var(--wf-border);background:var(--wf-border)}.mode-card{display:flex;min-height:10.5rem;flex-direction:column;padding:1rem;border:0;background:var(--wf-surface-raised);text-align:left;transition:background .2s,color .2s}.mode-card:hover{background:var(--wf-surface-muted)}.mode-card.active{color:#17221c;background:#c7f464}.mode-card :global(svg){width:1.05rem}.mode-card>span:nth-of-type(1){margin-top:auto!important;font-size:.72rem}.mode-card>span:nth-of-type(2){font-size:.56rem!important;line-height:1.5}.create-form{margin-top:1px;padding:clamp(1.3rem,3vw,2.2rem);border:1px solid var(--wf-border);background:var(--wf-surface-raised)}.create-form :global(label){font-size:.67rem!important}.create-form :global(input),.create-form :global(textarea),.create-form :global(select){border-radius:0!important;background:var(--wf-surface-canvas)!important;font-size:.7rem}.create-submit{display:inline-flex;align-items:center;gap:.55rem;min-height:2.9rem;padding:0 1.15rem;border-radius:999px;color:#17221c;background:#c7f464;font-size:.68rem;font-weight:850}.create-submit:disabled{opacity:.55}.create-submit :global(svg){width:.9rem}.process-note{position:sticky;top:6rem;height:max-content;padding:1.6rem;border:1px solid var(--wf-border);background:#17221c;color:#f3f0e8}.process-note>span{color:#c7f464;font-size:.56rem;font-weight:850;letter-spacing:.13em;text-transform:uppercase}.process-note h2{margin:3.5rem 0 1.5rem;font-size:2.4rem;font-weight:450;line-height:.95}.process-note ol{color:#aeb8b1!important;font-size:.66rem!important;line-height:1.65}.process-note li{padding:1rem 0;border-top:1px solid #3c4941}.process-note strong{display:block!important;margin-bottom:.3rem;color:#f3f0e8!important}
  @media(max-width:1100px){.mode-grid{grid-template-columns:repeat(3,1fr)}}@media(max-width:760px){.create-shell{padding:1rem}.create-title{font-size:4rem}.mode-grid{grid-template-columns:1fr 1fr}.mode-card{min-height:9rem}.create-shell>.mt-6{display:block}.process-note{position:static;margin-top:1rem}}
</style>
