<script lang="ts">
  import { ArrowRight, Play } from 'lucide-svelte';

  export let course: any;
  export let onContinue = () => {};
  export let onShare = () => {};
  export let isPaused = false;
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card hover:shadow-elevated transition">
  <div class="flex items-start justify-between mb-3">
    <div>
      <h3 class="text-sm font-bold text-ifa-text-primary">{course.title}</h3>
      <p class="text-xs text-ifa-text-secondary mt-1">{course.moduleInfo}</p>
    </div>
    <div class="text-right">
      <div class="text-lg font-black text-ifa-pine">{course.progressPercent}%</div>
    </div>
  </div>

  <!-- Progress Bar -->
  <div class="w-full h-1.5 bg-ifa-card-muted rounded-full mb-3">
    <div
      class="h-full bg-ifa-pine rounded-full transition-all duration-1000"
      style="width: {course.progressPercent}%"
    ></div>
  </div>

  <!-- JIT Status -->
  {#if course.jitStatus}
    <div class="text-[10px] text-ifa-text-muted mb-3 font-mono">{course.jitStatus}</div>
  {/if}

  <!-- Actions -->
  <div class="flex gap-2">
    <button
      type="button"
      on:click={onContinue}
      class="flex-1 py-2 bg-ifa-pine text-white rounded-lg text-xs font-semibold flex items-center justify-center gap-1.5 hover:bg-emerald-800 transition"
    >
      {#if isPaused}
        <Play class="w-3 h-3" />
        <span>Resume</span>
      {:else}
        <ArrowRight class="w-3 h-3" />
        <span>Continue</span>
      {/if}
    </button>
    <button
      type="button"
      on:click={onShare}
      class="px-3 py-2 border border-ifa-border rounded-lg text-xs font-semibold text-ifa-text-secondary hover:bg-ifa-card-muted transition"
    >
      Share
    </button>
  </div>
</div>
