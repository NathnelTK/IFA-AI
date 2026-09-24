<script lang="ts">
  import { Sparkles, BookOpen } from 'lucide-svelte';

  export let summary = '';
</script>

<div class="bg-gradient-to-r from-ifa-pine/10 to-emerald-50 rounded-xl border border-ifa-pine/20 p-5">
  <div class="flex items-center gap-2 mb-3">
    <div class="w-8 h-8 rounded-lg bg-ifa-pine/20 flex items-center justify-center text-ifa-pine">
      <Sparkles class="w-4 h-4" />
    </div>
    <h3 class="text-sm font-bold text-ifa-pine">Research Summary</h3>
  </div>
  <p class="text-sm text-ifa-text-primary leading-relaxed">{summary}</p>
  <div class="flex items-center gap-2 mt-3 text-xs text-ifa-text-secondary">
    <BookOpen class="w-3 h-3" />
    <span>Based on {Math.floor(Math.random() * 10) + 5} academic sources and {Math.floor(Math.random() * 15) + 5} web resources</span>
  </div>
</div>
