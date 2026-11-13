<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { ArrowLeft, BookOpen, FileText, Sparkles, Loader2, Plus, Trash2, X } from 'lucide-svelte';
  import { ApiError, educatorApi, studioCoursesApi, type CourseTemplateDto } from '$lib/api';
  import { session, setActiveWorkspace } from '$lib/stores/sessionStore';
  import AssetUploader from '$lib/components/assets/AssetUploader.svelte';
  import { reveal } from '$lib/motion/premiumMotion';

  type Mode = 'Manual' | 'Materials' | 'AI';
  type MaterialModule = { id: string; title: string; sourceUrls: string; assets: Array<{ id: string; name: string }> };

  const modes: Array<{ id: Mode; label: string; description: string; icon: typeof BookOpen }> = [
    { id: 'Manual', label: 'Manual creation', description: 'Build the structure yourself and optionally start from a template.', icon: BookOpen },
    { id: 'Materials', label: 'Material-based', description: 'Organize mixed files and links by module, then let AI analyze them.', icon: FileText },
    { id: 'AI', label: 'AI course creation', description: 'Describe your goals and resources, then review an editable blueprint.', icon: Sparkles }
  ];

  let mode: Mode = 'Manual';
  let workspaceId = '';
  let title = '';
  let description = '';
  let prompt = '';
  let category = 'General';
  let targetAudience = 'All Learners';
  let objectives = '';
  let templateId = '';
  let templates: CourseTemplateDto[] = [];
  let materialModules: MaterialModule[] = [newMaterialModule(1)];
  let submitting = false;
  let error = '';

  $: if ($session.activeWorkspaceId && workspaceId !== $session.activeWorkspaceId) {
    workspaceId = $session.activeWorkspaceId;
    materialModules = materialModules.map((module) => ({ ...module, assets: [] }));
  }

  onMount(async () => {
    try {
      templates = await educatorApi.templates();
    } catch {
      templates = [];
    }
  });

  function newMaterialModule(index: number): MaterialModule {
    return { id: crypto.randomUUID(), title: `Module ${index}`, sourceUrls: '', assets: [] };
  }

  function addMaterialModule() {
    materialModules = [...materialModules, newMaterialModule(materialModules.length + 1)];
  }

  function removeMaterialModule(id: string) {
    if (materialModules.length === 1) return;
    materialModules = materialModules.filter((module) => module.id !== id);
  }

  function updateMaterialModule(id: string, patch: Partial<MaterialModule>) {
    materialModules = materialModules.map((module) => module.id === id ? { ...module, ...patch } : module);
  }

  function addAsset(moduleId: string, asset: { assetId: string; fileName: string }) {
    materialModules = materialModules.map((module) => module.id === moduleId && !module.assets.some((item) => item.id === asset.assetId)
      ? { ...module, assets: [...module.assets, { id: asset.assetId, name: asset.fileName }] }
      : module);
  }

  function changeWorkspace(event: Event) {
    setActiveWorkspace((event.currentTarget as HTMLSelectElement).value);
  }

  async function createCourse() {
    error = '';
    if (!workspaceId) {
      error = 'Choose a verified educator workspace.';
      return;
    }
    if (mode === 'AI' && !prompt.trim()) {
      error = 'Describe the course you want to create.';
      return;
    }
    const modules = materialModules.map((module) => ({
      ...module,
      urls: module.sourceUrls.split('\n').map((value) => value.trim()).filter(Boolean)
    }));
    if (mode === 'Materials' && !modules.some((module) => module.urls.length > 0 || module.assets.length > 0)) {
      error = 'Add at least one source link or clean uploaded material.';
      return;
    }

    submitting = true;
    try {
      const created = await studioCoursesApi.create({
        mode: mode === 'AI' ? 'Prompt' : mode === 'Manual' && templateId ? 'Template' : mode,
        workspaceId,
        title: title.trim(),
        description: description.trim(),
        prompt: prompt.trim(),
        category,
        targetAudience: targetAudience.trim(),
        learningObjectives: objectives.split('\n').map((value) => value.trim()).filter(Boolean),
        templateId: mode === 'Manual' && templateId ? templateId : undefined,
        sourceUrls: [],
        materialModules: mode === 'Materials' ? modules.map((module) => ({ moduleKey: module.id, title: module.title.trim(), sourceUrls: module.urls })) : []
      });
      for (const module of modules) {
        for (const asset of module.assets) {
          await studioCoursesApi.attachAssetSource(created.id, asset.id, { moduleKey: module.id, title: module.title.trim() });
        }
      }
      if (mode === 'Materials') {
        await studioCoursesApi.analyze(created.id);
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
            <select value={workspaceId} on:change={changeWorkspace} required class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal">
              {#each $session.workspaces ?? [] as workspace}
                <option value={workspace.id}>{workspace.name}</option>
              {/each}
            </select>
          </label>
          <label class="text-sm font-semibold">Category
            <input bind:value={category} maxlength="100" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" />
          </label>
        </div>

        {#if mode === 'Manual'}
          <label class="block text-sm font-semibold">Starting template <span class="font-normal text-ifa-text-muted">(optional)</span>
            <select bind:value={templateId} class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal">
              <option value="">Blank course</option>
              {#each templates as template}<option value={template.id}>{template.title} · {template.category}</option>{/each}
            </select>
          </label>
        {/if}

        <label class="block text-sm font-semibold">Course title <span class="font-normal text-ifa-text-muted">{mode === 'AI' ? '(AI can propose one)' : ''}</span>
          <input bind:value={title} maxlength="200" required={mode !== 'AI'} class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" />
        </label>
        <label class="block text-sm font-semibold">Description
          <textarea bind:value={description} maxlength="2000" rows="3" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal"></textarea>
        </label>

        {#if mode === 'AI'}
          <label class="block text-sm font-semibold">Course planning brief
            <textarea bind:value={prompt} maxlength="8000" rows="8" required class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" placeholder="Describe the goals, learning outcomes, target audience, teaching approach, references, YouTube channels, papers, and constraints. The generated blueprint remains editable before approval."></textarea>
          </label>
        {/if}

        {#if mode === 'Materials'}
          <section class="space-y-3" aria-labelledby="module-builder-heading">
            <div class="flex items-end justify-between gap-3"><div><h2 id="module-builder-heading" class="text-sm font-bold">Material module builder</h2><p class="mt-1 text-xs text-ifa-text-muted">Mix PDFs, DOCX, PPTX, images, papers, websites, and YouTube links inside each planned module.</p></div><button type="button" on:click={addMaterialModule} class="inline-flex items-center gap-1 rounded-lg border border-ifa-border px-3 py-2 text-xs font-bold"><Plus class="h-3.5 w-3.5" />Add module</button></div>
            {#each materialModules as materialModule, index (materialModule.id)}
              <article class="space-y-3 border border-ifa-border bg-ifa-card-muted p-4">
                <div class="flex items-center gap-2"><input aria-label={`Module ${index + 1} title`} value={materialModule.title} on:input={(event) => updateMaterialModule(materialModule.id, { title: (event.currentTarget as HTMLInputElement).value })} maxlength="200" class="min-w-0 flex-1 border border-ifa-border bg-ifa-card px-3 py-2 text-sm font-bold" /><button type="button" on:click={() => removeMaterialModule(materialModule.id)} disabled={materialModules.length === 1} aria-label={`Remove ${materialModule.title}`} class="p-2 text-red-700 disabled:opacity-30"><Trash2 class="h-4 w-4" /></button></div>
                <label class="block text-xs font-semibold">Websites, papers, and YouTube URLs <span class="font-normal text-ifa-text-muted">(one per line)</span><textarea value={materialModule.sourceUrls} on:input={(event) => updateMaterialModule(materialModule.id, { sourceUrls: (event.currentTarget as HTMLTextAreaElement).value })} rows="3" class="mt-2 w-full border border-ifa-border bg-ifa-card px-3 py-2 font-normal" placeholder="https://…"></textarea></label>
                {#if workspaceId}<AssetUploader purpose="CourseMaterial" {workspaceId} label={`Add files to ${materialModule.title}`} on:complete={(event) => addAsset(materialModule.id, event.detail)} />{/if}
                {#if materialModule.assets.length}<div class="flex flex-wrap gap-2">{#each materialModule.assets as asset}<span class="inline-flex items-center gap-1 rounded-full bg-ifa-card px-3 py-1 text-xs">{asset.name}<button type="button" aria-label={`Remove ${asset.name}`} on:click={() => updateMaterialModule(materialModule.id, { assets: materialModule.assets.filter((item) => item.id !== asset.id) })}><X class="h-3 w-3" /></button></span>{/each}</div>{/if}
              </article>
            {/each}
          </section>
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
          {submitting ? 'Preparing course…' : mode === 'Materials' ? 'Analyze materials & create blueprint' : mode === 'AI' ? 'Create AI blueprint' : 'Create manual course'}
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
  .mode-grid{grid-template-columns:repeat(3,1fr)}
  @media(max-width:1100px){.mode-grid{grid-template-columns:repeat(3,1fr)}}@media(max-width:760px){.create-shell{padding:1rem}.create-title{font-size:4rem}.mode-grid{grid-template-columns:1fr}.mode-card{min-height:9rem}.create-shell>.mt-6{display:block}.process-note{position:static;margin-top:1rem}}
</style>
