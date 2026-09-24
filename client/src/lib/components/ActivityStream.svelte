<script lang="ts">
  import { BookOpen, Star, Trophy, Users, Zap, Clock } from 'lucide-svelte';

  const activities = [
    {
      id: 'act-1',
      type: 'enrollment',
      user: 'Ermiyas',
      action: 'enrolled in',
      target: 'C# Backend Development',
      time: '2 hours ago',
      icon: 'BookOpen',
      color: 'text-ifa-pine bg-emerald-100'
    },
    {
      id: 'act-2',
      type: 'completion',
      user: 'Negede',
      action: 'completed',
      target: 'SQL for Developers - Module 1',
      time: '5 hours ago',
      icon: 'Trophy',
      color: 'text-yellow-600 bg-yellow-100'
    },
    {
      id: 'act-3',
      type: 'achievement',
      user: 'Ermiyas',
      action: 'earned achievement',
      target: 'Fast Learner',
      time: '1 day ago',
      icon: 'Star',
      color: 'text-purple-600 bg-purple-100'
    },
    {
      id: 'act-4',
      type: 'recommendation',
      user: 'Negede',
      action: 'was recommended',
      target: 'Advanced Database Optimization',
      time: '2 days ago',
      icon: 'Zap',
      color: 'text-blue-600 bg-blue-100'
    },
    {
      id: 'act-5',
      type: 'enrollment',
      user: 'Nathnel',
      action: 'enrolled in',
      target: 'Python Fundamentals',
      time: '3 days ago',
      icon: 'BookOpen',
      color: 'text-ifa-pine bg-emerald-100'
    }
  ];
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h3 class="text-sm font-bold text-ifa-text-primary mb-4 flex items-center gap-2">
    <Users class="w-5 h-5 text-ifa-pine" />
    Activity Stream
  </h3>

  <div class="space-y-4">
    {#each activities as activity}
      <div class="flex items-start gap-3">
        <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 {activity.color}">
          {#if activity.icon === 'BookOpen'}
            <BookOpen class="w-4 h-4" />
          {:else if activity.icon === 'Trophy'}
            <Trophy class="w-4 h-4" />
          {:else if activity.icon === 'Star'}
            <Star class="w-4 h-4" />
          {:else if activity.icon === 'Zap'}
            <Zap class="w-4 h-4" />
          {:else}
            <Users class="w-4 h-4" />
          {/if}
        </div>

        <div class="flex-1 min-w-0">
          <p class="text-sm text-ifa-text-primary">
            <span class="font-semibold">{activity.user}</span>
            <span class="text-ifa-text-secondary">{activity.action}</span>
            <span class="font-semibold text-ifa-pine">{activity.target}</span>
          </p>
          <div class="flex items-center gap-1 text-xs text-ifa-text-muted mt-1">
            <Clock class="w-3 h-3" />
            <span>{activity.time}</span>
          </div>
        </div>
      </div>
    {/each}
  </div>

  <button
    type="button"
    class="w-full mt-4 py-2 text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition"
  >
    View all activity →
  </button>
</div>
