<script lang="ts">
  import { authApi } from '$lib/api';

  let enrollment: { secret: string; otpauthUri: string } | null = null;
  let code = '';
  let recoveryCodes: string[] = [];
  let message = '';
  let error = '';

  async function begin() {
    error = '';
    try {
      enrollment = await authApi.beginMfaEnrollment();
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'MFA enrollment could not start.';
    }
  }

  async function confirm() {
    error = '';
    try {
      const result = await authApi.confirmMfaEnrollment(code);
      recoveryCodes = result.recoveryCodes;
      enrollment = null;
      code = '';
      message = 'MFA is enabled. Store these recovery codes securely; they are shown once.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The authenticator code was rejected.';
    }
  }

  async function disable() {
    error = '';
    try {
      await authApi.disableMfa(code);
      code = '';
      recoveryCodes = [];
      message = 'MFA was disabled. Existing sessions will be revalidated.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'MFA could not be disabled.';
    }
  }
</script>

<svelte:head><title>Security settings · Witforge</title></svelte:head>

<main class="mx-auto max-w-3xl px-4 py-10 sm:px-8">
  <h1 class="text-3xl font-bold text-ifa-text-primary">Security</h1>
  <p class="mt-2 text-sm text-ifa-text-secondary">Manage authenticator-based multi-factor authentication and recovery codes.</p>

  <section class="mt-8 rounded-2xl border border-ifa-border bg-ifa-card p-6">
    <h2 class="text-lg font-bold">Authenticator app</h2>
    {#if !enrollment && recoveryCodes.length === 0}
      <button on:click={begin} class="mt-4 rounded-xl bg-ifa-pine px-4 py-2.5 text-sm font-bold text-white">Set up MFA</button>
    {/if}
    {#if enrollment}
      <p class="mt-4 text-sm text-ifa-text-secondary">Add this secret to your authenticator app, then enter its six-digit code.</p>
      <code class="mt-3 block overflow-wrap-anywhere rounded-lg bg-ifa-card-muted p-3 text-sm">{enrollment.secret}</code>
      <input bind:value={code} autocomplete="one-time-code" maxlength="6" placeholder="123456" class="mt-4 w-full max-w-xs rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3" />
      <button on:click={confirm} class="mt-3 rounded-xl bg-ifa-pine px-4 py-2.5 text-sm font-bold text-white">Confirm MFA</button>
    {/if}
    {#if recoveryCodes.length > 0}
      <p role="status" class="mt-4 text-sm text-ifa-text-secondary">{message}</p>
      <pre class="mt-3 overflow-auto rounded-xl bg-ifa-card-muted p-4 text-sm">{recoveryCodes.join('\n')}</pre>
    {/if}
    <div class="mt-8 border-t border-ifa-border pt-5">
      <label class="block text-sm font-semibold" for="disable-mfa">Disable with an authenticator or recovery code</label>
      <div class="mt-2 flex max-w-md gap-2">
        <input id="disable-mfa" bind:value={code} maxlength="32" class="min-w-0 flex-1 rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-2.5" />
        <button on:click={disable} class="rounded-xl border border-red-300 px-4 py-2.5 text-sm font-bold text-red-700">Disable</button>
      </div>
    </div>
    {#if error}<p role="alert" class="mt-4 text-sm text-red-700">{error}</p>{/if}
  </section>
</main>
