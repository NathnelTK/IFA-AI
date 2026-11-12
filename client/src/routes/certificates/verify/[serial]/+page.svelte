<script lang="ts">
  import { CheckCircle2, XCircle } from 'lucide-svelte';
  export let data: { certificate: any };
</script>

<svelte:head><title>Certificate verification · Witforge</title></svelte:head>

<main class="min-h-screen grid place-items-center bg-ifa-bg px-4 py-12">
  <section class="w-full max-w-2xl rounded-3xl border border-ifa-border bg-ifa-card p-7 shadow-sm sm:p-10">
    <div class="flex items-center gap-3">
      {#if data.certificate.valid}<CheckCircle2 class="h-8 w-8 text-emerald-600" />{:else}<XCircle class="h-8 w-8 text-red-600" />{/if}
      <div><p class="text-xs font-bold uppercase tracking-[0.18em] text-ifa-text-muted">Witforge certificate</p><h1 class="text-2xl font-bold">{data.certificate.valid ? 'Verified' : 'Not currently valid'}</h1></div>
    </div>
    <dl class="mt-8 grid gap-5 sm:grid-cols-2">
      <div><dt class="text-xs font-bold uppercase text-ifa-text-muted">Learner</dt><dd class="mt-1 text-lg font-semibold">{data.certificate.learnerName}</dd></div>
      <div><dt class="text-xs font-bold uppercase text-ifa-text-muted">Course</dt><dd class="mt-1 text-lg font-semibold">{data.certificate.courseTitle}</dd></div>
      <div><dt class="text-xs font-bold uppercase text-ifa-text-muted">Release</dt><dd class="mt-1">Version {data.certificate.releaseVersion}</dd></div>
      <div><dt class="text-xs font-bold uppercase text-ifa-text-muted">Completed</dt><dd class="mt-1">{new Date(data.certificate.completedAt).toLocaleDateString()}</dd></div>
      <div class="sm:col-span-2"><dt class="text-xs font-bold uppercase text-ifa-text-muted">Serial</dt><dd class="mt-1 font-mono text-sm">{data.certificate.serialNumber}</dd></div>
    </dl>
    {#if data.certificate.revokedAt}<p role="alert" class="mt-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">Revoked: {data.certificate.revocationReason}</p>{/if}
  </section>
</main>
