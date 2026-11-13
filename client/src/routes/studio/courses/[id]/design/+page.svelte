<script lang="ts">
  import { onMount } from 'svelte';
  import { beforeNavigate } from '$app/navigation';
  import { page } from '$app/stores';
  import { ArrowLeft, ExternalLink, RotateCcw, Save } from 'lucide-svelte';
  import { apiErrorMessage, coursesApi, educatorApi, type CourseAppearanceDto, type CourseDetailDto } from '$lib/api';
  import CourseCanvasPreview from '$lib/components/course/CourseCanvasPreview.svelte';
  import { COURSE_FONT_COMBINATIONS, COURSE_PALETTES, DEFAULT_COURSE_APPEARANCE, normalizeCourseAppearance } from '$lib/courseAppearance';

  const tabs = ['Canvas', 'Colors', 'Typography', 'Styles'] as const;
  let activeTab: typeof tabs[number] = 'Canvas';
  let course: CourseDetailDto | null = null;
  let appearance: CourseAppearanceDto = { ...DEFAULT_COURSE_APPEARANCE };
  let savedAppearance = JSON.stringify(appearance);
  let loading = true;
  let saving = false;
  let error = '';
  let notice = '';
  $: courseId = $page.params.id ?? '';
  $: dirty = JSON.stringify(appearance) !== savedAppearance;

  beforeNavigate((navigation) => {
    if (dirty && !saving && !confirm('Leave Canvas Studio and discard unapplied design changes?')) navigation.cancel();
  });

  onMount(async () => {
    try {
      const loadedCourse = await coursesApi.getById(courseId);
      course = loadedCourse;
      appearance = normalizeCourseAppearance(loadedCourse.appearance);
      savedAppearance = JSON.stringify(appearance);
    } catch (cause) {
      error = apiErrorMessage(cause, 'Canvas Studio could not load this course.');
    } finally {
      loading = false;
    }
  });

  function applyPalette(palette: typeof COURSE_PALETTES[number]) {
    appearance = { ...appearance, ...palette };
  }

  function applyFonts(fonts: typeof COURSE_FONT_COMBINATIONS[number]) {
    appearance = { ...appearance, headingFont: fonts.headingFont, bodyFont: fonts.bodyFont };
  }

  async function applyDesign() {
    if (!course || course.isPublished || saving) return;
    saving = true;
    error = '';
    notice = '';
    try {
      const result = await educatorApi.updateAppearance(course.id, appearance);
      appearance = normalizeCourseAppearance(result.appearance);
      savedAppearance = JSON.stringify(appearance);
      notice = 'Design applied to this draft. It will be frozen into the next published release.';
    } catch (cause) {
      error = apiErrorMessage(cause, 'The course design could not be applied.');
    } finally {
      saving = false;
    }
  }

  async function resetDesign() {
    if (!course || course.isPublished || !confirm('Reset this draft to the current Witforge course design?')) return;
    saving = true;
    error = '';
    try {
      await educatorApi.resetAppearance(course.id);
      appearance = { ...DEFAULT_COURSE_APPEARANCE };
      savedAppearance = JSON.stringify(appearance);
      notice = 'The course will use the standard Witforge appearance.';
    } catch (cause) {
      error = apiErrorMessage(cause, 'The design could not be reset.');
    } finally {
      saving = false;
    }
  }
</script>

<svelte:head><title>Canvas Studio · {course?.title ?? 'Witforge'}</title></svelte:head>

<main class="canvas-studio">
  <header class="studio-header">
    <div><a href={`/studio/courses/${courseId}/edit`}><ArrowLeft />Back to course</a><p>Canvas Studio</p><h1>{course?.title ?? 'Course design'}</h1><span>Customize presentation without changing lesson content, learner progress, or enrollment data.</span></div>
    <div class="header-actions"><a href={`/courses/${courseId}`} target="_blank" rel="noreferrer"><ExternalLink />Preview course</a><button type="button" on:click={resetDesign} disabled={saving || course?.isPublished}><RotateCcw />Reset</button><button type="button" class="apply" on:click={applyDesign} disabled={!dirty || saving || course?.isPublished}><Save />{saving ? 'Applying…' : 'Apply to course'}</button></div>
  </header>

  {#if course?.isPublished}<p role="alert" class="published-warning">This published release keeps its stable design. Unpublish the course before applying a new appearance, then review and publish a new release.</p>{/if}
  {#if error}<p role="alert" class="error">{error}</p>{/if}
  {#if notice}<p role="status" class="notice">{notice}</p>{/if}

  {#if loading}
    <p class="loading">Loading Canvas Studio…</p>
  {:else if course}
    <div class="studio-grid">
      <section class="controls">
        <nav aria-label="Canvas Studio sections">{#each tabs as tab}<button type="button" class:active={activeTab === tab} on:click={() => activeTab = tab}>{tab}</button>{/each}</nav>

        {#if activeTab === 'Canvas'}
          <fieldset><legend>Course layout</legend><p>Layouts change how navigation and lesson content are structured.</p><div class="choice-grid">{#each ['Classic', 'Sidebar', 'Editorial', 'Presentation'] as layout}<button type="button" class:selected={appearance.canvasLayout === layout} on:click={() => appearance = { ...appearance, canvasLayout: layout as CourseAppearanceDto['canvasLayout'] }}><strong>{layout}</strong><span>{layout === 'Classic' ? 'Familiar header and tabs' : layout === 'Sidebar' ? 'Persistent lesson navigation' : layout === 'Editorial' ? 'Reading-focused composition' : 'Large visual lesson stage'}</span></button>{/each}</div></fieldset>
          <label>Content width<select bind:value={appearance.contentWidth}><option>Narrow</option><option>Standard</option><option>Wide</option></select></label>
          <label>Content spacing<select bind:value={appearance.spacing}><option>Compact</option><option>Comfortable</option><option>Airy</option></select></label>
        {:else if activeTab === 'Colors'}
          <fieldset><legend>Built-in palettes</legend><p>Start with an accessible palette, then fine-tune individual colors.</p><div class="palette-grid">{#each COURSE_PALETTES as palette}<button type="button" on:click={() => applyPalette(palette)}><span style={`background:${palette.primaryColor}`}></span><span style={`background:${palette.secondaryColor}`}></span><strong>{palette.name}</strong></button>{/each}</div></fieldset>
          <div class="color-grid"><label>Primary<input type="color" bind:value={appearance.primaryColor} /></label><label>Secondary<input type="color" bind:value={appearance.secondaryColor} /></label><label>Background<input type="color" bind:value={appearance.backgroundColor} /></label><label>Surface<input type="color" bind:value={appearance.surfaceColor} /></label><label>Text<input type="color" bind:value={appearance.textColor} /></label></div>
        {:else if activeTab === 'Typography'}
          <fieldset><legend>Font combinations</legend><div class="font-list">{#each COURSE_FONT_COMBINATIONS as fonts}<button type="button" on:click={() => applyFonts(fonts)} class:selected={appearance.headingFont === fonts.headingFont && appearance.bodyFont === fonts.bodyFont}><strong>{fonts.name}</strong><span>{fonts.headingFont} headings · {fonts.bodyFont} body</span></button>{/each}</div></fieldset>
          <label>Heading font<select bind:value={appearance.headingFont}><option value="Manrope">Manrope</option><option value="Inter">Inter</option><option value="SourceSerif">Source Serif</option><option value="Merriweather">Merriweather</option><option value="IbmPlexSans">IBM Plex Sans</option></select></label>
          <label>Body font<select bind:value={appearance.bodyFont}><option value="Inter">Inter</option><option value="Manrope">Manrope</option><option value="SourceSerif">Source Serif</option><option value="Merriweather">Merriweather</option><option value="IbmPlexSans">IBM Plex Sans</option></select></label>
        {:else}
          <label>Cards<select bind:value={appearance.cardStyle}><option>Flat</option><option>Rounded</option><option>Elevated</option></select></label>
          <label>Buttons<select bind:value={appearance.buttonStyle}><option>Solid</option><option>Soft</option><option>Outline</option></select></label>
          <label>Borders<select bind:value={appearance.borderStyle}><option>None</option><option>Subtle</option><option>Strong</option></select></label>
          <div class="phase-note"><strong>Reusable design presets</strong><p>Saving, duplicating, and versioning cross-course presets is intentionally reserved for Phase 2 so this first release stays backward-compatible and reviewable.</p></div>
        {/if}
      </section>

      <section class="preview-panel"><div><span>Live course preview</span><span>{appearance.canvasLayout} · {appearance.spacing}</span></div><CourseCanvasPreview {appearance} title={course.title} /></section>
    </div>
  {/if}
</main>

<style>
  .canvas-studio{width:min(100%,96rem);margin:0 auto;padding:clamp(1rem,3vw,2.5rem);color:var(--wf-text)}.studio-header{display:flex;align-items:end;justify-content:space-between;gap:2rem;padding-bottom:1.4rem;border-bottom:1px solid var(--wf-border)}.studio-header a{display:inline-flex;align-items:center;gap:.35rem;font-size:.62rem;font-weight:800}.studio-header a :global(svg),.header-actions :global(svg){width:.85rem}.studio-header p{margin-top:2rem;color:#176b4a;font-size:.58rem;font-weight:900;letter-spacing:.14em;text-transform:uppercase}.studio-header h1{margin-top:.4rem;font-size:clamp(2rem,4vw,4rem);font-weight:500;line-height:.95}.studio-header>div>span{display:block;max-width:44rem;margin-top:.7rem;color:var(--wf-text-muted);font-size:.68rem;line-height:1.6}.header-actions{display:flex;flex-wrap:wrap;justify-content:end;gap:.5rem}.header-actions a,.header-actions button{display:inline-flex;align-items:center;gap:.4rem;min-height:2.5rem;padding:0 .9rem;border:1px solid var(--wf-border);background:var(--wf-surface-raised);font-size:.62rem;font-weight:800}.header-actions .apply{color:#17221c;background:#c7f464;border-color:#c7f464}.header-actions button:disabled{opacity:.45}.published-warning,.error,.notice{margin-top:1rem;padding:.8rem 1rem;border:1px solid;font-size:.65rem}.published-warning{border-color:#d9a441;background:#fff6d8;color:#6b4d00}.error{border-color:#e4a7a2;background:#fff0ef;color:#8c1d18}.notice{border-color:#79af95;background:#edf8f1;color:#145c40}.loading{padding:5rem;text-align:center;color:var(--wf-text-muted)}.studio-grid{display:grid;grid-template-columns:23rem minmax(0,1fr);gap:1.3rem;margin-top:1.3rem}.controls,.preview-panel{border:1px solid var(--wf-border);background:var(--wf-surface-raised)}.controls{padding:1rem}.controls nav{display:grid;grid-template-columns:repeat(4,1fr);gap:1px;margin:-1rem -1rem 1.3rem;background:var(--wf-border)}.controls nav button{padding:.8rem .25rem;background:var(--wf-surface-raised);font-size:.58rem;font-weight:800}.controls nav button.active{color:#17221c;background:#c7f464}.controls fieldset,.controls>label{display:block;margin-top:1.2rem}.controls legend,.controls>label{font-size:.66rem;font-weight:850}.controls fieldset>p{margin-top:.35rem;color:var(--wf-text-muted);font-size:.58rem;line-height:1.5}.controls select{width:100%;margin-top:.4rem;padding:.7rem;border:1px solid var(--wf-border);background:var(--wf-surface-canvas);font-size:.65rem}.choice-grid{display:grid;grid-template-columns:1fr 1fr;gap:.5rem;margin-top:.8rem}.choice-grid button,.font-list button{padding:.8rem;border:1px solid var(--wf-border);text-align:left}.choice-grid button.selected,.font-list button.selected{border-color:#176b4a;box-shadow:inset 0 0 0 1px #176b4a}.choice-grid strong,.choice-grid span,.font-list strong,.font-list span{display:block}.choice-grid strong,.font-list strong{font-size:.64rem}.choice-grid span,.font-list span{margin-top:.25rem;color:var(--wf-text-muted);font-size:.54rem;line-height:1.4}.palette-grid{display:grid;gap:.5rem;margin-top:.8rem}.palette-grid button{display:grid;grid-template-columns:1.5rem 1.5rem 1fr;align-items:center;gap:.4rem;padding:.55rem;border:1px solid var(--wf-border);text-align:left}.palette-grid button span{height:1.5rem}.palette-grid button strong{font-size:.58rem}.color-grid{display:grid;grid-template-columns:1fr 1fr;gap:.5rem;margin-top:1.2rem}.color-grid label{display:flex;align-items:center;justify-content:space-between;padding:.65rem;border:1px solid var(--wf-border);font-size:.58rem;font-weight:750}.color-grid input{width:2rem;height:1.5rem}.font-list{display:grid;gap:.5rem;margin-top:.8rem}.phase-note{margin-top:1.3rem;padding:1rem;background:var(--wf-surface-muted)}.phase-note strong{font-size:.65rem}.phase-note p{margin-top:.35rem;color:var(--wf-text-muted);font-size:.57rem;line-height:1.5}.preview-panel{position:sticky;top:5rem;height:max-content;padding:1rem}.preview-panel>div{display:flex;justify-content:space-between;margin-bottom:.7rem;color:var(--wf-text-muted);font-size:.56rem;font-weight:800;text-transform:uppercase}
  @media(max-width:980px){.studio-header{align-items:start;flex-direction:column}.header-actions{justify-content:start}.studio-grid{grid-template-columns:1fr}.preview-panel{position:static;grid-row:1}.controls nav{position:sticky;top:0;z-index:2}}@media(max-width:560px){.canvas-studio{padding:.8rem}.choice-grid,.color-grid{grid-template-columns:1fr}.header-actions{display:grid;grid-template-columns:1fr 1fr;width:100%}.header-actions .apply{grid-column:1/-1}.preview-panel{padding:.5rem}}
</style>
