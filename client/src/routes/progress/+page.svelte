<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { TrendingUp, Calendar, Award, Flame, Clock, Target, BookOpen, Loader2 } from 'lucide-svelte';
  import ProgressOverview from '$lib/components/ProgressOverview.svelte';
  import SkillProgressChart from '$lib/components/SkillProgressChart.svelte';
  import AssessmentHistory from '$lib/components/AssessmentHistory.svelte';
  import LearningActivityChart from '$lib/components/LearningActivityChart.svelte';
  import { progressApi, type LearnerProgressDto } from '$lib/api';
  import { initSession } from '$lib/stores/sessionStore';

  let loading = true;
  let failed = false;

  // Real, per-learner values — populated from GET /api/progress on mount.
  let overallStats = {
    totalCourses: 0,
    completedCourses: 0,
    inProgressCourses: 0,
    overallProgress: 0,
    totalStudyTime: '0h 0m',
    weeklyStudyTime: '0h 0m',
    learningStreak: 0
  };

  let skillProgress: { name: string; percentage: number; improvement: string; color: string }[] = [];
  let assessmentHistory: any[] = [];
  let weeklyActivity: { day: string; hours: number }[] = [];
  let weakAreas: { skill: string; currentLevel: number; targetLevel: number }[] = [];
  let avgQuizScore = 0;
  let totalQuestions = 0;

  const WEEKDAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  function relativeTime(iso: string): string {
    if (!iso) return '';
    const then = new Date(iso).getTime();
    if (Number.isNaN(then)) return '';
    const minutes = Math.round((Date.now() - then) / 60000);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.round(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.round(hours / 24);
    if (days < 30) return `${days}d ago`;
    return new Date(iso).toLocaleDateString();
  }

  function applyProgress(data: LearnerProgressDto) {
    overallStats = data.stats;

    skillProgress = data.skillProgress.map((s) => ({
      name: s.name,
      percentage: s.percentage,
      improvement: s.improvement,
      color: s.color
    }));

    assessmentHistory = data.assessments.map((a) => ({
      id: a.id,
      type: a.type,
      title: a.title,
      course: a.course,
      score: a.score,
      date: relativeTime(a.submittedAt),
      duration: a.attemptNumber > 1 ? `Attempt ${a.attemptNumber}` : 'First attempt'
    }));

    // Backend sends the last 7 calendar days in order; keep them as-is.
    weeklyActivity =
      data.weeklyActivity.length > 0
        ? data.weeklyActivity.map((d) => ({ day: d.day, hours: d.hours }))
        : WEEKDAYS.map((day) => ({ day, hours: 0 }));

    weakAreas = data.weakAreas ?? [];
    avgQuizScore = data.avgQuizScore ?? 0;
    totalQuestions = data.totalQuestions ?? 0;
  }

  onMount(async () => {
    try {
      await initSession();
      const data = await progressApi.get();
      applyProgress(data);
    } catch {
      failed = true;
    } finally {
      loading = false;
    }
  });

  function handleViewDetails(courseId: string) {
    if (courseId) void goto(`/courses/${courseId}`);
    else void goto('/courses');
  }

  function handlePracticeSkill(skill: string) {
    void goto(`/ai-tutor?topic=${encodeURIComponent(skill)}`);
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Progress</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Track your learning journey and achievements</p>
    </div>
    <div class="flex items-center gap-2">
      {#if loading}
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm">
          <Loader2 class="w-4 h-4 animate-spin" />
          <span>Loading…</span>
        </div>
      {:else if overallStats.learningStreak > 0}
        <div class="flex items-center gap-1 text-emerald-600 text-sm">
          <Flame class="w-4 h-4" />
          <span>{overallStats.learningStreak}-day streak</span>
        </div>
      {/if}
    </div>
  </div>

  {#if failed}
    <p role="alert" class="rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-sm text-amber-700">
      Couldn't load your progress right now. Connect to the IFA API and refresh to see your live data.
    </p>
  {/if}

  <!-- Progress Overview -->
  <ProgressOverview stats={overallStats} />

  {#if !loading && skillProgress.length === 0 && assessmentHistory.length === 0}
    <div class="bg-ifa-card rounded-2xl border border-ifa-border p-10 text-center">
      <Target class="w-8 h-8 text-ifa-text-muted mx-auto mb-3" />
      <h2 class="text-lg font-bold text-ifa-text-primary">Nothing to show yet</h2>
      <p class="text-sm text-ifa-text-secondary mt-1">
        Complete lessons and quizzes and your skills, activity and assessments will appear here.
      </p>
      <button
        type="button"
        on:click={() => goto('/courses')}
        class="mt-4 px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold hover:bg-emerald-800 transition"
      >
        Go to my courses
      </button>
    </div>
  {:else}
    <!-- Grid Layout -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Skill Progress Chart -->
      <SkillProgressChart skills={skillProgress} />

      <!-- Learning Activity Chart -->
      <LearningActivityChart activity={weeklyActivity} />
    </div>

    <!-- Assessment History -->
    <div>
      <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
        <Award class="w-5 h-5 text-ifa-pine" />
        Assessment History
      </h2>
      {#if assessmentHistory.length > 0}
        <AssessmentHistory
          assessments={assessmentHistory}
          onViewDetails={(id) => {
            const match = assessmentHistory.find((a) => a.id === id);
            handleViewDetails(match?.courseId ?? '');
          }}
        />
      {:else}
        <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6 text-center">
          <p class="text-sm text-ifa-text-secondary">No assessments yet — take a quiz to see your results here.</p>
        </div>
      {/if}
    </div>

    <!-- Weak Areas Focus -->
    {#if weakAreas.length > 0}
      <div>
        <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
          <Target class="w-5 h-5 text-ifa-accent-purple" />
          Areas to Focus On
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          {#each weakAreas as area}
            <div class="bg-ifa-card rounded-xl border border-ifa-border p-5">
              <div class="flex items-center justify-between mb-3">
                <h3 class="text-sm font-bold text-ifa-text-primary">{area.skill}</h3>
                <span class="text-xs font-semibold text-red-600 bg-red-50 px-2 py-1 rounded-full">
                  Needs Improvement
                </span>
              </div>
              <div class="flex items-center gap-4 mb-3">
                <div class="flex-1">
                  <div class="flex items-center justify-between text-xs mb-1">
                    <span class="text-ifa-text-secondary">Current: {area.currentLevel}%</span>
                    <span class="text-ifa-text-secondary">Target: {area.targetLevel}%</span>
                  </div>
                  <div class="flex gap-1">
                    <div class="flex-1 h-2 bg-red-200 rounded-full">
                      <div class="h-full bg-red-600 rounded-full" style="width: {area.currentLevel}%"></div>
                    </div>
                    <div class="flex-1 h-2 bg-emerald-200 rounded-full">
                      <div class="h-full bg-emerald-600 rounded-full" style="width: {area.targetLevel}%"></div>
                    </div>
                  </div>
                </div>
              </div>
              <button
                type="button"
                on:click={() => handlePracticeSkill(area.skill)}
                class="w-full py-2 bg-ifa-pine text-white rounded-lg text-xs font-semibold hover:bg-emerald-800 transition"
              >
                Practice {area.skill}
              </button>
            </div>
          {/each}
        </div>
      </div>
    {/if}

    <!-- Recent Achievements -->
    <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
      <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
        <Award class="w-5 h-5 text-yellow-500" />
        Recent Achievements
      </h2>
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="flex items-center gap-3 bg-ifa-card-muted rounded-lg p-4">
          <div class="w-10 h-10 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-600">
            <BookOpen class="w-5 h-5" />
          </div>
          <div>
            <h3 class="text-sm font-semibold text-ifa-text-primary">
              {overallStats.inProgressCourses} course{overallStats.inProgressCourses === 1 ? '' : 's'} in progress
            </h3>
            <p class="text-xs text-ifa-text-secondary">{overallStats.completedCourses} completed</p>
          </div>
        </div>
        <div class="flex items-center gap-3 bg-ifa-card-muted rounded-lg p-4">
          <div class="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center text-blue-600">
            <Flame class="w-5 h-5" />
          </div>
          <div>
            <h3 class="text-sm font-semibold text-ifa-text-primary">
              {overallStats.learningStreak}-Day Learning Streak
            </h3>
            <p class="text-xs text-ifa-text-secondary">
              {overallStats.learningStreak > 0 ? 'Keep it going!' : 'Start learning today'}
            </p>
          </div>
        </div>
        <div class="flex items-center gap-3 bg-ifa-card-muted rounded-lg p-4">
          <div class="w-10 h-10 rounded-full bg-purple-100 flex items-center justify-center text-purple-600">
            <Target class="w-5 h-5" />
          </div>
          <div>
            <h3 class="text-sm font-semibold text-ifa-text-primary">
              {avgQuizScore}% average quiz score
            </h3>
            <p class="text-xs text-ifa-text-secondary">{assessmentHistory.length} assessment{assessmentHistory.length === 1 ? '' : 's'} taken</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Time Statistics -->
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
      <div class="bg-ifa-card rounded-xl border border-ifa-border p-5">
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
          <Clock class="w-4 h-4" />
          <span>Total Study Time</span>
        </div>
        <p class="text-2xl font-bold text-ifa-pine">{overallStats.totalStudyTime}</p>
      </div>
      <div class="bg-ifa-card rounded-xl border border-ifa-border p-5">
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
          <Calendar class="w-4 h-4" />
          <span>This Week</span>
        </div>
        <p class="text-2xl font-bold text-ifa-pine">{overallStats.weeklyStudyTime}</p>
      </div>
      <div class="bg-ifa-card rounded-xl border border-ifa-border p-5">
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
          <BookOpen class="w-4 h-4" />
          <span>Courses Enrolled</span>
        </div>
        <p class="text-2xl font-bold text-ifa-pine">{overallStats.totalCourses}</p>
      </div>
      <div class="bg-ifa-card rounded-xl border border-ifa-border p-5">
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
          <TrendingUp class="w-4 h-4" />
          <span>Avg. Quiz Score</span>
        </div>
        <p class="text-2xl font-bold text-ifa-pine">{avgQuizScore}%</p>
      </div>
    </div>
  {/if}
</div>
