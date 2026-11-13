<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { BookOpen, CheckCircle2, ChevronRight, Loader2 } from 'lucide-svelte';
  import { ApiError, releasesApi, type CourseAppearanceDto } from '$lib/api';
  import { courseAppearanceStyle, normalizeCourseAppearance } from '$lib/courseAppearance';
  import { renderMarkdown } from '$lib/utils/markdown';
  import CourseChatbot from '$lib/components/CourseChatbot.svelte';
  import ErrorState from '$lib/components/ui/ErrorState.svelte';
  import LoadingSkeleton from '$lib/components/ui/LoadingSkeleton.svelte';

  type Lesson = { id: string; sourceLessonId: string; lessonNumber: number; title: string; summary: string; contentMarkdown: string; readingTimeMinutes: number };
  type Question = { id: string; prompt: string; options: string[] };
  type Quiz = { id: string; title: string; questions: Question[]; passingScorePercentage: number };
  type Module = { id: string; sourceModuleId: string; moduleNumber: number; title: string; summary: string; lessons: Lesson[]; quizzes: Quiz[] };
  type ReleaseView = {
    id: string;
    courseId: string;
    version: number;
    title: string;
    progressPercentage: number;
    courseAssistantEnabled: boolean;
    awardsCertificate: boolean;
    appearance?: CourseAppearanceDto | null;
    modules: Module[];
    progress: Array<{ itemType: string; itemId: string; completedAt?: string }>;
  };

  let release: ReleaseView | null = null;
  let selectedLesson: Lesson | null = null;
  let selectedModule: Module | null = null;
  let loading = true;
  let error = '';
  let completing = false;
  let quizAnswers: Record<string, number[]> = {};
  let quizNotice: Record<string, string> = {};

  $: courseId = $page.params.courseId;
  $: customAppearance = release?.appearance ?? null;
  $: resolvedAppearance = normalizeCourseAppearance(customAppearance);
  $: resolvedAppearanceStyle = customAppearance ? courseAppearanceStyle(resolvedAppearance) : '';

  onMount(load);

  async function load() {
    loading = true;
    error = '';
    try {
      if (!courseId) throw new Error('Course ID is missing.');
      const loadedRelease: ReleaseView = await releasesApi.mine(courseId);
      release = loadedRelease;
      const requestedLesson = $page.url.searchParams.get('lesson');
      selectLesson(loadedRelease.modules.flatMap((module) => module.lessons).find((lesson) => lesson.id === requestedLesson)
        ?? loadedRelease.modules[0]?.lessons[0]
        ?? null);
    } catch (cause) {
      if (cause instanceof ApiError && cause.status === 404) {
        await goto(`/courses/${courseId ?? ''}`, { replaceState: true });
        return;
      }
      error = cause instanceof Error ? cause.message : 'This course release could not be loaded.';
    } finally {
      loading = false;
    }
  }

  function selectLesson(lesson: Lesson | null) {
    selectedLesson = lesson;
    selectedModule = lesson && release
      ? release.modules.find((module) => module.lessons.some((candidate) => candidate.id === lesson.id)) ?? null
      : null;
    if (lesson) history.replaceState({}, '', `${$page.url.pathname}?lesson=${lesson.id}`);
  }

  function completed(itemId: string): boolean {
    return release?.progress.some((item) => item.itemId === itemId && !!item.completedAt) ?? false;
  }

  async function completeLesson() {
    if (!release || !selectedLesson || completing) return;
    completing = true;
    try {
      const result = await releasesApi.completeLesson(release.id, selectedLesson.id);
      release.progressPercentage = result.progressPercentage;
      release.progress = [...release.progress, { itemType: 'Lesson', itemId: selectedLesson.id, completedAt: new Date().toISOString() }];
      release = { ...release };
    } finally {
      completing = false;
    }
  }

  function choose(quizId: string, questionIndex: number, optionIndex: number) {
    const answers = [...(quizAnswers[quizId] ?? [])];
    answers[questionIndex] = optionIndex;
    quizAnswers = { ...quizAnswers, [quizId]: answers };
  }

  async function submitQuiz(quiz: Quiz) {
    if (!release) return;
    const answers = quizAnswers[quiz.id] ?? [];
    if (answers.length !== quiz.questions.length || answers.some((answer) => answer === undefined)) {
      quizNotice = { ...quizNotice, [quiz.id]: 'Answer every question before submitting.' };
      return;
    }
    try {
      const result = await releasesApi.submitQuiz(release.id, quiz.id, answers);
      quizNotice = { ...quizNotice, [quiz.id]: `${result.scorePercentage}% · ${result.isPassed ? 'Passed' : `${result.attemptsRemaining} attempts remaining`}` };
      await load();
    } catch (cause) {
      quizNotice = { ...quizNotice, [quiz.id]: cause instanceof Error ? cause.message : 'The quiz could not be submitted.' };
    }
  }
</script>

<svelte:head><title>{release?.title ?? 'Learning'} · Witforge</title></svelte:head>

{#if loading}
  <div class="mx-auto max-w-6xl p-6"><LoadingSkeleton rows={6} label="Loading course release" /></div>
{:else if error || !release}
  <div class="mx-auto max-w-4xl p-6"><ErrorState message={error || 'Course release not found.'} /></div>
{:else}
  <div class="release-grid min-h-[calc(100vh-5rem)]" class:with-assistant={release.courseAssistantEnabled} data-custom={customAppearance ? 'true' : 'false'} data-canvas={resolvedAppearance.canvasLayout.toLowerCase()} style={resolvedAppearanceStyle}>
    <aside class="border-b border-ifa-border bg-ifa-card p-4 lg:border-b-0 lg:border-r">
      <p class="text-xs font-bold uppercase tracking-wide text-ifa-pine">Release {release.version}</p>
      <h1 class="mt-2 text-lg font-bold text-ifa-text-primary">{release.title}</h1>
      <div class="mt-4 h-2 overflow-hidden rounded-full bg-ifa-card-muted"><div class="h-full bg-ifa-pine" style={`width:${release.progressPercentage}%`}></div></div>
      <p class="mt-1 text-xs text-ifa-text-muted">{release.progressPercentage}% complete</p>
      <nav aria-label="Course curriculum" class="mt-6 space-y-5">
        {#each release.modules as module}
          <section>
            <h2 class="text-xs font-bold uppercase tracking-wide text-ifa-text-secondary">{module.moduleNumber}. {module.title}</h2>
            <div class="mt-2 space-y-1">
              {#each module.lessons as lesson}
                <button on:click={() => selectLesson(lesson)} aria-current={selectedLesson?.id === lesson.id ? 'page' : undefined} class={`flex w-full items-center gap-2 rounded-lg px-2 py-2 text-left text-xs ${selectedLesson?.id === lesson.id ? 'bg-ifa-pine/10 font-bold text-ifa-pine' : 'text-ifa-text-secondary hover:bg-ifa-card-muted'}`}>
                  {#if completed(lesson.id)}<CheckCircle2 class="h-3.5 w-3.5 shrink-0 text-emerald-600" />{:else}<BookOpen class="h-3.5 w-3.5 shrink-0" />{/if}
                  <span class="line-clamp-2">{lesson.title}</span>
                </button>
              {/each}
            </div>
          </section>
        {/each}
      </nav>
    </aside>

    <main class="min-w-0 p-5 sm:p-8">
      {#if selectedLesson}
        <p class="text-xs font-bold uppercase tracking-wide text-ifa-pine">{selectedModule?.title}</p>
        <h2 class="mt-2 text-3xl font-bold text-ifa-text-primary">{selectedLesson.title}</h2>
        <p class="mt-2 text-sm text-ifa-text-muted">{selectedLesson.readingTimeMinutes} min read</p>
        <article class="prose mt-8 max-w-none dark:prose-invert">{@html renderMarkdown(selectedLesson.contentMarkdown)}</article>
        <button on:click={completeLesson} disabled={completing || completed(selectedLesson.id)} class="mt-8 inline-flex items-center gap-2 rounded-xl bg-ifa-pine px-5 py-3 text-sm font-bold text-white disabled:opacity-60">
          {#if completing}<Loader2 class="h-4 w-4 animate-spin" />{:else}<CheckCircle2 class="h-4 w-4" />{/if}
          {completed(selectedLesson.id) ? 'Lesson complete' : 'Mark lesson complete'}
        </button>

        {#if selectedModule?.quizzes.length}
          <section class="mt-12 border-t border-ifa-border pt-8">
            <h2 class="text-xl font-bold">Module assessments</h2>
            {#each selectedModule.quizzes as quiz}
              <form class="mt-5 rounded-2xl border border-ifa-border bg-ifa-card p-5" on:submit|preventDefault={() => submitQuiz(quiz)}>
                <h3 class="font-bold">{quiz.title}</h3>
                {#each quiz.questions as question, questionIndex}
                  <fieldset class="mt-5">
                    <legend class="text-sm font-semibold">{questionIndex + 1}. {question.prompt}</legend>
                    <div class="mt-2 space-y-2">
                      {#each question.options as option, optionIndex}
                        <label class="flex items-start gap-2 rounded-lg border border-ifa-border p-3 text-sm"><input type="radio" name={`${quiz.id}-${question.id}`} checked={quizAnswers[quiz.id]?.[questionIndex] === optionIndex} on:change={() => choose(quiz.id, questionIndex, optionIndex)} />{option}</label>
                      {/each}
                    </div>
                  </fieldset>
                {/each}
                {#if quizNotice[quiz.id]}<p role="status" class="mt-4 text-sm text-ifa-text-secondary">{quizNotice[quiz.id]}</p>{/if}
                <button class="mt-4 inline-flex items-center gap-1 rounded-xl border border-ifa-pine px-4 py-2 text-sm font-bold text-ifa-pine">Submit quiz <ChevronRight class="h-4 w-4" /></button>
              </form>
            {/each}
          </section>
        {/if}
      {/if}
    </main>

    {#if release.courseAssistantEnabled}
    <aside class="border-t border-ifa-border p-4 lg:border-l lg:border-t-0">
      {#if selectedModule}
        <CourseChatbot
          courseId={release.courseId}
          courseTitle={release.title}
          moduleId={selectedModule.sourceModuleId}
          lessonId={selectedLesson?.sourceLessonId ?? null}
          lessonTitle={selectedLesson?.title ?? null}
        />
      {/if}
    </aside>
    {/if}
  </div>
{/if}

<style>
  .release-grid{display:grid}.release-grid[data-custom='true']{color:var(--course-text);background:var(--course-background);font-family:var(--course-body-font)}.release-grid[data-custom='true'] :global(h1),.release-grid[data-custom='true'] :global(h2),.release-grid[data-custom='true'] :global(h3){font-family:var(--course-heading-font)}.release-grid[data-custom='true'] :global(article),.release-grid[data-custom='true'] :global(aside){border-radius:var(--course-radius)}.release-grid[data-custom='true'][data-canvas='editorial']>main{max-width:var(--course-content-width);margin-inline:auto;font-size:1.05rem;line-height:1.85}.release-grid[data-custom='true'][data-canvas='presentation']>main{max-width:1200px;margin-inline:auto}.release-grid[data-custom='true'][data-canvas='presentation']>main :global(h1){font-size:clamp(2.5rem,6vw,5.5rem)}
  @media(min-width:1024px){.release-grid{grid-template-columns:19rem minmax(0,1fr)}.release-grid.with-assistant{grid-template-columns:19rem minmax(0,1fr) 22rem}.release-grid[data-canvas='classic']{grid-template-columns:16rem minmax(0,1fr)}.release-grid[data-canvas='classic'].with-assistant{grid-template-columns:16rem minmax(0,1fr) 22rem}.release-grid[data-canvas='editorial']{grid-template-columns:14rem minmax(0,1fr)}.release-grid[data-canvas='editorial'].with-assistant{grid-template-columns:14rem minmax(0,1fr) 20rem}.release-grid[data-canvas='presentation']{grid-template-columns:11rem minmax(0,1fr)}.release-grid[data-canvas='presentation'].with-assistant{grid-template-columns:11rem minmax(0,1fr) 19rem}}
</style>
