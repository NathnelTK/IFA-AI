<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { Lightbulb, Star, Clock, BookOpen, TrendingUp, Loader2 } from 'lucide-svelte';
  import {
    recommendationsApi,
    coursesApi,
    type RecommendationDto,
    type CourseSummaryDto
  } from '$lib/api';

  interface UiRecommendation {
    id: string;
    title: string;
    reason: string;
    category: string;
    estimatedHours: number;
    confidenceScore: number;
    actionType: string;
    courseId?: string;
  }

  let loading = true;
  let error = '';
  let recommendations: UiRecommendation[] = [];
  let myCourses: CourseSummaryDto[] = [];

  const matchAccuracy = () => {
    if (recommendations.length === 0) return 0;
    const avg =
      recommendations.reduce((sum, r) => sum + (r.confidenceScore || 0), 0) / recommendations.length;
    return Math.round(avg <= 1 ? avg * 100 : avg);
  };

  function openRecommendation(rec: UiRecommendation) {
    if (rec.courseId) void goto(`/courses/${rec.courseId}`);
    else void goto('/marketplace');
  }

  onMount(async () => {
    try {
      const [recs, courses] = await Promise.all([
        recommendationsApi.list().catch(() => [] as RecommendationDto[]),
        coursesApi.listMine().catch(() => [] as CourseSummaryDto[])
      ]);
      recommendations = recs.map((r) => ({
        id: r.id,
        title: r.title,
        reason: r.reason,
        category: r.category || 'General',
        estimatedHours: r.estimatedHours || 4,
        confidenceScore: r.confidenceScore || 0,
        actionType: r.actionType,
        courseId: r.courseId
      }));
      myCourses = courses;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load recommendations.';
    } finally {
      loading = false;
    }
  });
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div>
      <h1 class="text-3xl font-bold text-ifa-text-primary flex items-center gap-3">
        <Lightbulb class="w-8 h-8 text-ifa-accent-green" />
        AI Recommendations
      </h1>
      <p class="text-ifa-text-secondary mt-1">Personalized learning paths based on your progress and goals</p>
    </div>
  </div>

  {#if error}
    <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>
  {/if}

  <!-- Stats Cards -->
  <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
    <div class="bg-ifa-card rounded-xl p-5 border border-ifa-border shadow-card">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-ifa-accent-green/10 flex items-center justify-center">
          <Star class="w-5 h-5 text-ifa-accent-green" />
        </div>
        <div>
          <p class="text-2xl font-bold text-ifa-text-primary">{matchAccuracy()}%</p>
          <p class="text-xs text-ifa-text-secondary">Avg. Confidence</p>
        </div>
      </div>
    </div>
    <div class="bg-ifa-card rounded-xl p-5 border border-ifa-border shadow-card">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-ifa-accent-blue/10 flex items-center justify-center">
          <BookOpen class="w-5 h-5 text-ifa-accent-blue" />
        </div>
        <div>
          <p class="text-2xl font-bold text-ifa-text-primary">{recommendations.length}</p>
          <p class="text-xs text-ifa-text-secondary">Courses Recommended</p>
        </div>
      </div>
    </div>
    <div class="bg-ifa-card rounded-xl p-5 border border-ifa-border shadow-card">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-ifa-accent-purple/10 flex items-center justify-center">
          <TrendingUp class="w-5 h-5 text-ifa-accent-purple" />
        </div>
        <div>
          <p class="text-2xl font-bold text-ifa-text-primary">{myCourses.length}</p>
          <p class="text-xs text-ifa-text-secondary">Active Courses</p>
        </div>
      </div>
    </div>
  </div>

  <!-- Recommended Courses -->
  <div class="space-y-4">
    <h2 class="text-xl font-semibold text-ifa-text-primary">Recommended for You</h2>

    {#if loading}
      <div class="flex items-center gap-2 text-sm text-ifa-text-secondary py-8">
        <Loader2 class="w-4 h-4 animate-spin" />
        <span>Loading recommendations…</span>
      </div>
    {:else if recommendations.length === 0}
      <div class="bg-ifa-card rounded-xl p-8 border border-ifa-border text-center">
        <p class="text-sm text-ifa-text-secondary">
          No recommendations yet. Complete a lesson or quiz and IFA will tailor suggestions for you.
        </p>
      </div>
    {:else}
      {#each recommendations as rec}
        <button
          type="button"
          on:click={() => openRecommendation(rec)}
          class="w-full text-left bg-ifa-card rounded-xl p-6 border border-ifa-border shadow-card hover:shadow-elevated transition-shadow"
        >
          <div class="flex gap-6">
            <div class="w-20 h-20 rounded-lg bg-ifa-pine flex items-center justify-center text-white text-2xl font-bold shrink-0">
              {rec.title.charAt(0)}
            </div>
            <div class="flex-1">
              <div class="flex items-start justify-between gap-4">
                <div>
                  <h3 class="text-lg font-semibold text-ifa-text-primary">{rec.title}</h3>
                  <p class="text-sm text-ifa-text-secondary mt-1">{rec.reason}</p>
                  <div class="flex items-center gap-4 mt-3 text-xs text-ifa-text-muted">
                    <span class="flex items-center gap-1">
                      <Clock class="w-3 h-3" />
                      {rec.estimatedHours}h
                    </span>
                    <span class="px-2 py-1 rounded-full bg-ifa-accent-green/10 text-ifa-accent-green font-medium">
                      {rec.confidenceScore <= 1 ? Math.round(rec.confidenceScore * 100) : Math.round(rec.confidenceScore)}% Match
                    </span>
                    <span class="px-2 py-1 rounded-full bg-ifa-card-muted border border-ifa-border">{rec.category}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </button>
      {/each}
    {/if}
  </div>

  <!-- Learning Path (from real enrollments) -->
  {#if myCourses.length > 0}
    <div class="bg-gradient-to-r from-ifa-pine to-ifa-pine-dark rounded-xl p-6 text-white">
      <h2 class="text-xl font-semibold mb-4">Your Learning Path</h2>
      <div class="space-y-3">
        {#each myCourses.slice(0, 4) as course, i}
          <button
            type="button"
            on:click={() => goto(`/courses/${course.id}`)}
            class="w-full flex items-center gap-3 text-left"
          >
            <div class="w-8 h-8 rounded-full bg-emerald-400 flex items-center justify-center text-ifa-pine font-bold text-sm shrink-0">
              {i + 1}
            </div>
            <div>
              <p class="font-medium">{course.title}</p>
              <p class="text-sm text-emerald-100">
                {course.progressPercentage >= 100 ? 'Completed' : `${course.progressPercentage}% complete`}
              </p>
            </div>
          </button>
        {/each}
      </div>
    </div>
  {/if}
</div>
