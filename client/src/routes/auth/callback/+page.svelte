<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { ssoApi } from '$lib/api';
  import { consumePkceState } from '$lib/auth/pkce';
  import { refreshSession } from '$lib/stores/sessionStore';

  let error = '';

  onMount(async () => {
    const code = $page.url.searchParams.get('code') ?? '';
    const state = $page.url.searchParams.get('state') ?? '';
    const saved = consumePkceState(state);
    if (!code || !saved) {
      error = 'The sign-in response is invalid or has expired.';
      return;
    }

    try {
      await ssoApi.exchange({
        clientId: saved.clientId,
        redirectUri: saved.redirectUri,
        code,
        codeVerifier: saved.verifier
      });
      await refreshSession();
      await goto(saved.returnTo);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The secure session could not be created.';
    }
  });
</script>

<svelte:head><title>Signing in to Witforge</title></svelte:head>

<main class="min-h-screen grid place-items-center bg-ifa-bg px-6">
  <section class="w-full max-w-md rounded-2xl border border-ifa-border bg-ifa-card p-8 text-center shadow-sm">
    <h1 class="text-xl font-bold text-ifa-text-primary">Finishing sign in</h1>
    {#if error}
      <p role="alert" class="mt-4 text-sm text-red-700">{error}</p>
    {:else}
      <p class="mt-3 text-sm text-ifa-text-secondary">Exchanging the one-time authorization code.</p>
    {/if}
  </section>
</main>
