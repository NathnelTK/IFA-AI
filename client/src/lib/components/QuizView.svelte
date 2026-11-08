<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { ArrowLeft, CheckCircle2, XCircle, RotateCcw, Award, Loader2 } from 'lucide-svelte';
  import type { Quiz } from '$lib/types';
  import { apiErrorMessage, quizzesApi, type QuizSubmissionResultDto } from '$lib/api';

  export let quiz: Quiz;
  export let moduleTitle: string;
  export let readOnly = false;

  const dispatch = createEventDispatcher<{
    back: void;
    scored: { quizId: string; score: number; result: QuizSubmissionResultDto };
  }>();

  // answers[questionIndex] = selected option index (or -1 if unanswered)
  let answers: number[] = quiz.questions.map(() => -1);
  let submitted = false;
  let submitting = false;
  let error = '';
  let result: QuizSubmissionResultDto | null = null;
  let localAttemptCount = quiz.attemptCount ?? 0;

  $: allAnswered = quiz.questions.length > 0 && answers.every((a) => a !== -1);
  $: correctCount = result?.correctAnswersCount ?? 0;
  $: score = result?.scorePercentage ?? 0;
  $: passed = result?.isPassed ?? false;
  $: attempts = Math.max(quiz.attemptCount ?? 0, localAttemptCount);
  $: exhausted = typeof quiz.maxAttempts === 'number' && attempts >= quiz.maxAttempts;

  function select(qIndex: number, optIndex: number) {
    if (submitted || submitting || readOnly || exhausted) return;
    answers[qIndex] = optIndex;
    answers = answers; // trigger reactivity
  }

  async function submit() {
    if (!allAnswered || submitting || submitted || readOnly || exhausted) return;
    submitting = true;
    error = '';
    try {
      result = await quizzesApi.submit(quiz.id, Object.fromEntries(quiz.questions.map((question, index) => [question.id, answers[index]])));
      localAttemptCount = result.attemptCount;
      submitted = true;
      dispatch('scored', { quizId: quiz.id, score: result.scorePercentage, result });
    } catch (cause) {
      error = apiErrorMessage(cause, 'Could not save your attempt. Your selected answers have been kept.');
    } finally {
      submitting = false;
    }
  }

  function retry() {
    if (exhausted || readOnly) return;
    answers = quiz.questions.map(() => -1);
    submitted = false;
    result = null;
    error = '';
  }
</script>

<div class="max-w-3xl mx-auto space-y-6">
  <button
    type="button"
    on:click={() => dispatch('back')}
    disabled={submitting}
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
        <p class="text-xs text-ifa-text-secondary mt-1">{quiz.questions.length} questions{quiz.passingScorePercentage !== undefined ? ` · pass at ${quiz.passingScorePercentage}%` : ''}</p>
        {#if !readOnly}<p class="text-xs text-ifa-text-muted mt-1">Saved attempts: {attempts}{quiz.maxAttempts ? ` / ${quiz.maxAttempts}` : ''}</p>{/if}
      </div>
      {#if !readOnly && quiz.latestAttempt}
        <div class="text-right shrink-0">
          <p class="text-[10px] uppercase font-bold text-ifa-text-muted tracking-wider">Last Saved Attempt</p>
          <p class="text-lg font-black text-ifa-pine">{quiz.latestAttempt.scorePercentage}%</p>
          <p class="text-[11px] text-ifa-text-secondary">{quiz.latestAttempt.isPassed ? 'Passed' : 'Not passed'}</p>
        </div>
      {/if}
    </div>

    {#if readOnly}
      <p class="rounded-xl border border-ifa-border bg-ifa-card-muted p-3 text-xs text-ifa-text-secondary">Saved assessment preview. Submissions and learner progress are disabled.</p>
    {:else if exhausted}
      <p role="status" class="rounded-xl border border-ifa-border bg-ifa-card-muted p-3 text-xs text-ifa-text-secondary">You have used all {quiz.maxAttempts} allowed attempts. Your results are saved.</p>
    {/if}
    {#if error}<p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700 whitespace-pre-line">{error}</p>{/if}

    <!-- Result banner -->
    {#if submitted}
      <div
        role="status"
        class="rounded-2xl p-5 flex items-center gap-4 border bg-ifa-card-muted {passed ? 'border-emerald-500/50' : 'border-amber-500/50'}"
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
            {passed ? ' You passed. This result is saved.' : exhausted ? ' This result is saved. No attempts remain.' : ' This result is saved. Review the explanations before trying again.'}
          </p>
        </div>
      </div>
    {/if}

    <!-- Questions -->
    <div class="space-y-5">
      {#each quiz.questions as q, qi}
        {@const feedback = result?.questionFeedbacks.find((item) => item.questionId === q.id)}
        <div class="rounded-2xl border border-ifa-border p-4 space-y-3">
          <h4 class="text-sm font-bold text-ifa-text-primary">
            <span class="text-ifa-text-muted">{qi + 1}.</span> {q.prompt}
          </h4>
          <div class="space-y-2">
            {#each q.options as option, oi}
              {@const isSelected = answers[qi] === oi}
              {@const isCorrect = feedback?.correctOptionIndex === oi}
              <button
                type="button"
                on:click={() => select(qi, oi)}
                disabled={submitted || submitting || readOnly || exhausted}
                aria-pressed={isSelected}
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
          {#if submitted && feedback?.explanation}
            <p class="text-xs text-ifa-text-secondary bg-ifa-card-muted rounded-lg p-3">
              <span class="font-semibold text-ifa-text-primary">Why:</span> {feedback.explanation}
            </p>
          {/if}
        </div>
      {/each}
      {#if quiz.questions.length === 0}<p class="text-sm text-ifa-text-secondary">No saved questions are available for this assessment.</p>{/if}
    </div>

    <!-- Actions -->
    <div class="flex items-center gap-3 pt-2">
      {#if !submitted && !readOnly}
        <button
          type="button"
          on:click={submit}
          disabled={!allAnswered || submitting || exhausted}
          class="px-5 py-2.5 rounded-full bg-ifa-pine text-white text-xs font-bold transition flex items-center gap-2 shadow-sm disabled:opacity-40 disabled:cursor-not-allowed hover:bg-ifa-pine-light"
        >
          {#if submitting}<Loader2 class="w-3.5 h-3.5 animate-spin" />{:else}<CheckCircle2 class="w-3.5 h-3.5" />{/if}
          <span>{submitting ? 'Saving attempt...' : `Submit answers${allAnswered ? '' : ` (${answers.filter((a) => a !== -1).length}/${quiz.questions.length})`}`}</span>
        </button>
      {:else}
        {#if !readOnly && !exhausted}<button
          type="button"
          on:click={retry}
          class="px-5 py-2.5 rounded-full bg-ifa-card-muted hover:bg-ifa-bg text-ifa-text-secondary hover:text-ifa-pine border border-ifa-border text-xs font-semibold transition flex items-center gap-2"
        >
          <RotateCcw class="w-3.5 h-3.5" />
          <span>Try again</span>
        </button>{/if}
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
