<script lang="ts">
  import { onMount } from 'svelte';
  import { Sparkles, Loader2, CheckCircle2, Lock, PlayCircle } from 'lucide-svelte';
  import LessonViewer from '$lib/components/LessonViewer.svelte';
  import QuizModal from '$lib/components/QuizModal.svelte';
  import NextModuleTrigger from '$lib/components/NextModuleTrigger.svelte';
  import type { GeneratedModuleResult, QuizResult } from '$lib/types';
  import {
    courseTitle,
    courseBlueprint,
    generatedModules,
    quizResults,
    activeModuleNumber,
    courseProgress,
    buildModule,
    recordQuizResult
  } from '$lib/stores/learningSession';

  let current: GeneratedModuleResult | null = null;
  let loading = true;
  let quizOpen = false;
  let triggerState: 'locked' | 'ready' | 'building' | 'done' = 'locked';

  $: blueprint = $courseBlueprint;
  $: activeNum = $activeModuleNumber;
  $: passed = Boolean($quizResults[activeNum]?.passed);
  $: nextProposal = blueprint.find((m) => m.id === activeNum + 1) ?? null;
  $: triggerState = passed ? (nextProposal ? 'ready' : 'done') : 'locked';

  async function loadModule(n: number) {
    loading = true;
    current = null;
    activeModuleNumber.set(n);
    current = await buildModule(n);
    loading = false;
  }

  function onQuizPassed(result: QuizResult) {
    recordQuizResult(activeNum, result);
  }

  async function generateNext() {
    if (!nextProposal) return;
    await loadModule(nextProposal.id);
  }

  function selectModule(n: number) {
    // Only allow navigating to module 1, already-built modules, or the next
    // module once the current one is passed.
    const isUnlocked = n === 1 || Boolean($generatedModules[n]) || Boolean($quizResults[n - 1]?.passed);
    if (isUnlocked) loadModule(n);
  }

  onMount(() => loadModule($activeModuleNumber));
</script>

<svelte:head>
  <title>Interactive Lesson · IFA</title>
</svelte:head>

<div class="p-6 md:p-8 max-w-[1400px] mx-auto">
  <!-- Breadcrumb + progress -->
  <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-6">
    <div>
      <a href="/" class="text-[11px] font-semibold text-ifa-text-muted hover:text-ifa-pine transition">← Back to dashboard</a>
      <h1 class="text-lg font-extrabold tracking-tight text-ifa-text-primary mt-1">{$courseTitle}</h1>
    </div>
    <div class="flex items-center gap-3">
      <div class="w-40 h-2 rounded-full bg-ifa-card border border-ifa-border overflow-hidden">
        <div class="h-full rounded-full bg-emerald-600 transition-all duration-500" style="width: {$courseProgress}%"></div>
      </div>
      <span class="text-xs font-bold text-ifa-pine">{$courseProgress}%</span>
    </div>
  </div>

  <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
    <!-- Module rail -->
    <aside class="lg:col-span-3 space-y-2">
      <p class="text-[11px] font-mono uppercase tracking-wider text-ifa-text-muted px-1 mb-1">Course Modules</p>
      {#each blueprint as mod (mod.id)}
        {@const isActive = mod.id === activeNum}
        {@const isBuilt = Boolean($generatedModules[mod.id])}
        {@const isPassed = Boolean($quizResults[mod.id]?.passed)}
        {@const isUnlocked = mod.id === 1 || isBuilt || Boolean($quizResults[mod.id - 1]?.passed)}
        <button
          type="button"
          on:click={() => selectModule(mod.id)}
          disabled={!isUnlocked}
          class="w-full text-left p-3 rounded-2xl border transition flex items-start gap-3
            {isActive
              ? 'border-ifa-pine bg-ifa-pine/[0.06] shadow-soft'
              : isUnlocked
                ? 'border-ifa-border bg-ifa-card hover:border-ifa-pine/40'
                : 'border-ifa-border-light bg-ifa-card-muted/50 opacity-60 cursor-not-allowed'}"
        >
          <div
            class="w-7 h-7 rounded-lg flex items-center justify-center shrink-0 text-[11px] font-bold
              {isPassed ? 'bg-emerald-100 text-emerald-600' : isActive ? 'bg-ifa-pine text-white' : 'bg-ifa-card-muted text-ifa-text-muted'}"
          >
            {#if isPassed}
              <CheckCircle2 class="w-4 h-4" />
            {:else if !isUnlocked}
              <Lock class="w-3.5 h-3.5" />
            {:else}
              {mod.id}
            {/if}
          </div>
          <div class="min-w-0">
            <p class="text-xs font-bold text-ifa-text-primary leading-snug line-clamp-2">{mod.title}</p>
            <p class="text-[10px] text-ifa-text-muted mt-0.5">{mod.estimatedHours}h · {mod.topics.length} topics</p>
          </div>
        </button>
      {/each}
    </aside>

    <!-- Lesson column -->
    <main class="lg:col-span-9 space-y-5">
      {#if loading || !current}
        <div class="bg-ifa-card rounded-3xl border border-ifa-border shadow-card p-16 flex flex-col items-center justify-center text-center gap-4">
          <div class="w-12 h-12 rounded-full bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
            <Loader2 class="w-6 h-6 animate-spin" />
          </div>
          <div>
            <p class="text-sm font-bold text-ifa-pine">Generating Module {activeNum} via Course Builder…</p>
            <p class="text-[11px] text-ifa-text-secondary mt-1">Synthesising Scholarxiv research & curating videos in &lt;3s</p>
          </div>
        </div>
      {:else}
        <LessonViewer
          lesson={current.lesson}
          moduleTitle={current.module.title}
          moduleNumber={current.module.moduleNumber}
        />

        <!-- Quiz gate -->
        <div class="bg-ifa-card rounded-3xl border border-ifa-border shadow-card p-5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <div class="flex items-start gap-3">
            <div class="w-10 h-10 rounded-2xl bg-ifa-accent-amber/10 flex items-center justify-center text-ifa-accent-amber shrink-0">
              <Sparkles class="w-5 h-5" />
            </div>
            <div>
              <h4 class="text-sm font-bold text-ifa-text-primary">
                {passed ? 'Diagnostic passed' : `${current.quiz.title}`}
              </h4>
              <p class="text-[11px] text-ifa-text-secondary mt-0.5">
                {#if passed}
                  You scored {$quizResults[activeNum].scorePercent}% — great work.
                {:else}
                  {current.quiz.questions.length} questions · pass ≥ {current.quiz.passingScorePercentage}% to unlock the next module.
                {/if}
              </p>
            </div>
          </div>
          <button
            type="button"
            on:click={() => (quizOpen = true)}
            class="w-full sm:w-auto px-5 py-2.5 rounded-xl text-white text-xs font-bold transition flex items-center justify-center gap-2 shrink-0
              {passed ? 'bg-ifa-card-muted !text-ifa-pine border border-ifa-border hover:bg-ifa-bg' : 'bg-ifa-pine hover:bg-ifa-pine-light'}"
          >
            <PlayCircle class="w-4 h-4" />
            <span>{passed ? 'Retake Quiz' : 'Take Diagnostic Quiz'}</span>
          </button>
        </div>

        <!-- JIT next-module trigger -->
        <NextModuleTrigger
          state={triggerState}
          nextModuleTitle={nextProposal?.title ?? ''}
          onGenerate={generateNext}
        />
      {/if}
    </main>
  </div>
</div>

{#if current}
  <QuizModal quiz={current.quiz} bind:open={quizOpen} onClose={() => (quizOpen = false)} onPassed={onQuizPassed} />
{/if}
