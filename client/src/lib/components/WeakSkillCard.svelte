<script lang="ts">
  import { AlertTriangle, ArrowRight, Target } from 'lucide-svelte';

  export let weakArea: any;
  export let onPractice = () => {};

  function getPriorityColor(priority: string) {
    switch (priority) {
      case 'HIGH':
        return 'text-red-600 bg-red-50 border-red-200';
      case 'MEDIUM':
        return 'text-orange-600 bg-orange-50 border-orange-200';
      default:
        return 'text-yellow-600 bg-yellow-50 border-yellow-200';
    }
  }
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 hover:shadow-elevated transition">
  <div class="flex items-start justify-between mb-3">
    <div class="flex items-center gap-2">
      <div class="w-8 h-8 rounded-lg bg-red-100 flex items-center justify-center text-red-600">
        <AlertTriangle class="w-4 h-4" />
      </div>
      <div>
        <h3 class="text-sm font-bold text-ifa-text-primary">{weakArea.skill}</h3>
        <p class="text-xs text-ifa-text-secondary">Weak Area</p>
      </div>
    </div>
    <div class="px-2 py-1 rounded-full text-[10px] font-bold border {getPriorityColor(weakArea.priority)}">
      {weakArea.priority}
    </div>
  </div>

  <!-- Progress comparison -->
  <div class="mb-3">
    <div class="flex items-center justify-between text-xs mb-1">
      <span class="text-ifa-text-secondary">Current: {weakArea.currentLevel}%</span>
      <span class="text-ifa-text-secondary">Target: {weakArea.targetLevel}%</span>
    </div>
    <div class="flex gap-1">
      <div class="flex-1 h-2 bg-red-200 rounded-full">
        <div class="h-full bg-red-600 rounded-full" style="width: {weakArea.currentLevel}%"></div>
      </div>
      <div class="flex-1 h-2 bg-emerald-200 rounded-full">
        <div class="h-full bg-emerald-600 rounded-full" style="width: {weakArea.targetLevel}%"></div>
      </div>
    </div>
  </div>

  <!-- Recommendation -->
  <div class="bg-ifa-card-muted rounded-lg p-3 mb-3">
    <div class="flex items-center gap-2 mb-1">
      <Target class="w-3 h-3 text-ifa-pine" />
      <span class="text-xs font-semibold text-ifa-text-primary">Recommended:</span>
    </div>
    <p class="text-sm font-bold text-ifa-pine">{weakArea.recommendedAction}</p>
    <p class="text-xs text-ifa-text-secondary mt-1">{weakArea.reason}</p>
  </div>

  <!-- Action -->
  <button
    type="button"
    on:click={onPractice}
    class="w-full py-2.5 bg-ifa-pine text-white rounded-xl text-xs font-semibold flex items-center justify-center gap-1.5 hover:bg-emerald-800 transition"
  >
    <span>Practice Now</span>
    <ArrowRight class="w-3 h-3" />
  </button>
</div>
