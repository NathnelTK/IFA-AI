<script lang="ts">
  import { page } from '$app/stores';
  import { authApi } from '$lib/api';

  let password = '';
  let confirmPassword = '';
  let submitting = false;
  let success = false;
  let error = '';

  async function submit() {
    error = '';
    if (password.length < 8 || password !== confirmPassword) {
      error = 'Use at least eight characters and make sure both passwords match.';
      return;
    }
    const token = $page.url.searchParams.get('token') ?? '';
    if (!token) {
      error = 'This reset link is incomplete.';
      return;
    }
    submitting = true;
    try {
      await authApi.resetPassword(token, password);
      success = true;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The password could not be reset.';
    } finally {
      submitting = false;
    }
  }
</script>

<svelte:head><title>Choose a password · Witforge</title></svelte:head>

<main class="min-h-screen grid place-items-center bg-ifa-bg px-6">
  <section class="w-full max-w-md rounded-2xl border border-ifa-border bg-ifa-card p-8 shadow-sm">
    <h1 class="text-2xl font-bold text-ifa-text-primary">Choose a new password</h1>
    {#if success}
      <p role="status" class="mt-4 text-sm text-ifa-text-secondary">Your password was updated and existing sessions were revoked.</p>
      <a href="/login" class="mt-6 inline-flex rounded-xl bg-ifa-pine px-5 py-3 text-sm font-bold text-white">Sign in</a>
    {:else}
      <form class="mt-6 space-y-4" on:submit|preventDefault={submit}>
        <label class="block text-sm font-semibold" for="new-password">New password</label>
        <input id="new-password" type="password" bind:value={password} autocomplete="new-password" minlength="8" required class="w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 text-sm" />
        <label class="block text-sm font-semibold" for="confirm-password">Confirm password</label>
        <input id="confirm-password" type="password" bind:value={confirmPassword} autocomplete="new-password" minlength="8" required class="w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 text-sm" />
        {#if error}<p role="alert" class="text-sm text-red-700">{error}</p>{/if}
        <button disabled={submitting} class="w-full rounded-xl bg-ifa-pine px-4 py-3 text-sm font-bold text-white disabled:opacity-60">{submitting ? 'Updating…' : 'Update password'}</button>
      </form>
    {/if}
  </section>
</main>
