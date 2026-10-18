<script lang="ts">
  import { goto } from '$app/navigation';
  import { ArrowRight, Sparkles, Compass, FlaskConical, Mic, Zap } from 'lucide-svelte';

  export let onGoalSubmit = (goal: string) => {
    // Kept for backwards compatibility with the home page wiring; the actual
    // advisor conversation now lives on the dedicated /chat page.
    void goal;
  };

  const chips = [
    { icon: Sparkles, label: 'Full-screen AI advisor chat' },
    { icon: FlaskConical, label: 'Academic research grounding' },
    { icon: Mic, label: 'Voxide voice' },
    { icon: Zap, label: 'JIT modules' }
  ];
</script>

<div class="relative overflow-hidden rounded-3xl border border-ifa-border bg-ifa-card shadow-card">
  <!-- Animated wave background (no photo): three drifting wave layers -->
  <div class="absolute inset-0 ifa-wave-field pointer-events-none" aria-hidden="true">
    <div class="absolute inset-0 ifa-wave-glow"></div>
    <svg class="ifa-wave ifa-wave-slow" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
      <path class="ifa-wave-path-1" d="M0 160 C 360 260 720 60 1080 160 C 1440 260 1800 60 2160 160 C 2520 260 2700 110 2880 160 L 2880 320 L 0 320 Z" />
    </svg>
    <svg class="ifa-wave ifa-wave-fast" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
      <path class="ifa-wave-path-2" d="M0 200 C 480 100 960 300 1440 200 C 1920 100 2400 300 2880 200 L 2880 320 L 0 320 Z" />
    </svg>
    <svg class="ifa-wave ifa-wave-mid" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
      <path class="ifa-wave-path-3" d="M0 240 C 720 160 1440 320 2160 240 C 2520 200 2700 260 2880 240 L 2880 320 L 0 320 Z" />
    </svg>
  </div>

  <!-- Readability scrim (theme-aware) so text always contrasts with the waves -->
  <div class="absolute inset-0 ifa-hero-scrim pointer-events-none"></div>

  <!-- Content Container -->
  <div class="relative z-10 p-8 md:p-10 max-w-2xl">
    <div class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md ifa-hero-chip text-[11px] font-mono tracking-wider font-semibold uppercase mb-3">
      // AI LEARNING ASSISTANT
    </div>

    <h2 class="text-3xl font-extrabold tracking-tight text-ifa-pine leading-tight mb-2">
      What do you want to learn today?
    </h2>
    <p class="text-xs text-ifa-text-secondary font-medium leading-relaxed mb-6">
      Chat with IFA about your goal — it asks the right questions, then builds a personalized course.
    </p>

    <!-- CTA buttons: the advisor conversation lives on the dedicated chat page -->
    <div class="flex flex-wrap items-center gap-3 mb-6">
      <button
        type="button"
        on:click={() => {
          onGoalSubmit('');
          goto('/chat');
        }}
        class="px-5 py-3 rounded-xl bg-ifa-pine text-white text-sm font-bold transition shadow-elevated flex items-center gap-2 hover:bg-ifa-pine-light"
      >
        <Sparkles class="w-4 h-4" />
        <span>Chat with IFA</span>
        <ArrowRight class="w-4 h-4" />
      </button>
      <button
        type="button"
        on:click={() => goto('/courses')}
        class="px-5 py-3 rounded-xl border border-ifa-border bg-ifa-card/90 backdrop-blur text-ifa-text-primary text-sm font-bold hover:bg-ifa-card-muted transition flex items-center gap-2"
      >
        <Compass class="w-4 h-4" />
        <span>Browse courses</span>
      </button>
    </div>

    <!-- Capability chips -->
    <div class="flex flex-wrap items-center gap-2">
      {#each chips as chip (chip.label)}
        <span class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft">
          <svelte:component this={chip.icon} class="w-3.5 h-3.5 text-ifa-pine" />
          {chip.label}
        </span>
      {/each}
    </div>
  </div>
</div>

<style>
  /* Animated wave field — light and dark aware */
  .ifa-wave-field {
    background: linear-gradient(160deg, #eef3ea 0%, #f8f6f0 55%, #e7f0e6 100%);
  }
  :global(.dark) .ifa-wave-field {
    background: linear-gradient(160deg, #0d1a12 0%, #0a0a0a 55%, #101c14 100%);
  }

  .ifa-wave-glow {
    position: absolute;
    inset: 0;
    background: radial-gradient(60% 90% at 85% 10%, rgba(42, 157, 104, 0.18), transparent 70%);
  }
  :global(.dark) .ifa-wave-glow {
    background: radial-gradient(60% 90% at 85% 10%, rgba(34, 197, 94, 0.12), transparent 70%);
  }

  .ifa-wave {
    position: absolute;
    bottom: -2px;
    left: 0;
    width: 200%;
    height: 62%;
    will-change: transform;
  }
  .ifa-wave-slow { animation: ifa-wave-drift 26s linear infinite; }
  .ifa-wave-mid { animation: ifa-wave-drift 18s linear infinite reverse; height: 48%; opacity: 0.75; }
  .ifa-wave-fast { animation: ifa-wave-drift 12s linear infinite; height: 34%; opacity: 0.55; }

  @keyframes ifa-wave-drift {
    from { transform: translateX(0); }
    to { transform: translateX(-50%); }
  }

  .ifa-wave-path-1 { fill: rgba(42, 157, 104, 0.16); }
  .ifa-wave-path-2 { fill: rgba(27, 61, 47, 0.12); }
  .ifa-wave-path-3 { fill: rgba(42, 157, 104, 0.20); }
  :global(.dark) .ifa-wave-path-1 { fill: rgba(34, 197, 94, 0.10); }
  :global(.dark) .ifa-wave-path-2 { fill: rgba(34, 197, 94, 0.06); }
  :global(.dark) .ifa-wave-path-3 { fill: rgba(34, 197, 94, 0.13); }

  /* Readability scrim over the waves (kept off Tailwind's gradient utilities
     so the global dark-mode gradient overrides in app.css can't clobber it) */
  .ifa-hero-scrim {
    background: linear-gradient(to right, rgba(248, 246, 240, 0.96) 0%, rgba(248, 246, 240, 0.88) 45%, rgba(248, 246, 240, 0.35) 75%, rgba(248, 246, 240, 0.05) 100%);
  }
  :global(.dark) .ifa-hero-scrim {
    background: linear-gradient(to right, rgba(10, 10, 10, 0.96) 0%, rgba(10, 10, 10, 0.88) 45%, rgba(10, 10, 10, 0.45) 75%, rgba(10, 10, 10, 0.08) 100%);
  }

  .ifa-hero-chip {
    background: rgba(27, 61, 47, 0.10);
    color: #1b3d2f;
  }
  :global(.dark) .ifa-hero-chip {
    background: rgba(34, 197, 94, 0.12);
    color: #4ade80;
  }

  @media (prefers-reduced-motion: reduce) {
    .ifa-wave-slow,
    .ifa-wave-mid,
    .ifa-wave-fast {
      animation: none;
    }
  }
</style>
