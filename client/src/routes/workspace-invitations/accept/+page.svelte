<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { workspacesApi } from '$lib/api';
  import { initSession, refreshSession } from '$lib/stores/sessionStore';

  let message = 'Checking the invitation…';
  let failed = false;

  onMount(async () => {
    const token = $page.url.searchParams.get('token') ?? '';
    if (!token) {
      failed = true;
      message = 'This invitation link is incomplete.';
      return;
    }
    const session = await initSession();
    if (!session.authenticated) {
      await goto(`/login?redirect=${encodeURIComponent(`${$page.url.pathname}${$page.url.search}`)}`);
      return;
    }
    try {
      await workspacesApi.acceptInvitation(token);
      await refreshSession();
      message = 'The workspace invitation was accepted.';
    } catch (cause) {
      failed = true;
      message = cause instanceof Error ? cause.message : 'The invitation could not be accepted.';
    }
  });
</script>

<svelte:head><title>Workspace invitation · Witforge</title></svelte:head>

<main class="min-h-screen grid place-items-center bg-ifa-bg px-6">
  <section class="w-full max-w-md rounded-2xl border border-ifa-border bg-ifa-card p-8 text-center shadow-sm">
    <h1 class="text-2xl font-bold text-ifa-text-primary">Workspace invitation</h1>
    <p role={failed ? 'alert' : 'status'} class="mt-4 text-sm text-ifa-text-secondary">{message}</p>
    {#if !failed && message.includes('accepted')}
      <a href="/studio/courses" class="mt-6 inline-flex rounded-xl bg-ifa-pine px-5 py-3 text-sm font-bold text-white">Open Studio</a>
    {/if}
  </section>
</main>
