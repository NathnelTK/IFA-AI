<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { ArrowLeft, ArrowRight, CheckCircle2, Clock, Circle } from 'lucide-svelte';
  import type { Lesson } from '$lib/types';

  export let lesson: Lesson;
  export let moduleTitle: string;
  export let hasNext = false;

  const dispatch = createEventDispatcher<{
    back: void;
    complete: { lessonId: string };
    next: void;
  }>();
</script>

<div class="max-w-3xl mx-auto space-y-6">
  <!-- Breadcrumb / Back -->
  <button
    type="button"
    on:click={() => dispatch('back')}
    class="flex items-center gap-1.5 text-sm font-semibold text-ifa-text-muted hover:text-ifa-pine transition"
  >
    <ArrowLeft class="w-4 h-4" />
    <span>Back to course</span>
  </button>

  <div class="bg-ifa-card rounded-3xl border border-ifa-border p-6 md:p-8 shadow-card space-y-5">
    <!-- Header -->
    <div>
      <p class="text-[11px] font-bold uppercase tracking-wider text-ifa-text-muted mb-1">{moduleTitle}</p>
      <h1 class="text-2xl font-bold text-ifa-text-primary">{lesson.title}</h1>
      <div class="flex items-center gap-3 mt-2 text-xs text-ifa-text-secondary">
        <span class="flex items-center gap-1"><Clock class="w-3.5 h-3.5" />{lesson.duration}</span>
        {#if lesson.completed}
          <span class="flex items-center gap-1 text-emerald-600 font-semibold">
            <CheckCircle2 class="w-3.5 h-3.5" />Completed
          </span>
        {:else}
          <span class="flex items-center gap-1 text-ifa-text-muted">
            <Circle class="w-3.5 h-3.5" />Not completed
          </span>
        {/if}
      </div>
    </div>

    <!-- Lesson body -->
    <div class="prose prose-sm max-w-none space-y-4">
      <p class="text-sm font-medium text-ifa-text-secondary italic">{lesson.summary}</p>
      {#each lesson.content as paragraph}
        <p class="text-sm leading-relaxed text-ifa-text-primary">{paragraph}</p>
      {/each}
    </div>

    <!-- Actions -->
    <div class="flex flex-wrap items-center gap-3 pt-4 border-t border-ifa-border">
      <button
        type="button"
        on:click={() => dispatch('complete', { lessonId: lesson.id })}
        class="px-4 py-2.5 rounded-full text-xs font-bold transition flex items-center gap-2 shadow-sm {lesson.completed
          ? 'bg-emerald-100 text-emerald-700 hover:bg-emerald-200'
          : 'bg-ifa-pine text-white hover:bg-ifa-pine-light'}"
      >
        <CheckCircle2 class="w-3.5 h-3.5" />
        <span>{lesson.completed ? 'Completed — mark incomplete' : 'Mark as complete'}</span>
      </button>

      {#if hasNext}
        <button
          type="button"
          on:click={() => dispatch('next')}
          class="px-4 py-2.5 rounded-full bg-ifa-card-muted hover:bg-ifa-bg text-ifa-text-secondary hover:text-ifa-pine border border-ifa-border text-xs font-semibold transition flex items-center gap-2"
        >
          <span>Next lesson</span>
          <ArrowRight class="w-3.5 h-3.5" />
        </button>
      {/if}
    </div>
  </div>
</div>
