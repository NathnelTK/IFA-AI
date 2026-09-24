<script lang="ts">
  import { TrendingUp, BookOpen, Clock, Flame } from 'lucide-svelte';

  export let stats: any;
</script>

<div class="grid grid-cols-1 md:grid-cols-4 gap-4">
  <div class="bg-gradient-to-br from-ifa-pine to-emerald-700 rounded-2xl p-5 text-white">
    <div class="flex items-center gap-2 mb-2">
      <BookOpen class="w-5 h-5" />
      <span class="text-sm font-semibold">Overall Progress</span>
    </div>
    <p class="text-3xl font-black">{stats.overallProgress}%</p>
    <p class="text-xs text-emerald-100 mt-1">{stats.completedCourses} of {stats.totalCourses} courses completed</p>
  </div>

  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5">
    <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
      <Clock class="w-4 h-4" />
      <span>Total Study Time</span>
    </div>
    <p class="text-2xl font-bold text-ifa-pine">{stats.totalStudyTime}</p>
    <p class="text-xs text-ifa-text-muted mt-1">{stats.weeklyStudyTime} this week</p>
  </div>

  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5">
    <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
      <Flame class="w-4 h-4" />
      <span>Learning Streak</span>
    </div>
    <p class="text-2xl font-bold text-orange-500">{stats.learningStreak} days</p>
    <p class="text-xs text-ifa-text-muted mt-1">Keep it up!</p>
  </div>

  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-5">
    <div class="flex items-center gap-2 text-ifa-text-secondary text-sm mb-2">
      <TrendingUp class="w-4 h-4" />
      <span>In Progress</span>
    </div>
    <p class="text-2xl font-bold text-ifa-pine">{stats.inProgressCourses}</p>
    <p class="text-xs text-ifa-text-muted mt-1">Active courses</p>
  </div>
</div>
