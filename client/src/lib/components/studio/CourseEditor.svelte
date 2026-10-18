<script lang="ts">
  import { onMount } from 'svelte';
  import { Loader2, Save, Sparkles, ChevronDown, ChevronRight, BookOpen } from 'lucide-svelte';
  import { coursesApi, educatorApi, type CourseDetailDto } from '$lib/api';

  export let courseId: string;

  let course: CourseDetailDto | null = null;
  let loading = true;
  let error = '';
  let notice = '';
  let savingModuleId: string | null = null;
  let aiBusyModuleId: string | null = null;
  let expandedModules: Record<string, boolean> = {};

  // Local edit buffers keyed by entity id.
  let moduleTitles: Record<string, string> = {};
  let lessonContents: Record<string, string> = {};
  let aiInstructions: Record<string, string> = {};

  onMount(loadCourse);

  async function loadCourse() {
    loading = true;
    error = '';
    try {
      course = await coursesApi.getById(courseId);
      const titles: Record<string, string> = {};
      const contents: Record<string, string> = {};
      const expanded: Record<string, boolean> = {};
      for (const mod of course.modules) {
        titles[mod.id] = mod.title;
        expanded[mod.id] = false;
        for (const lesson of mod.lessons) {
          contents[lesson.id] = lesson.contentMarkdown || '';
        }
      }
      moduleTitles = titles;
      lessonContents = contents;
      expandedModules = expanded;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load the course.';
    } finally {
      loading = false;
    }
  }

  function toggleModule(moduleId: string) {
    expandedModules = { ...expandedModules, [moduleId]: !expandedModules[moduleId] };
  }

  async function saveModule(moduleId: string) {
    if (!course || savingModuleId) return;
    savingModuleId = moduleId;
    notice = '';
    error = '';
    try {
      const mod = course.modules.find((m) => m.id === moduleId);
      if (!mod) return;
      await educatorApi.updateModule(courseId, moduleId, {
        title: moduleTitles[moduleId] ?? mod.title,
        lessons: mod.lessons.map((lesson, index) => ({
          id: lesson.id,
          title: lesson.title,
          content: lessonContents[lesson.id] ?? lesson.contentMarkdown ?? '',
          order: index
        }))
      });
      notice = `Module "${moduleTitles[moduleId] ?? mod.title}" saved.`;
      await loadCourse();
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not save the module.';
    } finally {
      savingModuleId = null;
    }
  }

  async function aiUpdateModule(moduleId: string) {
    const instruction = (aiInstructions[moduleId] ?? '').trim();
    if (!course || !instruction || aiBusyModuleId) return;
    aiBusyModuleId = moduleId;
    notice = '';
    error = '';
    try {
      await educatorApi.aiUpdateModule(courseId, moduleId, instruction);
      aiInstructions = { ...aiInstructions, [moduleId]: '' };
      notice = 'The AI updated the module — reload applied.';
      await loadCourse();
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The AI update failed.';
    } finally {
      aiBusyModuleId = null;
    }
  }
</script>

<section class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card space-y-3">
  <div class="flex items-center justify-between">
    <h2 class="text-sm font-bold text-ifa-text-primary flex items-center gap-2">
      <BookOpen class="w-4 h-4 text-ifa-pine" />
      Edit course content
    </h2>
    <button type="button" on:click={loadCourse} class="text-[11px] font-semibold text-ifa-pine hover:underline">
      Reload
    </button>
  </div>

  {#if error}
    <p role="alert" class="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{error}</p>
  {/if}
  {#if notice}
    <p class="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800">{notice}</p>
  {/if}

  {#if loading}
    <div class="flex items-center justify-center py-8">
      <Loader2 class="w-5 h-5 text-ifa-pine animate-spin" />
    </div>
  {:else if course}
    <div class="divide-y divide-ifa-border-light">
      {#each course.modules as mod (mod.id)}
        <div class="py-3">
          <button
            type="button"
            on:click={() => toggleModule(mod.id)}
            class="w-full flex items-center justify-between gap-2 text-left"
          >
            <span class="flex items-center gap-2 text-xs font-bold text-ifa-text-primary">
              {#if expandedModules[mod.id]}
                <ChevronDown class="w-3.5 h-3.5 text-ifa-text-muted" />
              {:else}
                <ChevronRight class="w-3.5 h-3.5 text-ifa-text-muted" />
              {/if}
              Module {mod.moduleNumber}: {mod.title}
            </span>
            <span class="text-[10px] text-ifa-text-muted shrink-0">{mod.lessons.length} lessons</span>
          </button>

          {#if expandedModules[mod.id]}
            <div class="mt-3 space-y-3 pl-5">
              <div>
                <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1" for="module-title-{mod.id}">Module title</label>
                <input
                  id="module-title-{mod.id}"
                  type="text"
                  bind:value={moduleTitles[mod.id]}
                  class="w-full px-3 py-2 rounded-xl bg-ifa-card-muted border border-ifa-border text-xs text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
                />
              </div>

              {#each mod.lessons as lesson (lesson.id)}
                <div>
                  <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1" for="lesson-content-{lesson.id}">
                    Lesson: {lesson.title}
                  </label>
                  <textarea
                    id="lesson-content-{lesson.id}"
                    bind:value={lessonContents[lesson.id]}
                    rows="6"
                    placeholder="Lesson content (markdown)…"
                    class="w-full px-3 py-2 rounded-xl bg-ifa-card-muted border border-ifa-border text-xs font-mono text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine resize-y"
                  ></textarea>
                </div>
              {/each}

              <!-- AI update -->
              <div class="flex items-center gap-2">
                <input
                  type="text"
                  bind:value={aiInstructions[mod.id]}
                  placeholder="Ask AI to update this module — e.g. 'add a hands-on exercise to lesson 2'"
                  class="flex-1 px-3 py-2 rounded-xl bg-ifa-card-muted border border-ifa-border text-xs text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine"
                />
                <button
                  type="button"
                  on:click={() => aiUpdateModule(mod.id)}
                  disabled={aiBusyModuleId !== null || !(aiInstructions[mod.id] ?? '').trim()}
                  class="px-3 py-2 rounded-xl bg-ifa-pine/10 text-ifa-pine text-xs font-semibold hover:bg-ifa-pine/20 transition flex items-center gap-1.5 disabled:opacity-50"
                >
                  {#if aiBusyModuleId === mod.id}
                    <Loader2 class="w-3.5 h-3.5 animate-spin" />
                  {:else}
                    <Sparkles class="w-3.5 h-3.5" />
                  {/if}
                  <span>Ask AI</span>
                </button>
              </div>

              <button
                type="button"
                on:click={() => saveModule(mod.id)}
                disabled={savingModuleId !== null}
                class="px-4 py-2 rounded-xl bg-ifa-pine text-white text-xs font-bold flex items-center gap-1.5 hover:bg-ifa-pine-light transition disabled:opacity-60"
              >
                {#if savingModuleId === mod.id}
                  <Loader2 class="w-3.5 h-3.5 animate-spin" />
                  <span>Saving…</span>
                {:else}
                  <Save class="w-3.5 h-3.5" />
                  <span>Save module</span>
                {/if}
              </button>
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</section>
