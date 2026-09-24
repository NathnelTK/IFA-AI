<script lang="ts">
  import { TrendingUp, Calendar, Award, Flame, Clock, Target, BookOpen } from 'lucide-svelte';
  import ProgressOverview from '$lib/components/ProgressOverview.svelte';
  import SkillProgressChart from '$lib/components/SkillProgressChart.svelte';
  import AssessmentHistory from '$lib/components/AssessmentHistory.svelte';
  import LearningActivityChart from '$lib/components/LearningActivityChart.svelte';

  const overallStats = {
    totalCourses: 4,
    completedCourses: 1,
    inProgressCourses: 2,
    overallProgress: 67,
    totalStudyTime: '48h 30m',
    weeklyStudyTime: '12h 30m',
    learningStreak: 7
  };

  const skillProgress = [
    { name: 'C#', percentage: 84, improvement: '+12%' },
    { name: 'Databases', percentage: 61, improvement: '+8%' },
    { name: 'APIs', percentage: 55, improvement: '+5%' },
    { name: 'Authentication', percentage: 45, improvement: '+3%' },
    { name: 'Testing', percentage: 32, improvement: '+15%' }
  ];

  const assessmentHistory = [
    {
      id: 'quiz-1',
      type: 'quiz',
      title: 'C# Fundamentals Quiz',
      course: 'C# Backend Development',
      score: 85,
      date: '2 hours ago',
      duration: '15 min'
    },
    {
      id: 'test-1',
      type: 'test',
      title: 'Module 1 Test',
      course: 'C# Backend Development',
      score: 78,
      date: '1 day ago',
      duration: '30 min'
    },
    {
      id: 'quiz-2',
      type: 'quiz',
      title: 'SQL Basics Quiz',
      course: 'SQL for Developers',
      score: 92,
      date: '3 days ago',
      duration: '12 min'
    }
  ];

  const weeklyActivity = [
    { day: 'Mon', hours: 2.5 },
    { day: 'Tue', hours: 1.8 },
    { day: 'Wed', hours: 3.2 },
    { day: 'Thu', hours: 1.5 },
    { day: 'Fri', hours: 2.0 },
    { day: 'Sat', hours: 0.8 },
    { day: 'Sun', hours: 0.5 }
  ];

  const weakAreas = [
    { skill: 'Testing', currentLevel: 32, targetLevel: 70 },
    { skill: 'Authentication', currentLevel: 45, targetLevel: 75 }
  ];

  function handleViewDetails(id: string) {
    console.log('Viewing details for:', id);
  }

  function handlePracticeSkill(skill: string) {
    console.log('Practicing skill:', skill);
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
      <div class="flex items-center gap-1 text-emerald-600 text-sm">
        <Flame class="w-4 h-4" />
        <span>7-day streak</span>
      </div>
    </div>
  </div>

  <!-- Progress Overview -->
  <ProgressOverview stats={overallStats} />

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
    <AssessmentHistory
      assessments={assessmentHistory}
      onViewDetails={handleViewDetails}
    />
  </div>

  <!-- Weak Areas Focus -->
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

  <!-- Recent Achievements -->
  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
    <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
      <Award class="w-5 h-5 text-yellow-500" />
      Recent Achievements
    </h2>
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="flex items-center gap-3 bg-ifa-card-muted rounded-lg p-4">
        <div class="w-10 h-10 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-600">
          <Award class="w-5 h-5" />
        </div>
        <div>
          <h3 class="text-sm font-semibold text-ifa-text-primary">Completed C# Module 1</h3>
          <p class="text-xs text-ifa-text-secondary">2 hours ago</p>
        </div>
      </div>
      <div class="flex items-center gap-3 bg-ifa-card-muted rounded-lg p-4">
        <div class="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center text-blue-600">
          <Flame class="w-5 h-5" />
        </div>
        <div>
          <h3 class="text-sm font-semibold text-ifa-text-primary">7-Day Learning Streak</h3>
          <p class="text-xs text-ifa-text-secondary">Today</p>
        </div>
      </div>
      <div class="flex items-center gap-3 bg-ifa-card-muted rounded-lg p-4">
        <div class="w-10 h-10 rounded-full bg-purple-100 flex items-center justify-center text-purple-600">
          <Target class="w-5 h-5" />
        </div>
        <div>
          <h3 class="text-sm font-semibold text-ifa-text-primary">Skill Improvement +15%</h3>
          <p class="text-xs text-ifa-text-secondary">This week</p>
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
        <span>Modules Completed</span>
      </div>
      <p class="text-2xl font-bold text-ifa-pine">8</p>
    </div>
    <div class="bg-ifa-card rounded-xl border border-ifa-border p-5">
      <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
        <TrendingUp class="w-4 h-4" />
        <span>Avg. Quiz Score</span>
      </div>
      <p class="text-2xl font-bold text-ifa-pine">85%</p>
    </div>
  </div>
</div>
