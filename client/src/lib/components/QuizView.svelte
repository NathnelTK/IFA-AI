<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { ArrowLeft, CheckCircle2, XCircle, RotateCcw, Award } from 'lucide-svelte';
  import type { Quiz } from '$lib/types';

  export let quiz: Quiz;
  export let moduleTitle: string;

  const dispatch = createEventDispatcher<{
    back: void;
    scored: { quizId: string; score: number };
  }>();

  // answers[questionIndex] = selected option index (or -1 if unanswered)
  let answers: number[] = quiz.questions.map(() => -1);
  let submitted = false;

  $: allAnswered = answers.every((a) => a !== -1);
  $: correctCount = quiz.questions.reduce(
    (n, q, i) => (answers[i] === q.correctIndex ? n + 1 : n),
    0
  );
  $: score = Math.round((correctCount / quiz.questions.length) * 100);
  $: passed = score >= 70;

  function select(qIndex: number, optIndex: number) {
    if (submitted) return;
    answers[qIndex] = optIndex;
    answers = answers; // trigger reactivity
  }

  function submit() {
    if (!allAnswered) return;
    submitted = true;
    dispatch('scored', { quizId: quiz.id, score });
  }

  function retry() {
    answers = quiz.questions.map(() => -1);
    submitted = false;
  }
</script>

<div class="max-w-3xl mx-auto space-y-6">
  <button
    type="button"
    on:click={() => dispatch('back')}
    class="flex items-center gap-1.5 text-sm font-semibold text-ifa-text-muted hover:text-ifa-pine transition"
  >
    <ArrowLeft class="w-4 h-4" />
    <span>Back to course</span>
  </button>

  <div class="bg-ifa-card rounded-3xl border border-ifa-border p-6 md:p-8 shadow-card space-y-6">
    <!-- Header -->
    <div class="flex items-start justify-between gap-4">
      <div>
        <p class="text-[11px] font-bold uppercase tracking-wider text-ifa-text-muted mb-1">{moduleTitle}</p>
        <h1 class="text-2xl font-bold text-ifa-text-primary">{quiz.title}</h1>
        <p class="text-xs text-ifa-text-secondary mt-1">{quiz.questions.length} questions • pass at 70%</p>
      </div>
      {#if quiz.bestScore !== null}
        <div class="text-right shrink-0">
          <p class="text-[10px] uppercase font-bold text-ifa-text-muted tracking-wider">Best</p>
          <p class="text-lg font-black text-ifa-pine">{quiz.bestScore}%</p>
        </div>
      {/if}
    </div>

    <!-- Result banner -->
    {#if submitted}
      <div
        class="rounded-2xl p-5 flex items-center gap-4 border {passed
          ? 'bg-emerald-50 border-emerald-200'
          : 'bg-amber-50 border-amber-200'}"
      >
        <div
          class="w-12 h-12 rounded-full flex items-center justify-center shrink-0 {passed
            ? 'bg-emerald-600 text-white'
            : 'bg-amber-500 text-white'}"
        >
          <Award class="w-6 h-6" />
        </div>
        <div>
          <h3 class="text-lg font-bold text-ifa-text-primary">
            {passed ? 'Nice work!' : 'Keep practicing'} You scored {score}%
          </h3>
          <p class="text-xs text-ifa-text-secondary">
            {correctCount} of {quiz.questions.length} correct.
            {passed ? ' You passed this module quiz.' : ' Review the explanations and try again.'}
          </p>
        </div>
      </div>
    {/if}

    <!-- Questions -->
    <div class="space-y-5">
      {#each quiz.questions as q, qi}
        <div class="rounded-2xl border border-ifa-border p-4 space-y-3">
          <h4 class="text-sm font-bold text-ifa-text-primary">
            <span class="text-ifa-text-muted">{qi + 1}.</span> {q.prompt}
          </h4>
          <div class="space-y-2">
            {#each q.options as option, oi}
              {@const isSelected = answers[qi] === oi}
              {@const isCorrect = oi === q.correctIndex}
              <button
                type="button"
                on:click={() => select(qi, oi)}
                disabled={submitted}
                class="w-full text-left px-3.5 py-2.5 rounded-xl border text-sm transition flex items-center justify-between gap-2
                  {submitted && isCorrect
                    ? 'border-emerald-400 bg-emerald-50 text-emerald-800'
                    : submitted && isSelected && !isCorrect
                      ? 'border-red-300 bg-red-50 text-red-700'
                      : isSelected
                        ? 'border-ifa-pine bg-ifa-pine/5 text-ifa-text-primary'
                        : 'border-ifa-border bg-ifa-card-muted/40 text-ifa-text-secondary hover:border-ifa-pine/40'}"
              >
                <span>{option}</span>
                {#if submitted && isCorrect}
                  <CheckCircle2 class="w-4 h-4 text-emerald-600 shrink-0" />
                {:else if submitted && isSelected && !isCorrect}
                  <XCircle class="w-4 h-4 text-red-500 shrink-0" />
                {/if}
              </button>
            {/each}
          </div>
          {#if submitted}
            <p class="text-xs text-ifa-text-secondary bg-ifa-card-muted rounded-lg p-3">
              <span class="font-semibold text-ifa-text-primary">Why:</span> {q.explanation}
            </p>
          {/if}
        </div>
      {/each}
    </div>

    <!-- Actions -->
    <div class="flex items-center gap-3 pt-2">
      {#if !submitted}
        <button
          type="button"
          on:click={submit}
          disabled={!allAnswered}
          class="px-5 py-2.5 rounded-full bg-ifa-pine text-white text-xs font-bold transition flex items-center gap-2 shadow-sm disabled:opacity-40 disabled:cursor-not-allowed hover:bg-ifa-pine-light"
        >
          <CheckCircle2 class="w-3.5 h-3.5" />
          <span>Submit answers{allAnswered ? '' : ` (${answers.filter((a) => a !== -1).length}/${quiz.questions.length})`}</span>
        </button>
      {:else}
        <button
          type="button"
          on:click={retry}
          class="px-5 py-2.5 rounded-full bg-ifa-card-muted hover:bg-ifa-bg text-ifa-text-secondary hover:text-ifa-pine border border-ifa-border text-xs font-semibold transition flex items-center gap-2"
        >
          <RotateCcw class="w-3.5 h-3.5" />
          <span>Try again</span>
        </button>
        <button
          type="button"
          on:click={() => dispatch('back')}
          class="px-5 py-2.5 rounded-full bg-ifa-pine text-white text-xs font-bold transition hover:bg-ifa-pine-light"
        >
          Back to course
        </button>
      {/if}
    </div>
  </div>
</div>
