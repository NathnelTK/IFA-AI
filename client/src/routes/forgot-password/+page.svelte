<script lang="ts">
  import { authApi } from '$lib/api';

  let email = '';
  let submitting = false;
  let message = '';

  async function submit() {
    submitting = true;
    message = '';
    try {
      const result = await authApi.forgotPassword(email.trim());
      message = result.message;
    } catch (cause) {
      message = cause instanceof Error ? cause.message : 'The reset request could not be submitted.';
    } finally {
      submitting = false;
    }
  }
</script>

<svelte:head><title>Reset password · Witforge</title></svelte:head>

<main class="min-h-screen grid place-items-center bg-ifa-bg px-6">
  <section class="w-full max-w-md rounded-2xl border border-ifa-border bg-ifa-card p-8 shadow-sm">
    <h1 class="text-2xl font-bold text-ifa-text-primary">Reset your password</h1>
    <p class="mt-2 text-sm text-ifa-text-secondary">Enter your account email. The response never reveals whether an account exists.</p>
    <form class="mt-6 space-y-4" on:submit|preventDefault={submit}>
      <label class="block text-sm font-semibold text-ifa-text-primary" for="reset-email">Email</label>
      <input id="reset-email" type="email" bind:value={email} autocomplete="email" required class="w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 text-sm" />
      <button disabled={submitting} class="w-full rounded-xl bg-ifa-pine px-4 py-3 text-sm font-bold text-white disabled:opacity-60">
        {submitting ? 'Sending…' : 'Send reset link'}
      </button>
    </form>
    {#if message}<p role="status" class="mt-4 text-sm text-ifa-text-secondary">{message}</p>{/if}
  </section>
</main>
