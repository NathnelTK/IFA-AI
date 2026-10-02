<script lang="ts">
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import {
    ArrowLeft,
    BookOpen,
    CheckCircle2,
    Circle,
    ClipboardCheck,
    Clock,
    PlayCircle,
    Share2,
    Bookmark,
    BookmarkCheck
  } from 'lucide-svelte';
  import LessonView from '$lib/components/LessonView.svelte';
  import QuizView from '$lib/components/QuizView.svelte';
  import CourseShareDialog from '$lib/components/CourseShareDialog.svelte';
  import {
    courses,
    courseProgress,
    markLessonComplete,
    recordQuizScore,
    toggleBookmark
  } from '$lib/stores/coursesStore';
  import type { Lesson, Module, Quiz } from '$lib/types';

  $: courseId = $page.params.id;
  // Reactively resolve the course from the store so progress updates live.
  $: course = $courses.find((c) => c.id === courseId) ?? null;
  $: progress = course ? courseProgress(course) : 0;

  // View state: course outline, a specific lesson, or a module quiz.
  type View =
    | { kind: 'outline' }
    | { kind: 'lesson'; moduleId: string; lessonId: string }
    | { kind: 'quiz'; moduleId: string };
  let view: View = { kind: 'outline' };

  let shareOpen = false;

  $: activeModule =
    view.kind !== 'outline' ? course?.modules.find((m) => m.id === view.moduleId) ?? null : null;
  $: activeLesson =
    view.kind === 'lesson' && activeModule
      ? activeModule.lessons.find((l) => l.id === view.lessonId) ?? null
      : null;

  // Flatten lessons for "next lesson" navigation.
  $: flatLessons = course
    ? course.modules.flatMap((m) => m.lessons.map((l) => ({ moduleId: m.id, lesson: l })))
    : [];

  function openLesson(moduleId: string, lessonId: string) {
    view = { kind: 'lesson', moduleId, lessonId };
    scrollTop();
  }
  function openQuiz(moduleId: string) {
    view = { kind: 'quiz', moduleId };
    scrollTop();
  }
  function backToOutline() {
    view = { kind: 'outline' };
    scrollTop();
  }
  function scrollTop() {
    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function handleComplete(lessonId: string) {
    if (!course) return;
    const lesson = flatLessons.find((f) => f.lesson.id === lessonId)?.lesson;
    markLessonComplete(course.id, lessonId, !lesson?.completed);
  }

  function goNext(currentLessonId: string) {
    const idx = flatLessons.findIndex((f) => f.lesson.id === currentLessonId);
    const next = flatLessons[idx + 1];
    if (next) openLesson(next.moduleId, next.lesson.id);
    else backToOutline();
  }

  function handleScored(quizId: string, score: number) {
    if (course) recordQuizScore(course.id, quizId, score);
  }

  function moduleProgress(module: Module): number {
    if (module.lessons.length === 0) return 0;
    return Math.round(
      (module.lessons.filter((l) => l.completed).length / module.lessons.length) * 100
    );
  }
</script>

{#if !course}
  <!-- Unknown course -->
  <div class="p-8 max-w-3xl mx-auto text-center py-20">
    <BookOpen class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
    <h1 class="text-xl font-bold text-ifa-text-primary mb-2">Course not found</h1>
    <p class="text-ifa-text-secondary mb-6">We couldn't find a course with that id.</p>
    <a
      href="/courses"
      class="inline-flex items-center gap-2 px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold hover:bg-ifa-pine-light transition"
    >
      <ArrowLeft class="w-4 h-4" />
      <span>Back to courses</span>
    </a>
  </div>
{:else}
  <div class="p-6 md:p-8 max-w-5xl mx-auto">
    {#if view.kind === 'lesson' && activeLesson && activeModule}
      <LessonView
        lesson={activeLesson}
        moduleTitle={activeModule.title}
        hasNext={flatLessons.findIndex((f) => f.lesson.id === activeLesson.id) < flatLessons.length - 1}
        on:back={backToOutline}
        on:complete={(e) => handleComplete(e.detail.lessonId)}
        on:next={() => activeLesson && goNext(activeLesson.id)}
      />
    {:else if view.kind === 'quiz' && activeModule?.quiz}
      <QuizView
        quiz={activeModule.quiz}
        moduleTitle={activeModule.title}
        on:back={backToOutline}
        on:scored={(e) => handleScored(e.detail.quizId, e.detail.score)}
      />
    {:else}
      <!-- Course outline -->
      <div class="space-y-6">
        <a
          href="/courses"
          class="inline-flex items-center gap-1.5 text-sm font-semibold text-ifa-text-muted hover:text-ifa-pine transition"
        >
          <ArrowLeft class="w-4 h-4" />
          <span>All courses</span>
        </a>

        <!-- Course header -->
        <div class="bg-ifa-card rounded-3xl border border-ifa-border p-6 md:p-8 shadow-card">
          <div class="flex flex-col md:flex-row gap-6">
            <div
              class="w-full md:w-40 h-32 rounded-2xl bg-gradient-to-br from-[#1C1F24] to-[#0D0F12] flex items-center justify-center text-white shrink-0"
            >
              <span class="text-3xl font-black tracking-tight">{course.thumbnail}</span>
            </div>
            <div class="flex-1">
              <div class="flex items-start justify-between gap-4">
                <div>
                  <h1 class="text-2xl font-bold text-ifa-text-primary">{course.title}</h1>
                  <p class="text-sm text-ifa-text-secondary mt-1">{course.provider}</p>
                </div>
                <div class="flex items-center gap-2 shrink-0">
                  <button
                    type="button"
                    on:click={() => course && toggleBookmark(course.id)}
                    title="Bookmark"
                    class="w-9 h-9 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-accent-purple transition"
                  >
                    {#if course.isBookmarked}
                      <BookmarkCheck class="w-4 h-4 text-ifa-accent-purple" />
                    {:else}
                      <Bookmark class="w-4 h-4" />
                    {/if}
                  </button>
                  <button
                    type="button"
                    on:click={() => (shareOpen = true)}
                    title="Share course"
                    class="w-9 h-9 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine transition"
                  >
                    <Share2 class="w-4 h-4" />
                  </button>
                </div>
              </div>
              <p class="text-sm text-ifa-text-secondary mt-3 leading-relaxed">{course.description}</p>

              <div class="flex flex-wrap items-center gap-4 mt-4 text-xs text-ifa-text-muted">
                <span class="flex items-center gap-1"><Clock class="w-3.5 h-3.5" />{course.duration}</span>
                <span class="flex items-center gap-1"><BookOpen class="w-3.5 h-3.5" />{course.modules.length} modules</span>
                <span class="flex items-center gap-1">
                  <ClipboardCheck class="w-3.5 h-3.5" />
                  {course.modules.flatMap((m) => m.lessons).length} lessons
                </span>
              </div>

              <!-- Progress bar -->
              <div class="mt-4">
                <div class="flex items-center justify-between text-xs mb-1">
                  <span class="text-ifa-text-secondary font-medium">Progress</span>
                  <span class="font-bold text-ifa-pine">{progress}%</span>
                </div>
                <div class="w-full h-2 rounded-full bg-ifa-bg border border-ifa-border overflow-hidden">
                  <div class="h-full rounded-full bg-emerald-600 transition-all duration-500" style="width: {progress}%"></div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Modules -->
        <div class="space-y-4">
          {#each course.modules as module, mi}
            <div class="bg-ifa-card rounded-2xl border border-ifa-border shadow-card overflow-hidden">
              <div class="px-5 py-4 border-b border-ifa-border flex items-center justify-between gap-4">
                <div class="flex items-center gap-3 min-w-0">
                  <div class="w-8 h-8 rounded-lg bg-ifa-pine/10 text-ifa-pine flex items-center justify-center text-xs font-bold shrink-0">
                    {mi + 1}
                  </div>
                  <div class="min-w-0">
                    <h3 class="text-sm font-bold text-ifa-text-primary truncate">{module.title}</h3>
                    <p class="text-[11px] text-ifa-text-muted truncate">{module.summary}</p>
                  </div>
                </div>
                <span class="text-[11px] font-bold text-ifa-pine shrink-0">{moduleProgress(module)}%</span>
              </div>

              <!-- Lessons -->
              <div class="divide-y divide-ifa-border-light">
                {#each module.lessons as lesson}
                  <button
                    type="button"
                    on:click={() => openLesson(module.id, lesson.id)}
                    class="w-full flex items-center gap-3 px-5 py-3 text-left hover:bg-ifa-card-muted/60 transition group"
                  >
                    {#if lesson.completed}
                      <CheckCircle2 class="w-4 h-4 text-emerald-600 shrink-0" />
                    {:else}
                      <Circle class="w-4 h-4 text-ifa-text-muted shrink-0" />
                    {/if}
                    <div class="flex-1 min-w-0">
                      <p class="text-sm font-semibold text-ifa-text-primary group-hover:text-ifa-pine transition truncate">
                        {lesson.title}
                      </p>
                      <p class="text-[11px] text-ifa-text-muted truncate">{lesson.summary}</p>
                    </div>
                    <span class="text-[11px] text-ifa-text-muted flex items-center gap-1 shrink-0">
                      <Clock class="w-3 h-3" />{lesson.duration}
                    </span>
                    <PlayCircle class="w-4 h-4 text-ifa-text-muted group-hover:text-ifa-pine transition shrink-0" />
                  </button>
                {/each}

                <!-- Quiz row -->
                {#if module.quiz}
                  <button
                    type="button"
                    on:click={() => openQuiz(module.id)}
                    class="w-full flex items-center gap-3 px-5 py-3 text-left hover:bg-ifa-card-muted/60 transition group bg-ifa-card-muted/30"
                  >
                    <ClipboardCheck class="w-4 h-4 text-ifa-accent-purple shrink-0" />
                    <div class="flex-1 min-w-0">
                      <p class="text-sm font-semibold text-ifa-text-primary group-hover:text-ifa-pine transition">
                        {module.quiz.title}
                      </p>
                      <p class="text-[11px] text-ifa-text-muted">
                        {module.quiz.questions.length} questions
                        {#if module.quiz.bestScore !== null}• best {module.quiz.bestScore}%{/if}
                      </p>
                    </div>
                    <span class="text-[11px] font-bold text-ifa-accent-purple shrink-0">Take quiz</span>
                  </button>
                {/if}
              </div>
            </div>
          {/each}
        </div>
      </div>
    {/if}
  </div>

  <CourseShareDialog isOpen={shareOpen} courseTitle={course.title} on:close={() => (shareOpen = false)} />
{/if}
