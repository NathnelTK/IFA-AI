<script lang="ts">
  import { goto } from '$app/navigation';
  import { ArrowRight, Code2, Lock, Mail, User, Sparkles, Loader2 } from 'lucide-svelte';
  import { register } from '$lib/stores/sessionStore';

  let name = '';
  let email = '';
  let password = '';
  let confirm = '';
  let submitting = false;
  let error = '';

  async function handleSubmit() {
    if (submitting) return;
    if (!name.trim() || !email.trim() || !password) {
      error = 'Name, email and password are required.';
      return;
    }
    if (password.length < 6) {
      error = 'Password must be at least 6 characters.';
      return;
    }
    if (password !== confirm) {
      error = 'Passwords do not match.';
      return;
    }
    submitting = true;
    error = '';
    try {
      await register(name.trim(), email.trim(), password);
      await goto('/home');
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Registration failed.';
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
        Start your <span class="text-ifa-pine">personalized</span> learning journey
      </h1>
      <p class="text-ifa-text-secondary leading-relaxed mb-6">
        Create an account and tell the AI advisor what you want to learn. IFA researches your topic and builds a course just for you.
      </p>
      <ul class="space-y-3 text-sm text-ifa-text-secondary">
        <li class="flex items-center gap-2"><Sparkles class="w-4 h-4 text-ifa-pine" /> Free, adaptive course generation</li>
        <li class="flex items-center gap-2"><Sparkles class="w-4 h-4 text-ifa-pine" /> Module quizzes and progress tracking</li>
        <li class="flex items-center gap-2"><Sparkles class="w-4 h-4 text-ifa-pine" /> Study alongside the community</li>
      </ul>
    </div>

    <!-- Auth card -->
    <div class="bg-ifa-card rounded-3xl border border-ifa-border shadow-elevated p-8">
      <h2 class="text-2xl font-bold text-ifa-text-primary mb-1">Create account</h2>
      <p class="text-sm text-ifa-text-secondary mb-6">It only takes a moment.</p>

      {#if error}
        <p role="alert" class="mb-4 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</p>
      {/if}

      <form class="space-y-4" on:submit|preventDefault={handleSubmit}>
        <div>
          <label for="name" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Full name</label>
          <div class="relative">
            <User class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
            <input
              id="name"
              type="text"
              bind:value={name}
              autocomplete="name"
              placeholder="Ada Lovelace"
              class="w-full pl-10 pr-4 py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
            />
          </div>
        </div>

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
              autocomplete="new-password"
              placeholder="At least 6 characters"
              class="w-full pl-10 pr-4 py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
            />
          </div>
        </div>

        <div>
          <label for="confirm" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Confirm password</label>
          <div class="relative">
            <Lock class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
            <input
              id="confirm"
              type="password"
              bind:value={confirm}
              autocomplete="new-password"
              placeholder="Repeat your password"
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
            <span>Creating account…</span>
          {:else}
            <span>Create account</span>
            <ArrowRight class="w-4 h-4" />
          {/if}
        </button>
      </form>

      <p class="text-sm text-ifa-text-secondary mt-6 text-center">
        Already have an account?
        <a href="/login" class="font-semibold text-ifa-pine hover:text-ifa-pine-dark">Sign in</a>
      </p>
    </div>
  </div>
</div>
