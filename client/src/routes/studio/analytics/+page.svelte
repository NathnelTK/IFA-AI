<script lang="ts">
  import { onMount } from 'svelte';
  import { analyticsApi } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';
  let data: any = null;
  let error = '';
  $: workspaceId = typeof localStorage === 'undefined' ? $session.workspaces?.[0]?.id : localStorage.getItem('witforge.activeWorkspace') ?? $session.workspaces?.[0]?.id;
  onMount(async () => {
    if (!workspaceId) return;
    try { data = await analyticsApi.workspace(workspaceId); }
    catch (cause) { error = cause instanceof Error ? cause.message : 'Analytics could not be loaded.'; }
  });
</script>

<svelte:head><title>Studio analytics · Witforge</title></svelte:head>

<main class="mx-auto max-w-6xl px-4 py-8 sm:px-6">
  <p class="text-xs font-bold uppercase tracking-[0.18em] text-ifa-pine">Studio</p><h1 class="mt-2 text-3xl font-bold">Analytics</h1>
  {#if error}<p role="alert" class="mt-5 text-red-700">{error}</p>{/if}
  {#if data}
    <div class="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {#each [['Enrollments', data.enrollments], ['Completion rate', `${data.completionRate}%`], ['Revenue', `${data.revenue} ${data.currency}`], ['Rating', `${data.rating} · ${data.reviewCount} reviews`]] as metric}
        <article class="rounded-2xl border border-ifa-border bg-ifa-card p-5"><p class="text-xs font-bold uppercase text-ifa-text-muted">{metric[0]}</p><p class="mt-2 text-2xl font-bold">{metric[1]}</p></article>
      {/each}
    </div>
    <section class="mt-6 rounded-2xl border border-ifa-border bg-ifa-card p-5"><h2 class="font-bold">Enrollment trend</h2><div class="mt-6 flex h-48 items-end gap-2">{#each data.enrollmentTrend as point}<div class="flex min-w-0 flex-1 flex-col items-center gap-2"><div class="w-full rounded-t bg-ifa-pine" style={`height:${Math.max(4, point.count * 12)}px`} title={`${point.count} enrollments`}></div><span class="text-[10px] text-ifa-text-muted">{new Date(point.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}</span></div>{/each}</div></section>
  {/if}
</main>
