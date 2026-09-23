<script lang="ts">
  import { ArrowRight, TrendingUp, Award, Target, Flame, BookOpen } from 'lucide-svelte';
  import SkillRadar from '$lib/components/SkillRadar.svelte';
  import SkillBreakdown from '$lib/components/SkillBreakdown.svelte';
  import WeakSkillCard from '$lib/components/WeakSkillCard.svelte';

  const skillCategories = [
    {
      name: 'Backend Development',
      skills: [
        { name: 'C#', percentage: 84, color: '#2A9D68', improvement: '+12%' },
        { name: 'Databases', percentage: 61, color: '#E07A5F', improvement: '+8%' },
        { name: 'APIs', percentage: 55, color: '#7C5CFC', improvement: '+5%' },
        { name: 'Authentication', percentage: 45, color: '#E11D48', improvement: '+3%' },
        { name: 'Testing', percentage: 32, color: '#EF4444', improvement: '+15%' }
      ]
    },
    {
      name: 'Frontend Development',
      skills: [
        { name: 'JavaScript', percentage: 72, color: '#F59E0B', improvement: '+10%' },
        { name: 'React', percentage: 68, color: '#3B82F6', improvement: '+7%' },
        { name: 'CSS/Tailwind', percentage: 75, color: '#10B981', improvement: '+9%' }
      ]
    }
  ];

  const weakAreas = [
    {
      skill: 'Testing',
      currentLevel: 32,
      targetLevel: 70,
      recommendedAction: 'Practice Unit Testing',
      reason: 'Low quiz performance in recent assessments',
      priority: 'HIGH'
    },
    {
      skill: 'Authentication',
      currentLevel: 45,
      targetLevel: 75,
      recommendedAction: 'Review JWT Authentication',
      reason: 'Struggled with OAuth flows',
      priority: 'MEDIUM'
    }
  ];

  const recentImprovements = [
    { skill: 'C#', improvement: '+12%', timeAgo: 'This week' },
    { skill: 'Testing', improvement: '+15%', timeAgo: 'This week' },
    { skill: 'APIs', improvement: '+5%', timeAgo: 'Last week' }
  ];

  const relatedCourses = [
    { title: 'Advanced C# Patterns', progress: 45 },
    { title: 'Unit Testing Best Practices', progress: 20 }
  ];

  function handlePracticeSkill(skill: string) {
    console.log('Starting practice for:', skill);
  }

  function handleViewCourse(courseTitle: string) {
    console.log('Viewing course:', courseTitle);
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
      <div class="flex items-center gap-1 text-ifa-pine text-sm">
        <TrendingUp class="w-4 h-4" />
        <span>+8% overall improvement</span>
      </div>
    </div>
  </div>

  <!-- Skill Radar Chart -->
  <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
    <SkillRadar skills={skillCategories[0].skills} title="Backend Development Skills" />
    <SkillRadar skills={skillCategories[1].skills} title="Frontend Development Skills" />
  </div>

  <!-- Skill Categories Breakdown -->
  <div class="space-y-6">
    {#each skillCategories as category}
      <SkillBreakdown
        categoryName={category.name}
        skills={category.skills}
      />
    {/each}
  </div>

  <!-- Weak Areas -->
  <div>
    <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
      <Target class="w-5 h-5 text-ifa-accent-purple" />
      Areas to Focus On
    </h2>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      {#each weakAreas as area}
        <WeakSkillCard
          weakArea={area}
          onPractice={() => handlePracticeSkill(area.skill)}
        />
      {/each}
    </div>
  </div>

  <!-- Recent Improvements -->
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

  <!-- Related Courses -->
  <div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
    <h2 class="text-lg font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
      <BookOpen class="w-5 h-5 text-ifa-pine" />
      Recommended Courses
    </h2>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      {#each relatedCourses as course}
        <div class="bg-ifa-card-muted rounded-xl p-4 flex items-center justify-between">
          <div>
            <h3 class="text-sm font-semibold text-ifa-text-primary">{course.title}</h3>
            <div class="flex items-center gap-2 mt-2">
              <div class="flex-1 h-2 bg-white rounded-full">
                <div class="h-full bg-ifa-pine rounded-full" style="width: {course.progress}%"></div>
              </div>
              <span class="text-xs font-semibold text-ifa-pine">{course.progress}%</span>
            </div>
          </div>
          <button
            type="button"
            on:click={() => handleViewCourse(course.title)}
            class="px-4 py-2 bg-ifa-pine text-white rounded-lg text-xs font-semibold flex items-center gap-1.5 hover:bg-emerald-800 transition"
          >
            <span>Continue</span>
            <ArrowRight class="w-3 h-3" />
          </button>
        </div>
      {/each}
    </div>
  </div>

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
</div>
