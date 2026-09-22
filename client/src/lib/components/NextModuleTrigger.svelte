<script lang="ts">
  import { Lock, Sparkles, ArrowRight, CheckCircle2, Loader2 } from 'lucide-svelte';

  // `locked`  → quiz not yet passed, trigger disabled.
  // `ready`   → quiz passed, next module can be generated.
  // `building`→ Model 3 is materialising the next module (JIT).
  // `done`    → this was the final module in the blueprint.
  export let state: 'locked' | 'ready' | 'building' | 'done' = 'locked';
  export let nextModuleTitle = '';
  export let onGenerate = () => {};
</script>

<div class="rounded-3xl border border-ifa-border bg-ifa-card shadow-card overflow-hidden">
  <div class="p-5 flex flex-col sm:flex-row items-start sm:items-center gap-4 justify-between">
    <div class="flex items-start gap-3">
      <div
        class="w-10 h-10 rounded-2xl flex items-center justify-center shrink-0
          {state === 'done'
            ? 'bg-emerald-100 text-emerald-600'
            : state === 'locked'
              ? 'bg-ifa-card-muted text-ifa-text-muted'
              : 'bg-ifa-pine/10 text-ifa-pine'}"
      >
        {#if state === 'locked'}
          <Lock class="w-5 h-5" />
        {:else if state === 'done'}
          <CheckCircle2 class="w-5 h-5" />
        {:else if state === 'building'}
          <Loader2 class="w-5 h-5 animate-spin" />
        {:else}
          <Sparkles class="w-5 h-5" />
        {/if}
      </div>

      <div>
        {#if state === 'locked'}
          <h4 class="text-sm font-bold text-ifa-text-primary">Complete the quiz to continue</h4>
          <p class="text-[11px] text-ifa-text-secondary mt-0.5">
            Pass this module's diagnostic to unlock Just-In-Time generation of the next module.
          </p>
        {:else if state === 'ready'}
          <h4 class="text-sm font-bold text-ifa-text-primary">Ready for the next module</h4>
          <p class="text-[11px] text-ifa-text-secondary mt-0.5">
            Up next: <span class="font-semibold text-ifa-pine">{nextModuleTitle}</span>
          </p>
        {:else if state === 'building'}
          <h4 class="text-sm font-bold text-ifa-pine">Generating your next module…</h4>
          <p class="text-[11px] text-ifa-text-secondary mt-0.5">
            Synthesising research, curating videos & building lessons via the Course Builder model.
          </p>
        {:else}
          <h4 class="text-sm font-bold text-ifa-text-primary">Course complete 🎉</h4>
          <p class="text-[11px] text-ifa-text-secondary mt-0.5">
            You've finished every module in this learning path. Beautiful work.
          </p>
        {/if}
      </div>
    </div>

    {#if state === 'ready' || state === 'building'}
      <button
        type="button"
        on:click={onGenerate}
        disabled={state === 'building'}
        class="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light disabled:opacity-70 disabled:cursor-wait text-white text-xs font-bold transition flex items-center justify-center gap-2 shadow-sm shrink-0"
      >
        {#if state === 'building'}
          <Loader2 class="w-3.5 h-3.5 animate-spin" />
          <span>Generating…</span>
        {:else}
          <span>Complete & Generate Next Module</span>
          <ArrowRight class="w-3.5 h-3.5" />
        {/if}
      </button>
    {/if}
  </div>

  {#if state === 'building'}
    <div class="h-1 w-full bg-ifa-border overflow-hidden">
      <div class="h-full w-1/3 bg-ifa-pine animate-[ifaSlide_1.2s_ease-in-out_infinite]"></div>
    </div>
  {/if}
</div>

<style>
  @keyframes ifaSlide {
    0% {
      transform: translateX(-100%);
    }
    100% {
      transform: translateX(400%);
    }
  }
</style>
