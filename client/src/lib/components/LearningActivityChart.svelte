<script lang="ts">
  import { BarChart3 } from 'lucide-svelte';

  export let activity: any[] = [];

  $: safeActivity = activity.map((day) => ({ ...day, hours: Number.isFinite(Number(day.hours)) ? Math.max(0, Number(day.hours)) : 0 }));
  $: maxHours = Math.max(1, ...safeActivity.map((day) => day.hours));
  $: weeklyTotal = safeActivity.reduce((sum, day) => sum + day.hours, 0);
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-6">
  <h3 class="text-sm font-bold text-ifa-text-primary mb-4">Weekly Learning Activity</h3>
  <div class="flex items-end justify-between h-40 gap-2">
    {#each safeActivity as day}
      <div class="flex-1 flex flex-col items-center gap-2">
        <div
          class="w-full bg-ifa-pine rounded-t transition-all duration-500"
          style="height: {(day.hours / maxHours) * 100}%"
          role="img"
          aria-label={`${day.day}: ${day.hours} hours`}
        ></div>
        <span class="text-xs text-ifa-text-secondary font-medium">{day.day}</span>
        <span class="text-xs font-bold text-ifa-pine">{day.hours}h</span>
      </div>
    {/each}
  </div>
  <div class="flex items-center justify-between mt-4 pt-4 border-t border-ifa-border">
    <div class="flex items-center gap-2 text-xs text-ifa-text-secondary">
      <BarChart3 class="w-4 h-4" />
      <span>Weekly total: {weeklyTotal}h</span>
    </div>
    <span class="text-xs text-ifa-text-muted font-semibold">{weeklyTotal > 0 ? 'Activity recorded' : 'No activity yet'}</span>
  </div>
</div>
