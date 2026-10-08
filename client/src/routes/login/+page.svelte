<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { ArrowRight, Code2, Lock, Mail, Sparkles, Loader2 } from 'lucide-svelte';
  import { authApi, type DemoAccountDto } from '$lib/api';
  import { signIn, continueAsDemo } from '$lib/stores/sessionStore';

  let email = '';
  let password = '';
  let submitting = false;
  let error = '';
  let demoAccounts: DemoAccountDto[] = [];
  let demoLoading = true;

  onMount(async () => {
    try {
      demoAccounts = await authApi.demoAccounts();
    } catch {
      demoAccounts = [];
    } finally {
      demoLoading = false;
    }
  });

  async function handleSubmit() {
    if (submitting) return;
    if (!email.trim() || !password) {
      error = 'Enter both your email and password.';
      return;
    }
    submitting = true;
    error = '';
    try {
      await signIn(email.trim(), password);
      await goto('/home');
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Sign in failed.';
    } finally {
      submitting = false;
    }
  }

  function useDemoAccount(account: DemoAccountDto) {
    email = account.email;
    password = 'ifa12345';
    error = '';
  }

  async function handleDemoLogin() {
    if (submitting) return;
    submitting = true;
    error = '';
    try {
      await continueAsDemo();
      await goto('/home');
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Demo sign in failed.';
    } finally {
      submitting = false;
    }
  }
</script>

<div class="min-h-screen bg-ifa-bg text-ifa-text-primary antialiased flex items-center justify-center p-6">
  <div class="w-full max-w-5xl grid grid-cols-1 lg:grid-cols-2 gap-8 items-center">
    <!-- Brand panel -->
    <div class="hidden lg:block">
      <div class="flex items-center gap-3 mb-6">
        <div class="w-11 h-11 rounded-xl bg-ifa-pine flex items-center justify-center text-white">
          <Code2 class="w-6 h-6" />
        </div>
        <span class="text-2xl font-bold text-ifa-pine">IFA</span>
      </div>
      <h1 class="text-4xl font-extrabold tracking-tight text-ifa-text-primary leading-tight mb-4">
        Welcome back to your <span class="text-ifa-pine">AI learning companion</span>
      </h1>
      <p class="text-ifa-text-secondary leading-relaxed mb-6">
        Sign in to continue your adaptive learning path, generate new modules, and track your progress.
      </p>
      <ul class="space-y-3 text-sm text-ifa-text-secondary">
        <li class="flex items-center gap-2"><Sparkles class="w-4 h-4 text-ifa-pine" /> Chat with the AI advisor to scope a course</li>
        <li class="flex gap-2"><Sparkles class="w-4 h-4 text-ifa-pine shrink-0 mt-0.5" /> Research-grounded, just-in-time module generation</li>
        <li class="flex gap-2"><Sparkles class="w-4 h-4 text-ifa-pine shrink-0 mt-0.5" /> Personal progress, skills and recommendations</li>
      </ul>
    </div>

    <!-- Auth card -->
    <div class="bg-ifa-card rounded-3xl border border-ifa-border shadow-elevated p-8">
      <h2 class="text-2xl font-bold text-ifa-text-primary mb-1">Sign in</h2>
      <p class="text-sm text-ifa-text-secondary mb-6">Use your IFA account to continue.</p>

      {#if error}
        <p role="alert" class="mb-4 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</p>
      {/if}

      <form class="space-y-4" on:submit|preventDefault={handleSubmit}>
        <div>
          <label for="email" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Email</label>
          <div class="relative">
            <Mail class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
            <input
              id="email"
              type="email"
              bind:value={email}
              autocomplete="email"
              placeholder="you@example.com"
              class="w-full pl-10 pr-4 py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
            />
          </div>
        </div>

        <div>
          <label for="password" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Password</label>
          <div class="relative">
            <Lock class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
            <input
              id="password"
              type="password"
              bind:value={password}
              autocomplete="current-password"
              placeholder="••••••••"
              class="w-full pl-10 pr-4 py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
            />
          </div>
        </div>

        <button
          type="submit"
          disabled={submitting}
          class="w-full py-3 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-sm font-bold transition flex items-center justify-center gap-2 disabled:opacity-60"
        >
          {#if submitting}
            <Loader2 class="w-4 h-4 animate-spin" />
            <span>Signing in…</span>
          {:else}
            <span>Sign in</span>
            <ArrowRight class="w-4 h-4" />
          {/if}
        </button>
      </form>

      <div class="flex items-center gap-3 my-5">
        <div class="flex-1 h-px bg-ifa-border"></div>
        <span class="text-[11px] font-semibold text-ifa-text-muted uppercase">or</span>
        <div class="flex-1 h-px bg-ifa-border"></div>
      </div>

      <button
        type="button"
        on:click={handleDemoLogin}
        disabled={submitting}
        class="w-full py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-primary text-sm font-semibold hover:bg-ifa-border/40 transition disabled:opacity-60"
      >
        Continue with the demo account
      </button>

      <!-- Demo accounts -->
      <div class="mt-5">
        <p class="text-[11px] font-semibold text-ifa-text-muted uppercase mb-2">Demo accounts</p>
        {#if demoLoading}
          <p class="text-xs text-ifa-text-muted">Loading demo accounts…</p>
        {:else if demoAccounts.length === 0}
          <p class="text-xs text-ifa-text-muted">Demo accounts unavailable — is the API running?</p>
        {:else}
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {#each demoAccounts as account}
              <button
                type="button"
                on:click={() => useDemoAccount(account)}
                class="flex items-center gap-2 p-2 rounded-xl border border-ifa-border bg-ifa-bg-warm hover:bg-ifa-card-muted transition text-left"
              >
                {#if account.avatarUrl}
                  <img src={account.avatarUrl} alt="" class="w-8 h-8 rounded-full object-cover" />
                {:else}
                  <div class="w-8 h-8 rounded-full bg-ifa-pine text-white flex items-center justify-center text-xs font-bold">
                    {account.name.charAt(0)}
                  </div>
                {/if}
                <div class="min-w-0">
                  <p class="text-xs font-semibold text-ifa-text-primary truncate">{account.name}</p>
                  <p class="text-[10px] text-ifa-text-muted truncate">{account.email}</p>
                </div>
              </button>
            {/each}
          </div>
          <p class="text-[11px] text-ifa-text-muted mt-2">Password for all demo accounts: <code class="font-mono">ifa12345</code></p>
        {/if}
      </div>

      <p class="text-sm text-ifa-text-secondary mt-6 text-center">
        New to IFA?
        <a href="/register" class="font-semibold text-ifa-pine hover:text-ifa-pine-dark">Create an account</a>
      </p>
    </div>
  </div>
</div>
