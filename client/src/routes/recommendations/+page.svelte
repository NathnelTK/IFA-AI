<script lang="ts">
  import { demoCourses } from '$lib/data/catalog';
  import { Lightbulb, Star, Clock, Users, BookOpen, TrendingUp } from 'lucide-svelte';

  // Personalized recommendations based on course data
  const recommendedCourses = [
    {
      ...demoCourses[0],
      reason: 'Based on your progress in Backend Development',
      matchScore: 95
    },
    {
      ...demoCourses[1],
      reason: 'Complementary to your C# skills',
      matchScore: 88
    },
    {
      ...demoCourses[3],
      reason: 'Next step in your learning path',
      matchScore: 92
    }
  ];
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

  <!-- Stats Cards -->
  <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
    <div class="bg-ifa-card rounded-xl p-5 border border-ifa-border shadow-card">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-ifa-accent-green/10 flex items-center justify-center">
          <Star class="w-5 h-5 text-ifa-accent-green" />
        </div>
        <div>
          <p class="text-2xl font-bold text-ifa-text-primary">95%</p>
          <p class="text-xs text-ifa-text-secondary">Match Accuracy</p>
        </div>
      </div>
    </div>
    <div class="bg-ifa-card rounded-xl p-5 border border-ifa-border shadow-card">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-ifa-accent-blue/10 flex items-center justify-center">
          <BookOpen class="w-5 h-5 text-ifa-accent-blue" />
        </div>
        <div>
          <p class="text-2xl font-bold text-ifa-text-primary">12</p>
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
          <p class="text-2xl font-bold text-ifa-text-primary">+40%</p>
          <p class="text-xs text-ifa-text-secondary">Faster Completion</p>
        </div>
      </div>
    </div>
  </div>

  <!-- Recommended Courses -->
  <div class="space-y-4">
    <h2 class="text-xl font-semibold text-ifa-text-primary">Recommended for You</h2>
    {#each recommendedCourses as course}
      <div class="bg-ifa-card rounded-xl p-6 border border-ifa-border shadow-card hover:shadow-elevated transition-shadow">
        <div class="flex gap-6">
          <div class="w-20 h-20 rounded-lg bg-ifa-pine flex items-center justify-center text-white text-2xl font-bold shrink-0">
            {course.thumbnail}
          </div>
          <div class="flex-1">
            <div class="flex items-start justify-between">
              <div>
                <h3 class="text-lg font-semibold text-ifa-text-primary">{course.title}</h3>
                <p class="text-sm text-ifa-text-secondary mt-1">{course.description}</p>
                <div class="flex items-center gap-4 mt-3 text-xs text-ifa-text-muted">
                  <span class="flex items-center gap-1">
                    <Clock class="w-3 h-3" />
                    {course.duration}
                  </span>
                  <span class="flex items-center gap-1">
                    <Users class="w-3 h-3" />
                    {course.provider}
                  </span>
                  <span class="px-2 py-1 rounded-full bg-ifa-accent-green/10 text-ifa-accent-green font-medium">
                    {course.matchScore}% Match
                  </span>
                </div>
              </div>
              <div class="text-right">
                <p class="text-sm text-ifa-text-secondary font-medium">{course.reason}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    {/each}
  </div>

  <!-- Learning Path Suggestions -->
  <div class="bg-gradient-to-r from-ifa-pine to-ifa-pine-dark rounded-xl p-6 text-white">
    <h2 class="text-xl font-semibold mb-4">Your Learning Path</h2>
    <div class="space-y-3">
      <div class="flex items-center gap-3">
        <div class="w-8 h-8 rounded-full bg-emerald-400 flex items-center justify-center text-ifa-pine font-bold text-sm">1</div>
        <div>
          <p class="font-medium">C# Backend Development</p>
          <p class="text-sm text-emerald-200">In Progress - 45% Complete</p>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <div class="w-8 h-8 rounded-full bg-emerald-400/30 flex items-center justify-center text-emerald-300 font-bold text-sm">2</div>
        <div>
          <p class="font-medium">ASP.NET Core Fundamentals</p>
          <p class="text-sm text-emerald-200">Recommended Next</p>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <div class="w-8 h-8 rounded-full bg-emerald-400/20 flex items-center justify-center text-emerald-300 font-bold text-sm">3</div>
        <div>
          <p class="font-medium">SQL for Developers</p>
          <p class="text-sm text-emerald-200">Future Path</p>
        </div>
      </div>
    </div>
  </div>
</div>