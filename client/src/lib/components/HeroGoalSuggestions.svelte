<script lang="ts">
  import { Target, ArrowRight, Zap, BookOpen, Code2, Database } from 'lucide-svelte';

  export let onSelectGoal: (goal: string) => void = () => {};

  const suggestedGoals = [
    {
      id: 'goal-1',
      title: 'Master C# Backend Development',
      description: 'Complete the C# Backend Development course and build real-world APIs',
      estimatedTime: '4 weeks',
      difficulty: 'Intermediate',
      icon: Code2,
      color: '#2A9D68'
    },
    {
      id: 'goal-2',
      title: 'Learn Database Optimization',
      description: 'Master SQL query optimization and database design patterns',
      estimatedTime: '3 weeks',
      difficulty: 'Advanced',
      icon: Database,
      color: '#E07A5F'
    },
    {
      id: 'goal-3',
      title: 'Build REST API Portfolio',
      description: 'Create 3 production-ready REST APIs for your portfolio',
      estimatedTime: '6 weeks',
      difficulty: 'Intermediate',
      icon: BookOpen,
      color: '#7C5CFC'
    }
  ];

  function handleSelectGoal(goal: { title: string }) {
    onSelectGoal(goal.title);
  }

  function getDifficultyColor(difficulty: string) {
    switch (difficulty) {
      case 'Beginner':
        return 'text-emerald-600 bg-emerald-50';
      case 'Intermediate':
        return 'text-blue-600 bg-blue-50';
      case 'Advanced':
        return 'text-orange-600 bg-orange-50';
      default:
        return 'text-gray-600 bg-gray-50';
    }
  }
</script>

<div class="bg-ifa-bg-warm rounded-2xl border border-ifa-pine/20 p-6">
  <div class="flex items-center gap-2 mb-4">
    <div class="w-8 h-8 rounded-lg bg-ifa-pine/20 flex items-center justify-center text-ifa-pine">
      <Target class="w-4 h-4" />
    </div>
    <h3 class="text-sm font-bold text-ifa-pine">IFA Recommended Goals</h3>
  </div>

  <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
    {#each suggestedGoals as goal}
      <button type="button" class="text-left bg-ifa-card rounded-xl border border-ifa-border p-4 hover:shadow-elevated transition cursor-pointer" on:click={() => handleSelectGoal(goal)}>
        <div class="flex items-start justify-between mb-3">
          <div
            class="w-10 h-10 rounded-lg flex items-center justify-center"
            style="background-color: {goal.color}15; color: {goal.color};"
          >
            {#if goal.icon === Code2}
              <Code2 class="w-5 h-5" />
            {:else if goal.icon === Database}
              <Database class="w-5 h-5" />
            {:else if goal.icon === BookOpen}
              <BookOpen class="w-5 h-5" />
            {:else}
              <Target class="w-5 h-5" />
            {/if}
          </div>
          <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full {getDifficultyColor(goal.difficulty)}">
            {goal.difficulty}
          </span>
        </div>

        <h4 class="text-sm font-bold text-ifa-text-primary mb-1">{goal.title}</h4>
        <p class="text-xs text-ifa-text-secondary mb-3 line-clamp-2">{goal.description}</p>

        <div class="flex items-center justify-between">
          <div class="flex items-center gap-1 text-xs text-ifa-text-muted">
            <Zap class="w-3 h-3" />
            <span>{goal.estimatedTime}</span>
          </div>
          <ArrowRight class="w-4 h-4 text-ifa-pine" />
        </div>
      </button>
    {/each}
  </div>

  <div class="mt-4 text-center">
    <button
      type="button"
      on:click={() => onSelectGoal('Explore all recommended goals')}
      class="text-xs font-semibold text-ifa-pine hover:text-ifa-pine-dark transition"
    >
      View all goal suggestions →
    </button>
  </div>
</div>
