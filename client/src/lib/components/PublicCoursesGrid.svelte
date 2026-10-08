<script lang="ts">
  import { Star, Clock, ArrowRight, Compass, PlusCircle, Loader2 } from 'lucide-svelte';
  import { goto } from '$app/navigation';
  import { publicCourses } from '../stores/dashboardStore';
  import { coursesApi } from '$lib/api';
  import { courseCover } from '$lib/utils/cover';
  import type { CourseCard } from '$lib/types';

  export let onPublishCourse = () => {};

  // The home page shows only a preview of the newest courses; the full catalog
  // lives on the marketplace page (reachable via "Show all").
  const HOME_PREVIEW_COUNT = 4;
  $: previewCourses = $publicCourses.slice(0, HOME_PREVIEW_COUNT);

  let enrollingId: string | null = null;
  let enrollError = '';

  async function handleEnroll(course: CourseCard) {
    if (enrollingId) return;
    enrollError = '';
    if (!course.shareCode) {
      // No share code (offline demo data or a private course) — send the learner
      // to the marketplace where the real catalog is loaded.
      await goto('/marketplace');
      return;
    }
    enrollingId = course.id;
    try {
      const result = await coursesApi.join(course.shareCode);
      const targetId = result?.courseId || course.id;
      await goto(`/courses/${targetId}`);
    } catch (cause) {
      enrollError = cause instanceof Error ? cause.message : 'Enrollment failed.';
    } finally {
      enrollingId = null;
    }
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

    <!-- Publish Button & Show all -->
    <div class="flex items-center gap-2">
      <button
        type="button"
        on:click={onPublishCourse}
        class="hidden sm:flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-ifa-card border border-ifa-border text-xs font-semibold text-ifa-pine hover:bg-ifa-card-muted shadow-soft transition"
      >
        <PlusCircle class="w-3.5 h-3.5 text-emerald-600" />
        <span>Publish Course</span>
      </button>

      <a
        href="/marketplace"
        class="flex items-center gap-1 px-3 py-1.5 rounded-full bg-ifa-pine text-white text-xs font-semibold hover:bg-ifa-pine-light shadow-soft transition"
      >
        <span>Show all</span>
        <ArrowRight class="w-3.5 h-3.5" />
      </a>
    </div>
  </div>

  {#if enrollError}
    <p role="alert" class="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-[11px] text-red-700">{enrollError}</p>
  {/if}

  <!-- Cards Grid (preview: newest few) -->
  <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
    {#each previewCourses as course}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-3.5 flex flex-col justify-between shadow-card hover:shadow-elevated transition-all group">
        <!-- Thumbnail Container -->
        <div class="relative w-full h-32 rounded-xl overflow-hidden mb-3 bg-black/5">
          <img
            src={course.imageUrl}
            alt={course.title}
            loading="lazy"
            on:error={(e) => ((e.currentTarget as HTMLImageElement).src = courseCover(course.title))}
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
            on:click={() => handleEnroll(course)}
            disabled={enrollingId !== null}
            class="text-xs font-bold text-ifa-pine hover:text-ifa-pine-light transition flex items-center gap-1 group/btn disabled:opacity-50"
          >
            {#if enrollingId === course.id}
              <Loader2 class="w-3 h-3 animate-spin" />
              <span>Enrolling…</span>
            {:else}
              <span>Enroll Now</span>
              <ArrowRight class="w-3 h-3 group-hover/btn:translate-x-0.5 transition-transform" />
            {/if}
          </button>
        </div>
      </div>
    {/each}
  </div>
</div>
