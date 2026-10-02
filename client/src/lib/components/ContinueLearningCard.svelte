<script lang="ts">
  import { ArrowRight, Clock, PlayCircle, Share2, Users } from 'lucide-svelte';
  import { activeCourse, courseProgress, moduleInfo, nextLesson } from '$lib/stores/coursesStore';

  export let onContinue = () => {};
  export let onShare = () => {};
  export let onCompareToggle = () => {};

  let copySuccess = false;

  // Derive display fields from the live course in the store.
  $: course = $activeCourse;
  $: progress = course ? courseProgress(course) : 0;
  $: info = course ? moduleInfo(course) : '';
  $: next = course ? nextLesson(course) : null;

  function handleShareClick() {
    onShare();
    copySuccess = true;
    setTimeout(() => (copySuccess = false), 2000);
  }
</script>

{#if course}
<div class="space-y-3">
  <!-- Section Title -->
  <div class="flex items-center justify-between">
    <div class="flex items-center gap-2">
      <div class="w-6 h-6 rounded-lg bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
        <PlayCircle class="w-3.5 h-3.5 text-emerald-700" />
      </div>
      <h3 class="text-sm font-bold text-ifa-text-primary tracking-tight">Continue Your Learning</h3>
    </div>
    <a href="/my-learning" class="text-xs font-semibold text-ifa-text-muted hover:text-ifa-pine transition flex items-center gap-1">
      <span>View all</span>
      <ArrowRight class="w-3.5 h-3.5" />
    </a>
  </div>

  <!-- Main Card Container -->
  <div class="bg-ifa-card rounded-3xl border border-ifa-border p-5 shadow-card hover:shadow-elevated transition-all">
    <div class="flex flex-col lg:flex-row items-stretch gap-5">
      <!-- Course Thumbnail -->
      <div class="relative w-full lg:w-44 h-36 rounded-2xl bg-gradient-to-br from-[#1C1F24] to-[#0D0F12] flex flex-col items-center justify-center text-white shrink-0 overflow-hidden border border-black/10">
        <!-- In Progress Badge -->
        <div class="absolute top-3 left-3 px-2 py-0.5 rounded-full bg-black/60 backdrop-blur-md text-[9px] font-mono tracking-widest text-emerald-400 border border-emerald-500/30 uppercase">
          IN PROGRESS
        </div>

        <!-- Glowing Course Sphere Visual -->
        <div class="relative flex items-center justify-center">
          <div class="absolute w-20 h-20 rounded-full bg-emerald-500/20 blur-xl"></div>
          <div class="w-16 h-16 rounded-full border border-emerald-400/40 flex items-center justify-center bg-black/40 shadow-inner">
            <span class="text-2xl font-black tracking-tight text-white">{course.thumbnail}</span>
          </div>
        </div>
      </div>

      <!-- Course Details -->
      <div class="flex-1 flex flex-col justify-between py-1">
        <div>
          <div class="flex items-center justify-between mb-1">
            <h4 class="text-base font-bold text-ifa-text-primary">{course.title}</h4>
            <span class="text-xs font-bold text-ifa-pine">{progress}%</span>
          </div>

          <!-- Progress Bar -->
          <div class="w-full h-2 rounded-full bg-ifa-bg border border-ifa-border overflow-hidden mb-2">
            <div
              class="h-full rounded-full bg-emerald-600 transition-all duration-500"
              style="width: {progress}%"
            ></div>
          </div>

          <p class="text-xs text-ifa-text-secondary font-medium">{info}{next ? ` • ${next.module.title}` : ''}</p>
        </div>

        <!-- Actions -->
        <div class="flex flex-wrap items-center gap-2.5 pt-3">
          <button
            type="button"
            on:click={onContinue}
            class="px-4 py-2 rounded-full bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center gap-2 shadow-sm"
          >
            <span>Continue Learning</span>
            <ArrowRight class="w-3.5 h-3.5" />
          </button>

          <!-- Social Share Button (User Feature) -->
          <button
            type="button"
            on:click={handleShareClick}
            title="Share course with a friend"
            class="px-3 py-2 rounded-full bg-ifa-card-muted hover:bg-ifa-bg text-ifa-text-secondary hover:text-ifa-pine border border-ifa-border text-xs font-semibold transition flex items-center gap-1.5"
          >
            <Share2 class="w-3.5 h-3.5" />
            <span>{copySuccess ? 'Link Copied!' : 'Share'}</span>
          </button>

          <!-- Peer Progress Compare Button (User Feature) -->
          <button
            type="button"
            on:click={onCompareToggle}
            title="Compare progress with peer"
            class="px-3 py-2 rounded-full bg-ifa-card-muted hover:bg-ifa-bg text-ifa-text-secondary hover:text-ifa-pine border border-ifa-border text-xs font-semibold transition flex items-center gap-1.5"
          >
            <Users class="w-3.5 h-3.5 text-ifa-accent-purple" />
            <span>Compare</span>
          </button>
        </div>
      </div>

      <!-- Next Lesson Preview Box -->
      {#if next}
      <div class="w-full lg:w-64 bg-ifa-card-muted/70 rounded-2xl p-4 border border-ifa-border flex flex-col justify-between shrink-0">
        <div>
          <div class="flex items-center gap-1.5 text-[11px] font-semibold text-ifa-text-muted uppercase tracking-wider mb-1">
            <span class="w-1.5 h-1.5 rounded-full bg-ifa-accent-amber"></span>
            <span>Next Lesson</span>
          </div>
          <h5 class="text-xs font-bold text-ifa-text-primary mb-1">{next.lesson.title}</h5>
          <p class="text-[11px] text-ifa-text-secondary line-clamp-2 leading-relaxed">
            {next.lesson.summary}
          </p>
        </div>

        <div class="flex items-center justify-between pt-3 border-t border-ifa-border-light">
          <div class="flex items-center gap-1 text-[11px] font-medium text-ifa-text-muted">
            <Clock class="w-3.5 h-3.5" />
            <span>{next.lesson.duration}</span>
          </div>
          <button
            type="button"
            on:click={onContinue}
            class="w-7 h-7 rounded-full bg-white border border-ifa-border flex items-center justify-center text-ifa-pine hover:bg-ifa-pine hover:text-white transition shadow-soft"
          >
            <ArrowRight class="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
      {/if}
    </div>
  </div>
</div>
{/if}
