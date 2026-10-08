<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { ArrowRight, TrendingUp, Award, Target, Flame, BookOpen, Loader2 } from 'lucide-svelte';
  import SkillBreakdown from '$lib/components/SkillBreakdown.svelte';
  import WeakSkillCard from '$lib/components/WeakSkillCard.svelte';
  import { coursesApi, skillsApi, type LearnerSkillsResultDto } from '$lib/api';
  import { initSession, getLearnerId } from '$lib/stores/sessionStore';

  interface UiSkill {
    name: string;
    percentage: number;
    color: string;
    improvement: string;
  }
  interface UiCategory {
    name: string;
    skills: UiSkill[];
  }
  interface UiWeakArea {
    skill: string;
    currentLevel: number;
    targetLevel: number;
    recommendedAction: string;
    reason: string;
    priority: string;
  }
  interface UiRelatedCourse {
    title: string;
    progress: number;
  }

  // Real per-learner data, hydrated from the API on mount. No shared demo
  // skills — every learner sees their own metrics (or an empty state).
  let skillCategories: UiCategory[] = [];
  let weakAreas: UiWeakArea[] = [];
  let recentImprovements: { skill: string; improvement: string; timeAgo: string }[] = [];
  let relatedCourses: UiRelatedCourse[] = [];
  let overallImprovement = 0;
  let loading = true;
  let failed = false;

  function mapCategories(result: LearnerSkillsResultDto): UiCategory[] {
    return result.categories.map((category) => ({
      name: category.name,
      skills: category.skills.map((skill) => ({
        name: skill.name,
        percentage: skill.percentage,
        color: skill.color,
        improvement: skill.improvement
      }))
    }));
  }

  function deriveWeakAreas(categories: UiCategory[]): UiWeakArea[] {
    return categories
      .flatMap((category) => category.skills)
      .filter((skill) => skill.percentage < 50)
      .map((skill) => ({
        skill: skill.name,
        currentLevel: skill.percentage,
        targetLevel: Math.min(100, skill.percentage + 30),
        recommendedAction: `Practice ${skill.name}`,
        reason: 'Below the 50% mastery threshold in recent assessments',
        priority: skill.percentage < 35 ? 'HIGH' : 'MEDIUM'
      }));
  }

  function deriveImprovements(categories: UiCategory[]) {
    return categories
      .flatMap((category) => category.skills)
      .filter((skill) => parseFloat(skill.improvement) > 0)
      .sort((a, b) => parseFloat(b.improvement) - parseFloat(a.improvement))
      .slice(0, 3)
      .map((skill) => ({ skill: skill.name, improvement: skill.improvement, timeAgo: 'This week' }));
  }

  onMount(async () => {
    try {
      await initSession();
      const learnerId = getLearnerId();

      const [skills, courses] = await Promise.all([
        learnerId ? skillsApi.forLearner(learnerId).catch(() => null) : Promise.resolve(null),
        coursesApi.listMine().catch(() => [])
      ]);

      if (skills) {
        skillCategories = mapCategories(skills);
        overallImprovement = skills.overallImprovement ?? 0;
        weakAreas = deriveWeakAreas(skillCategories);
        recentImprovements = deriveImprovements(skillCategories);
      }

      if (courses.length > 0) {
        relatedCourses = courses
          .slice(0, 4)
          .map((course) => ({ title: course.title, progress: course.progressPercentage }));
      }
    } catch {
      failed = true;
    } finally {
      loading = false;
    }
  });

  function handlePracticeSkill(skill: string) {
    void goto(`/ai-tutor?topic=${encodeURIComponent(skill)}`);
  }

  function handleViewCourse() {
    void goto('/courses');
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">My Skills</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Track your skill development and identify areas for improvement</p>
    </div>
    <div class="flex items-center gap-2">
      {#if loading}
        <div class="flex items-center gap-2 text-ifa-text-secondary text-sm">
          <Loader2 class="w-4 h-4 animate-spin" />
          <span>Loading…</span>
        </div>
      {:else if overallImprovement !== 0}
        <div class="flex items-center gap-1 text-ifa-pine text-sm">
          <TrendingUp class="w-4 h-4" />
          <span>{overallImprovement >= 0 ? '+' : ''}{overallImprovement}% overall improvement</span>
        </div>
      {/if}
    </div>
  </div>

  {#if failed}
    <p role="alert" class="rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-sm text-amber-700">
      Couldn't load your skills right now. Connect to the IFA API and refresh to see your live skill profile.
    </p>
  {/if}

  {#if loading}
    <div class="bg-ifa-card rounded-2xl border border-ifa-border p-10 text-center text-ifa-text-secondary text-sm">
      Loading your skill profile…
    </div>
  {:else if skillCategories.length === 0}
    <!-- Empty state: no fabricated, shared skill data. -->
    <div class="bg-ifa-card rounded-2xl border border-ifa-border p-10 text-center">
      <Target class="w-8 h-8 text-ifa-text-muted mx-auto mb-3" />
      <h2 class="text-lg font-bold text-ifa-text-primary">No skill data yet</h2>
      <p class="text-sm text-ifa-text-secondary mt-1 max-w-md mx-auto">
        Your skills are built from your own quiz and assessment performance. Complete a few lessons and
        quizzes and your skill profile will appear here.
      </p>
      <button
        type="button"
        on:click={handleViewCourse}
        class="mt-4 px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold hover:bg-emerald-800 transition"
      >
        Go to my courses
      </button>
    </div>
  {:else}
    <!-- Skill Categories Breakdown (real, per-learner metrics) -->
    <div class="space-y-6">
      {#each skillCategories as category}
        <SkillBreakdown categoryName={category.name} skills={category.skills} />
      {/each}
    </div>

    <!-- Weak Areas -->
    {#if weakAreas.length > 0}
      <div>
        <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
          <Target class="w-5 h-5 text-ifa-accent-purple" />
          Areas to Focus On
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          {#each weakAreas as area}
            <WeakSkillCard weakArea={area} onPractice={() => handlePracticeSkill(area.skill)} />
          {/each}
        </div>
      </div>
    {/if}

    <!-- Recent Improvements -->
    {#if recentImprovements.length > 0}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
        <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
          <Flame class="w-5 h-5 text-orange-500" />
          Recent Improvements
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          {#each recentImprovements as improvement}
            <div class="bg-ifa-card-muted rounded-xl p-4">
              <div class="flex items-center justify-between mb-2">
                <span class="text-sm font-semibold text-ifa-text-primary">{improvement.skill}</span>
                <span class="text-sm font-bold text-emerald-600">{improvement.improvement}</span>
              </div>
              <p class="text-xs text-ifa-text-muted">{improvement.timeAgo}</p>
            </div>
          {/each}
        </div>
      </div>
    {/if}

    <!-- Related Courses -->
    {#if relatedCourses.length > 0}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
        <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
          <BookOpen class="w-5 h-5 text-ifa-pine" />
          Recommended Courses
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          {#each relatedCourses as course}
            <div class="bg-ifa-card-muted rounded-xl p-4 flex items-center justify-between">
              <div class="min-w-0 flex-1">
                <h3 class="text-sm font-semibold text-ifa-text-primary truncate">{course.title}</h3>
                <div class="flex items-center gap-2 mt-2">
                  <div class="flex-1 h-2 bg-white rounded-full">
                    <div class="h-full bg-ifa-pine rounded-full" style="width: {course.progress}%"></div>
                  </div>
                  <span class="text-xs font-semibold text-ifa-pine">{course.progress}%</span>
                </div>
              </div>
              <button
                type="button"
                on:click={handleViewCourse}
                class="ml-3 px-4 py-2 bg-ifa-pine text-white rounded-lg text-xs font-semibold flex items-center gap-1.5 hover:bg-emerald-800 transition"
              >
                <span>Continue</span>
                <ArrowRight class="w-3 h-3" />
              </button>
            </div>
          {/each}
        </div>
      </div>
    {/if}

    <!-- Skill History -->
    <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
      <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
        <Award class="w-5 h-5 text-yellow-500" />
        Skill History
      </h2>
      <div class="text-center py-8">
        <p class="text-ifa-text-secondary text-sm">Detailed skill history and analytics coming soon</p>
      </div>
    </div>
  {/if}
</div>
