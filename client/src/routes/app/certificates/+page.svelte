<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '$lib/api';
  import EmptyState from '$lib/components/ui/EmptyState.svelte';
  let certificates: any[] = [];
  let loading = true;
  onMount(async () => {
    try { certificates = await api.get<any[]>('/api/certificates/mine'); } catch { certificates = []; }
    finally { loading = false; }
  });
</script>

<svelte:head><title>Certificates · Witforge</title></svelte:head>

<main class="mx-auto max-w-5xl px-4 py-8 sm:px-6">
  <h1 class="text-3xl font-bold">Certificates</h1>
  <p class="mt-2 text-sm text-ifa-text-secondary">Completion records are bound to immutable course releases.</p>
  {#if !loading && certificates.length === 0}
    <div class="mt-8"><EmptyState title="No certificates yet" description="Certificates appear after all release lessons, required quizzes, and assignments are complete." /></div>
  {:else}
    <div class="mt-8 grid gap-4 sm:grid-cols-2">
      {#each certificates as certificate}
        <article class="rounded-2xl border border-ifa-border bg-ifa-card p-5"><p class="text-xs font-bold uppercase text-ifa-pine">{certificate.status}</p><h2 class="mt-2 text-lg font-bold">{certificate.courseTitle}</h2><p class="mt-2 font-mono text-xs text-ifa-text-muted">{certificate.serialNumber}</p>{#if certificate.status === 'Ready'}<a href={`/api/certificates/${certificate.id}/download`} class="mt-5 inline-flex rounded-lg bg-ifa-pine px-4 py-2 text-sm font-bold text-white">Download PDF</a>{/if}</article>
      {/each}
    </div>
  {/if}
</main>
