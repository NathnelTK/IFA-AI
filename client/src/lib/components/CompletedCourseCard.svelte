<script lang="ts">
  import { CheckCircle, Award, ArrowRight, Download } from 'lucide-svelte';

  export let course: any;
  export let onViewCertificate = () => {};

  function handleDownloadCertificate() {
    console.log('Downloading certificate for:', course.id);
  }
</script>

<div class="bg-ifa-card rounded-2xl border border-ifa-border p-5 shadow-card hover:shadow-elevated transition">
  <div class="flex items-start justify-between mb-3">
    <div class="flex items-center gap-2">
      <div class="w-8 h-8 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-600">
        <CheckCircle class="w-4 h-4" />
      </div>
      <div>
        <h3 class="text-sm font-bold text-ifa-text-primary">{course.title}</h3>
        <p class="text-xs text-ifa-text-secondary mt-1">Completed {course.completedDate}</p>
      </div>
    </div>
    <div class="text-right">
      <div class="text-lg font-black text-emerald-600">{course.finalScore}%</div>
      <div class="text-[10px] text-ifa-text-muted">Final Score</div>
    </div>
  </div>

  <!-- Achievement Badge -->
  <div class="flex items-center gap-2 bg-emerald-50 rounded-lg p-3 mb-3">
    <Award class="w-4 h-4 text-emerald-600" />
    <span class="text-xs font-semibold text-emerald-700">Course Completed Successfully</span>
  </div>

  <!-- Actions -->
  <div class="flex gap-2">
    <button
      type="button"
      on:click={onViewCertificate}
      class="flex-1 py-2 bg-ifa-pine text-white rounded-lg text-xs font-semibold flex items-center justify-center gap-1.5 hover:bg-emerald-800 transition"
    >
      <Award class="w-3 h-3" />
      <span>View Certificate</span>
    </button>
    <button
      type="button"
      on:click={handleDownloadCertificate}
      class="px-3 py-2 border border-ifa-border rounded-lg text-xs font-semibold text-ifa-text-secondary hover:bg-ifa-card-muted transition"
    >
      <Download class="w-3 h-3" />
    </button>
  </div>
</div>
