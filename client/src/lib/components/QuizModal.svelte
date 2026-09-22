<script lang="ts">
  import { X, Check, AlertCircle, ArrowRight, Trophy, RotateCcw } from 'lucide-svelte';
  import type { GeneratedQuiz, QuizResult } from '../types';

  export let quiz: GeneratedQuiz;
  export let open = false;
  export let onClose = () => {};
  export let onPassed = (_result: QuizResult) => {};

  let currentIndex = 0;
  let selectedOption: number | null = null;
  let revealed = false;
  let answers: number[] = [];
  let finished = false;

  $: question = quiz.questions[currentIndex];
  $: totalQuestions = quiz.questions.length;
  $: correctCount = answers.filter((a, i) => a === quiz.questions[i]?.correctOptionIndex).length;
  $: scorePercent = totalQuestions ? Math.round((correctCount / totalQuestions) * 100) : 0;
  $: passed = scorePercent >= quiz.passingScorePercentage;

  function reset() {
    currentIndex = 0;
    selectedOption = null;
    revealed = false;
    answers = [];
    finished = false;
  }

  function selectOption(index: number) {
    if (revealed) return;
    selectedOption = index;
  }

  function submitAnswer() {
    if (selectedOption === null) return;
    answers = [...answers, selectedOption];
    revealed = true;
  }

  function nextQuestion() {
    if (currentIndex < totalQuestions - 1) {
      currentIndex += 1;
      selectedOption = null;
      revealed = false;
    } else {
      finished = true;
      if (passed) {
        onPassed({ scorePercent, correctCount, totalQuestions, passed });
      }
    }
  }

  function handleClose() {
    onClose();
  }

  function retry() {
    reset();
  }

  // Restart the quiz each time it is (re)opened.
  $: if (open) {
    // guard so it only resets on the opening transition
  }
  let wasOpen = false;
  $: {
    if (open && !wasOpen) reset();
    wasOpen = open;
  }
</script>

{#if open}
  <!-- Backdrop -->
  <div
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-ifa-pine/30 backdrop-blur-sm animate-in fade-in duration-200"
    role="dialog"
    aria-modal="true"
    aria-label="Module diagnostic quiz"
  >
    <div class="w-full max-w-lg bg-ifa-card rounded-3xl border border-ifa-border shadow-elevated overflow-hidden animate-in zoom-in-95 duration-200">
      <!-- Header -->
      <div class="flex items-center justify-between px-6 py-4 border-b border-ifa-border-light bg-ifa-card-muted/50">
        <div>
          <p class="text-[11px] font-mono uppercase tracking-wider text-ifa-text-muted">Diagnostic Quiz</p>
          <h3 class="text-sm font-bold text-ifa-text-primary">{quiz.title}</h3>
        </div>
        <button
          type="button"
          on:click={handleClose}
          class="w-8 h-8 rounded-full flex items-center justify-center text-ifa-text-muted hover:bg-ifa-bg hover:text-ifa-text-primary transition"
          aria-label="Close quiz"
        >
          <X class="w-4 h-4" />
        </button>
      </div>

      {#if !finished}
        <!-- Progress -->
        <div class="px-6 pt-4">
          <div class="flex items-center justify-between mb-2">
            <span class="text-[11px] font-semibold text-ifa-text-secondary">
              Question {currentIndex + 1} of {totalQuestions}
            </span>
            <span class="text-[11px] font-medium text-ifa-text-muted">Pass ≥ {quiz.passingScorePercentage}%</span>
          </div>
          <div class="w-full h-1.5 rounded-full bg-ifa-bg border border-ifa-border overflow-hidden">
            <div
              class="h-full rounded-full bg-ifa-pine transition-all duration-300"
              style="width: {((currentIndex + (revealed ? 1 : 0)) / totalQuestions) * 100}%"
            ></div>
          </div>
        </div>

        <!-- Question -->
        <div class="px-6 py-5 space-y-4">
          <p class="text-sm font-semibold text-ifa-text-primary leading-relaxed">{question.prompt}</p>

          <div class="space-y-2">
            {#each question.options as option, i}
              {@const isCorrect = i === question.correctOptionIndex}
              {@const isSelected = selectedOption === i}
              <button
                type="button"
                on:click={() => selectOption(i)}
                disabled={revealed}
                class="w-full text-left px-4 py-3 rounded-xl border text-xs font-medium transition flex items-center justify-between gap-3
                  {revealed && isCorrect
                    ? 'border-emerald-400 bg-emerald-50 text-emerald-900'
                    : revealed && isSelected && !isCorrect
                      ? 'border-red-300 bg-red-50 text-red-900'
                      : isSelected
                        ? 'border-ifa-pine bg-ifa-pine/5 text-ifa-text-primary'
                        : 'border-ifa-border bg-ifa-card hover:border-ifa-pine/40 hover:bg-ifa-card-muted text-ifa-text-primary'}"
              >
                <span>{option}</span>
                {#if revealed && isCorrect}
                  <Check class="w-4 h-4 text-emerald-600 shrink-0" />
                {:else if revealed && isSelected && !isCorrect}
                  <AlertCircle class="w-4 h-4 text-red-500 shrink-0" />
                {/if}
              </button>
            {/each}
          </div>

          {#if revealed}
            <div class="rounded-xl bg-ifa-card-muted border border-ifa-border-light p-3.5 animate-in fade-in slide-in-from-bottom-2 duration-200">
              <p class="text-[11px] font-bold text-ifa-pine mb-1 flex items-center gap-1.5">
                <span class="w-1.5 h-1.5 rounded-full bg-ifa-accent-amber"></span>
                Why: targets <span class="font-mono">{question.targetSkillName}</span>
              </p>
              <p class="text-[11px] text-ifa-text-secondary leading-relaxed">{question.explanation}</p>
            </div>
          {/if}
        </div>

        <!-- Footer actions -->
        <div class="px-6 py-4 border-t border-ifa-border-light flex justify-end">
          {#if !revealed}
            <button
              type="button"
              on:click={submitAnswer}
              disabled={selectedOption === null}
              class="px-5 py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light disabled:opacity-40 disabled:cursor-not-allowed text-white text-xs font-bold transition"
            >
              Check Answer
            </button>
          {:else}
            <button
              type="button"
              on:click={nextQuestion}
              class="px-5 py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center gap-2"
            >
              <span>{currentIndex < totalQuestions - 1 ? 'Next Question' : 'See Results'}</span>
              <ArrowRight class="w-3.5 h-3.5" />
            </button>
          {/if}
        </div>
      {:else}
        <!-- Results screen -->
        <div class="px-6 py-8 text-center space-y-4">
          <div
            class="w-16 h-16 mx-auto rounded-full flex items-center justify-center {passed
              ? 'bg-emerald-100 text-emerald-600'
              : 'bg-amber-100 text-amber-600'}"
          >
            {#if passed}
              <Trophy class="w-8 h-8" />
            {:else}
              <RotateCcw class="w-8 h-8" />
            {/if}
          </div>

          <div>
            <h3 class="text-lg font-extrabold text-ifa-text-primary">
              {passed ? 'Module Passed!' : 'Almost there'}
            </h3>
            <p class="text-xs text-ifa-text-secondary mt-1">
              You scored <strong class="text-ifa-pine">{scorePercent}%</strong>
              ({correctCount}/{totalQuestions} correct){passed
                ? ' — the next module is unlocked.'
                : `. You need ${quiz.passingScorePercentage}% to continue.`}
            </p>
          </div>

          <div class="flex items-center justify-center gap-2.5 pt-2">
            {#if passed}
              <button
                type="button"
                on:click={handleClose}
                class="px-5 py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center gap-2"
              >
                <Check class="w-3.5 h-3.5" />
                <span>Continue</span>
              </button>
            {:else}
              <button
                type="button"
                on:click={retry}
                class="px-5 py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center gap-2"
              >
                <RotateCcw class="w-3.5 h-3.5" />
                <span>Retry Quiz</span>
              </button>
              <button
                type="button"
                on:click={handleClose}
                class="px-4 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary hover:text-ifa-text-primary text-xs font-semibold transition"
              >
                Review Lesson
              </button>
            {/if}
          </div>
        </div>
      {/if}
    </div>
  </div>
{/if}
