<script lang="ts">
  import { ArrowRight, Play, Share2, Users } from 'lucide-svelte';

  export let course: any;
  export let onContinue = () => {};

  function handleShare() {
    console.log('Sharing course:', course.id);
  }

  function handleCompare() {
    console.log('Comparing progress for:', course.id);
  }
</script>

<div class="bg-gradient-to-br from-ifa-pine to-emerald-700 rounded-3xl p-6 text-white shadow-elevated">
  <div class="flex items-start justify-between mb-4">
    <div>
      <div class="flex items-center gap-2 mb-2">
        <span class="text-xs font-semibold bg-white/20 px-2 py-1 rounded-full">Continue Learning</span>
        <span class="text-xs font-mono text-emerald-200">{course.jitStatus}</span>
      </div>
      <h2 class="text-xl font-bold">{course.title}</h2>
      <p class="text-sm text-emerald-100 mt-1">{course.moduleInfo}</p>
    </div>
    <div class="text-right">
      <div class="text-3xl font-black">{course.progressPercent}%</div>
      <div class="text-xs text-emerald-200">Complete</div>
    </div>
  </div>

  <!-- Progress Bar -->
  <div class="w-full h-2 bg-white/20 rounded-full mb-4">
    <div
      class="h-full bg-white rounded-full transition-all duration-1000"
      style="width: {course.progressPercent}%"
    ></div>
  </div>

  <!-- Next Lesson Info -->
  <div class="bg-white/10 rounded-xl p-4 mb-4">
    <div class="flex items-center justify-between">
      <div>
        <p class="text-xs text-emerald-200 mb-1">Next: {course.nextLessonTitle}</p>
        <p class="text-sm font-semibold">{course.nextLessonSummary}</p>
      </div>
      <div class="text-xs text-emerald-200 flex items-center gap-1">
        <Play class="w-3 h-3" />
        <span>{course.nextLessonDuration}</span>
      </div>
    </div>
  </div>

  <!-- Actions -->
  <div class="flex gap-3">
    <button
      type="button"
      on:click={onContinue}
      class="flex-1 py-3 bg-white text-ifa-pine rounded-xl font-semibold flex items-center justify-center gap-2 hover:bg-emerald-50 transition"
    >
      <span>Continue Learning</span>
      <ArrowRight class="w-4 h-4" />
    </button>
    <button
      type="button"
      on:click={handleShare}
      class="px-4 py-3 bg-white/10 rounded-xl hover:bg-white/20 transition"
      title="Share Course"
    >
      <Share2 class="w-4 h-4" />
    </button>
    <button
      type="button"
      on:click={handleCompare}
      class="px-4 py-3 bg-white/10 rounded-xl hover:bg-white/20 transition"
      title="Compare Progress"
    >
      <Users class="w-4 h-4" />
    </button>
  </div>
</div>
