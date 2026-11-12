<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { ssoApi } from '$lib/api';
  import { initSession } from '$lib/stores/sessionStore';

  let error = '';

  onMount(async () => {
    const current = $page.url;
    const clientId = current.searchParams.get('client_id') ?? '';
    const redirectUri = current.searchParams.get('redirect_uri') ?? '';
    const codeChallenge = current.searchParams.get('code_challenge') ?? '';
    const state = current.searchParams.get('state') ?? '';
    if (!clientId || !redirectUri || !codeChallenge || !state) {
      error = 'This sign-in request is incomplete.';
      return;
    }

    const currentSession = await initSession();
    if (!currentSession.authenticated) {
      await goto(`/login?redirect=${encodeURIComponent(`${current.pathname}${current.search}`)}`);
      return;
    }
    try {
      const result = await ssoApi.authorize({ clientId, redirectUri, codeChallenge, state });
      window.location.assign(result.redirectUrl);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The sign-in request could not be authorized.';
    }
  });
</script>

<svelte:head><title>Connecting to Witforge</title></svelte:head>

<main class="min-h-screen grid place-items-center bg-ifa-bg px-6">
  <section class="w-full max-w-md rounded-2xl border border-ifa-border bg-ifa-card p-8 text-center shadow-sm">
    <h1 class="text-xl font-bold text-ifa-text-primary">Connecting your secure session</h1>
    {#if error}
      <p role="alert" class="mt-4 text-sm text-red-700">{error}</p>
    {:else}
      <p class="mt-3 text-sm text-ifa-text-secondary">Verifying this app and preparing a host-scoped session.</p>
    {/if}
  </section>
</main>
