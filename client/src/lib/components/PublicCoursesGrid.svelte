<script lang="ts">
  import { Star, Clock, ChevronLeft, ChevronRight, ArrowRight, Compass, PlusCircle } from 'lucide-svelte';
  import { publicCourses } from '../stores/dashboardStore';

  export let onPublishCourse = () => {};

  let currentIndex = 0;

  function next() {
    if (currentIndex < $publicCourses.length - 1) currentIndex++;
  }

  function prev() {
    if (currentIndex > 0) currentIndex--;
  }
</script>

<div class="space-y-3">
  <!-- Header & Controls -->
  <div class="flex items-center justify-between">
    <div>
      <div class="flex items-center gap-2 mb-0.5">
        <div class="w-6 h-6 rounded-lg bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
          <Compass class="w-3.5 h-3.5 text-emerald-700" />
        </div>
        <h3 class="text-sm font-bold text-ifa-text-primary tracking-tight">Explore Public Courses</h3>
      </div>
      <p class="text-xs text-ifa-text-secondary font-medium">
        Learn from expert-created courses. Enroll in any course and start learning today.
      </p>
    </div>

    <!-- Carousel Nav Buttons & Publish Button -->
    <div class="flex items-center gap-2">
      <button
        type="button"
        on:click={onPublishCourse}
        class="hidden sm:flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-ifa-card border border-ifa-border text-xs font-semibold text-ifa-pine hover:bg-ifa-card-muted shadow-soft transition"
      >
        <PlusCircle class="w-3.5 h-3.5 text-emerald-600" />
        <span>Publish Course</span>
      </button>

      <div class="flex items-center gap-1">
        <button
          type="button"
          on:click={prev}
          disabled={currentIndex === 0}
          class="w-7 h-7 rounded-full bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine disabled:opacity-40 transition shadow-soft"
        >
          <ChevronLeft class="w-3.5 h-3.5" />
        </button>
        <button
          type="button"
          on:click={next}
          disabled={currentIndex >= $publicCourses.length - 1}
          class="w-7 h-7 rounded-full bg-ifa-card border border-ifa-border flex items-center justify-center text-ifa-text-secondary hover:text-ifa-pine disabled:opacity-40 transition shadow-soft"
        >
          <ChevronRight class="w-3.5 h-3.5" />
        </button>
      </div>
    </div>
  </div>

  <!-- Cards Grid -->
  <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
    {#each $publicCourses as course}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-3.5 flex flex-col justify-between shadow-card hover:shadow-elevated transition-all group">
        <!-- Thumbnail Container -->
        <div class="relative w-full h-32 rounded-xl overflow-hidden mb-3 bg-black/5">
          <img
            src={course.imageUrl}
            alt={course.title}
            class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
          />
          {#if course.badge}
            <div class="absolute top-2 left-2 px-2 py-0.5 rounded-md text-[10px] font-bold text-white shadow-sm {course.badge === 'Popular'
              ? 'bg-amber-600'
              : course.badge === 'Bestseller'
                ? 'bg-emerald-600'
                : 'bg-indigo-600'}">
              {course.badge}
            </div>
          {/if}
        </div>

        <!-- Course Meta -->
        <div>
          <h4 class="text-xs font-bold text-ifa-text-primary line-clamp-1 group-hover:text-ifa-pine transition">
            {course.title}
          </h4>
          <p class="text-[11px] text-ifa-text-muted mt-0.5">{course.provider}</p>

          <div class="flex items-center justify-between text-[11px] text-ifa-text-secondary mt-2">
            <div class="flex items-center gap-1 font-semibold text-amber-600">
              <Star class="w-3 h-3 fill-current" />
              <span>{course.rating}</span>
              <span class="text-ifa-text-muted font-normal">({course.reviewCount})</span>
            </div>
            <div class="flex items-center gap-1 text-ifa-text-muted">
              <Clock class="w-3 h-3" />
              <span>{course.duration}</span>
            </div>
          </div>
        </div>

        <!-- Enroll Action -->
        <div class="mt-3 pt-2.5 border-t border-ifa-border-light flex items-center justify-between">
          <button
            type="button"
            class="text-xs font-bold text-ifa-pine hover:text-ifa-pine-light transition flex items-center gap-1 group/btn"
          >
            <span>Enroll Now</span>
            <ArrowRight class="w-3 h-3 group-hover/btn:translate-x-0.5 transition-transform" />
          </button>
        </div>
      </div>
    {/each}
  </div>
</div>
