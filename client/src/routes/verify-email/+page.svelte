<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { authApi } from '$lib/api';

  let state: 'working' | 'success' | 'error' = 'working';
  let message = 'Verifying your email address…';

  onMount(async () => {
    const token = $page.url.searchParams.get('token') ?? '';
    if (!token) {
      state = 'error';
      message = 'This verification link is incomplete.';
      return;
    }
    try {
      await authApi.verifyEmail(token);
      state = 'success';
      message = 'Your email is verified. You can now sign in.';
    } catch (cause) {
      state = 'error';
      message = cause instanceof Error ? cause.message : 'This verification link is invalid or expired.';
    }
  });
</script>

<svelte:head><title>Verify email · Witforge</title></svelte:head>

<main class="min-h-screen grid place-items-center bg-ifa-bg px-6">
  <section class="w-full max-w-md rounded-2xl border border-ifa-border bg-ifa-card p-8 text-center shadow-sm">
    <p class="text-xs font-bold uppercase tracking-[0.18em] text-ifa-pine">Account security</p>
    <h1 class="mt-2 text-2xl font-bold text-ifa-text-primary">Email verification</h1>
    <p role={state === 'error' ? 'alert' : 'status'} class="mt-4 text-sm text-ifa-text-secondary">{message}</p>
    {#if state === 'success'}
      <a href="/login" class="mt-6 inline-flex rounded-xl bg-ifa-pine px-5 py-3 text-sm font-bold text-white">Continue to sign in</a>
    {/if}
  </section>
</main>
