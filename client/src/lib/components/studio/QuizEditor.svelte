<script lang="ts">
  import { Plus, Trash2, ClipboardCheck } from 'lucide-svelte';
  import type { QuizAuthoringDto } from '$lib/api';

  export let value: QuizAuthoringDto[] = [];
  export let defaultPassingScore = 70;
  export let disabled = false;
  export let prefix = 'assessment';

  function addQuiz() {
    value = [...value, { title: '', passingScorePercentage: defaultPassingScore, kind: 0, questions: [] }];
  }

  function addQuestion(index: number) {
    value[index].questions = [...value[index].questions, { prompt: '', options: ['', ''], correctOptionIndex: -1, explanation: '', targetSkillName: '' }];
    value = [...value];
  }

  function removeOption(quizIndex: number, questionIndex: number, optionIndex: number) {
    const question = value[quizIndex].questions[questionIndex];
    question.options = question.options.filter((_, index) => index !== optionIndex);
    if (question.correctOptionIndex === optionIndex) question.correctOptionIndex = -1;
    else if (question.correctOptionIndex > optionIndex) question.correctOptionIndex -= 1;
    value = [...value];
  }
</script>

<fieldset {disabled} class="min-w-0 space-y-4 border-t border-ifa-border pt-5">
  <div class="flex flex-wrap items-center justify-between gap-2">
    <h5 class="text-xs font-bold text-ifa-text-primary inline-flex items-center gap-2"><ClipboardCheck class="w-4 h-4" />Assessments</h5>
    <button type="button" on:click={addQuiz} class="px-3 py-1.5 rounded-lg border border-ifa-border text-xs font-semibold text-ifa-pine inline-flex items-center gap-1"><Plus class="w-3.5 h-3.5" />Add Assessment</button>
  </div>
  <p class="text-xs text-ifa-text-secondary">Author multiple-choice questions and select one correct answer. Save assessments separately from lesson changes.</p>
  {#each value as quiz, qi}
    <div class="rounded-xl border border-ifa-border bg-ifa-card-muted/30 p-4 space-y-4">
      <div class="flex items-start gap-2">
        <label class="flex-1 text-xs font-semibold text-ifa-text-primary">Assessment Title
          <input type="text" bind:value={quiz.title} placeholder="e.g. Module knowledge check" class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-xs" />
        </label>
        <button type="button" aria-label="Remove assessment" on:click={() => { if (confirm('Remove this assessment from the draft? Save assessments to apply the removal.')) value = value.filter((_, index) => index !== qi); }} class="mt-5 p-2 rounded-lg text-ifa-text-muted hover:text-red-600"><Trash2 class="w-4 h-4" /></button>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <label class="text-xs text-ifa-text-secondary">Type
          <select bind:value={quiz.kind} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-ifa-text-primary"><option value={0}>Knowledge Check</option><option value={1}>Module Exam</option></select>
        </label>
        <label class="text-xs text-ifa-text-secondary">Passing Score (%)
          <input type="number" min="1" max="100" bind:value={quiz.passingScorePercentage} class="mt-1 w-full px-3 py-2 rounded-xl bg-ifa-card border border-ifa-border text-ifa-text-primary" />
        </label>
      </div>
      {#each quiz.questions as question, questionIndex}
        <div class="rounded-xl border border-ifa-border bg-ifa-card p-3 space-y-3">
          <div class="flex items-start gap-2">
            <label class="flex-1 text-xs font-semibold text-ifa-text-primary">Question {questionIndex + 1}
              <textarea bind:value={question.prompt} rows="2" class="mt-1 w-full px-3 py-2 rounded-lg bg-ifa-card border border-ifa-border text-xs"></textarea>
            </label>
            <button type="button" aria-label="Remove question" on:click={() => { quiz.questions = quiz.questions.filter((_, index) => index !== questionIndex); value = [...value]; }} class="mt-5 p-2 text-ifa-text-muted hover:text-red-600"><Trash2 class="w-3.5 h-3.5" /></button>
          </div>
          <fieldset class="space-y-2">
            <legend class="text-[11px] font-semibold text-ifa-text-secondary mb-2">Options (Select the Correct Answer)</legend>
            {#each question.options as option, oi}
              <div class="flex items-center gap-2">
                <input type="radio" name={`${prefix}-${qi}-${questionIndex}-answer`} checked={question.correctOptionIndex === oi} on:change={() => { question.correctOptionIndex = oi; value = [...value]; }} aria-label={`Option ${oi + 1} is correct`} />
                <input type="text" bind:value={question.options[oi]} aria-label={`Option ${oi + 1}`} class="min-w-0 flex-1 px-3 py-2 rounded-lg bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary" />
                <button type="button" aria-label={`Remove option ${oi + 1}`} disabled={question.options.length <= 2} on:click={() => removeOption(qi, questionIndex, oi)} class="p-1 text-ifa-text-muted hover:text-red-600 disabled:opacity-30"><Trash2 class="w-3.5 h-3.5" /></button>
              </div>
            {/each}
            <button type="button" disabled={question.options.length >= 8} on:click={() => { question.options = [...question.options, '']; value = [...value]; }} class="text-xs font-semibold text-ifa-pine disabled:opacity-40">+ Add Option</button>
          </fieldset>
          <label class="block text-xs text-ifa-text-secondary">Explanation Shown After Submission
            <textarea bind:value={question.explanation} rows="2" class="mt-1 w-full px-3 py-2 rounded-lg bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary"></textarea>
          </label>
          <label class="block text-xs text-ifa-text-secondary">Target Skill (Optional)
            <input type="text" bind:value={question.targetSkillName} class="mt-1 w-full px-3 py-2 rounded-lg bg-ifa-card border border-ifa-border text-xs text-ifa-text-primary" />
          </label>
        </div>
      {:else}
        <p class="text-xs text-ifa-text-muted">Add at least one question before saving this assessment.</p>
      {/each}
      <button type="button" on:click={() => addQuestion(qi)} class="text-xs font-semibold text-ifa-pine inline-flex items-center gap-1"><Plus class="w-3.5 h-3.5" />Add Question</button>
    </div>
  {:else}
    <p class="text-xs text-ifa-text-muted">No assessments in this module yet.</p>
  {/each}
</fieldset>
