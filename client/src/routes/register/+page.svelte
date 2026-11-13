<script lang="ts">
  import { onDestroy } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { ArrowRight, Code2, Lock, Mail, User, CheckCircle2, Loader2 } from 'lucide-svelte';
  import { ApiError, apiErrorMessage } from '$lib/api';
  import { AUTH_LIMITS, validateRegistration, type RegistrationField } from '$lib/auth/validation';
  import { register } from '$lib/stores/sessionStore';

  // Optional ?redirect=/path — after registration we send the new learner back
  // where they started (e.g. a shared course link or the chat page).
  $: redirectTarget = $page.url.searchParams.get('redirect');

  function safeRedirect(target: string | null): string | null {
    return target?.startsWith('/') && !target.startsWith('//') ? target : null;
  }

  let accountType: 'Learner' | 'Educator' = 'Learner';
  let name = '';
  let email = '';
  let password = '';
  let confirm = '';
  let submitting = false;
  let error = '';
  let status = '';
  let verificationPending = false;
  let cooldownSeconds = 0;
  let cooldownTimer: ReturnType<typeof setInterval> | undefined;
  let touched: Record<RegistrationField, boolean> = {
    name: false,
    email: false,
    password: false,
    confirm: false
  };
  $: validationErrors = validateRegistration({ name, email, password, confirm });

  function touch(field: RegistrationField) {
    touched = { ...touched, [field]: true };
  }

  function edit() {
    error = '';
  }

  function focusFirstError() {
    const first = (['name', 'email', 'password', 'confirm'] as RegistrationField[])
      .find((field) => validationErrors[field]);
    if (first) document.getElementById(first)?.focus();
  }

  function applyRateLimit(cause: unknown) {
    if (!(cause instanceof ApiError) || cause.status !== 429) return;
    cooldownSeconds = cause.retryAfterSeconds ?? 60;
    if (cooldownTimer) clearInterval(cooldownTimer);
    cooldownTimer = setInterval(() => {
      cooldownSeconds = Math.max(0, cooldownSeconds - 1);
      if (cooldownSeconds === 0 && cooldownTimer) {
        clearInterval(cooldownTimer);
        cooldownTimer = undefined;
      }
    }, 1000);
  }

  onDestroy(() => {
    if (cooldownTimer) clearInterval(cooldownTimer);
  });

  async function handleSubmit() {
    if (submitting) return;
    touched = { name: true, email: true, password: true, confirm: true };
    if (Object.keys(validationErrors).length > 0) {
      focusFirstError();
      return;
    }
    submitting = true;
    error = '';
    status = '';
    try {
      const outcome = await register(name.trim(), email.trim(), password, accountType);
      if (outcome.verificationRequired) {
        verificationPending = true;
        status = outcome.message ?? 'Check your email to verify your account.';
        return;
      }
      const destination = safeRedirect(redirectTarget) ?? (accountType === 'Educator' ? '/apply/educator' : '/app');
      await goto(destination);
    } catch (cause) {
      applyRateLimit(cause);
      error = apiErrorMessage(cause, 'Registration failed.');
    } finally {
      submitting = false;
    }
  }
</script>

<div class="register-page">
  <div class="register-layout">
    <!-- Brand panel -->
    <div class="register-story hidden lg:block">
      <div class="flex items-center gap-3 mb-6">
        <div class="w-11 h-11 rounded-xl bg-ifa-pine flex items-center justify-center text-white">
          <Code2 class="w-6 h-6" />
        </div>
        <span class="text-2xl font-bold text-ifa-pine">Witforge</span>
      </div>
      <h1 class="wf-display">
        Start with a path you can <span>finish.</span>
      </h1>
      <p class="text-ifa-text-secondary leading-relaxed mb-6">
        Create an account to learn from courses or build and share courses with learners.
      </p>
      <ul class="space-y-3 text-sm text-ifa-text-secondary">
        <li class="flex items-center gap-2"><CheckCircle2 class="w-4 h-4 text-ifa-pine" /> Separate Learner and Educator workspaces</li>
        <li class="flex items-center gap-2"><CheckCircle2 class="w-4 h-4 text-ifa-pine" /> Module quizzes and progress tracking</li>
        <li class="flex items-center gap-2"><CheckCircle2 class="w-4 h-4 text-ifa-pine" /> Study alongside the community</li>
      </ul>
    </div>

    <!-- Auth card -->
    <div class="register-card">
      <p class="form-index">01 / CREATE ACCOUNT</p>
      <h2 class="wf-display">Join Witforge.</h2>
      <p class="text-sm text-ifa-text-secondary mb-6">It only takes a moment.</p>

      {#if error}
        <p role="alert" class="mb-4 rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</p>
      {/if}
      {#if status}
        <div role="status" class="mb-4 rounded-xl border border-ifa-pine/30 bg-ifa-pine/10 p-3 text-sm text-ifa-text-primary">
          <p>{status}</p>
          {#if verificationPending}
            <a class="mt-2 inline-flex font-semibold text-ifa-pine" href={accountType === 'Educator' ? '/login?redirect=%2Fapply%2Feducator' : '/login'}>Continue to sign in</a>
          {/if}
        </div>
      {/if}

      <form class="space-y-4" novalidate on:submit|preventDefault={handleSubmit}>
        <fieldset>
          <legend class="block text-xs font-semibold text-ifa-text-secondary mb-2">I want to</legend>
          <div class="grid grid-cols-2 gap-2">
            <label class="flex cursor-pointer items-start gap-2 border border-ifa-border bg-ifa-card-muted p-3" class:border-ifa-pine={accountType === 'Learner'}>
              <input type="radio" bind:group={accountType} value="Learner" />
              <span><strong class="block text-xs text-ifa-text-primary">Learn</strong><small class="text-ifa-text-muted">Take courses and track progress</small></span>
            </label>
            <label class="flex cursor-pointer items-start gap-2 border border-ifa-border bg-ifa-card-muted p-3" class:border-ifa-pine={accountType === 'Educator'}>
              <input type="radio" bind:group={accountType} value="Educator" />
              <span><strong class="block text-xs text-ifa-text-primary">Teach</strong><small class="text-ifa-text-muted">Apply for an educator workspace</small></span>
            </label>
          </div>
        </fieldset>
        <div>
          <label for="name" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Full name</label>
          <div class="relative">
            <User class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
            <input
              id="name"
              type="text"
              bind:value={name}
              on:blur={() => touch('name')}
              on:input={edit}
              autocomplete="name"
              maxlength={AUTH_LIMITS.name}
              aria-invalid={touched.name && validationErrors.name ? 'true' : undefined}
              aria-describedby={touched.name && validationErrors.name ? 'name-error' : undefined}
              placeholder="Ada Lovelace"
              class="w-full pl-10 pr-4 py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
            />
          </div>
          {#if touched.name && validationErrors.name}<p id="name-error" class="mt-1 text-xs text-red-700">{validationErrors.name}</p>{/if}
        </div>

        <div>
          <label for="email" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Email</label>
          <div class="relative">
            <Mail class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
            <input
              id="email"
              type="email"
              bind:value={email}
              on:blur={() => touch('email')}
              on:input={edit}
              autocomplete="email"
              maxlength={AUTH_LIMITS.email}
              aria-invalid={touched.email && validationErrors.email ? 'true' : undefined}
              aria-describedby={touched.email && validationErrors.email ? 'email-error' : undefined}
              placeholder="you@example.com"
              class="w-full pl-10 pr-4 py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
            />
          </div>
          {#if touched.email && validationErrors.email}<p id="email-error" class="mt-1 text-xs text-red-700">{validationErrors.email}</p>{/if}
        </div>

        <div>
          <label for="password" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Password</label>
          <div class="relative">
            <Lock class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
            <input
              id="password"
              type="password"
              bind:value={password}
              on:blur={() => touch('password')}
              on:input={edit}
              autocomplete="new-password"
              minlength={AUTH_LIMITS.passwordMin}
              maxlength={AUTH_LIMITS.passwordMax}
              aria-invalid={touched.password && validationErrors.password ? 'true' : undefined}
              aria-describedby={touched.password && validationErrors.password ? 'password-help password-error' : 'password-help'}
              placeholder="At least 8 characters"
              class="w-full pl-10 pr-4 py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
            />
          </div>
          {#if touched.password && validationErrors.password}<p id="password-error" class="mt-1 text-xs text-red-700">{validationErrors.password}</p>{/if}
          <p id="password-help" class="mt-1 text-xs text-ifa-text-muted">Use 8–128 characters.</p>
        </div>

        <div>
          <label for="confirm" class="block text-xs font-semibold text-ifa-text-secondary mb-1">Confirm password</label>
          <div class="relative">
            <Lock class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
            <input
              id="confirm"
              type="password"
              bind:value={confirm}
              on:blur={() => touch('confirm')}
              on:input={edit}
              autocomplete="new-password"
              maxlength={AUTH_LIMITS.passwordMax}
              aria-invalid={touched.confirm && validationErrors.confirm ? 'true' : undefined}
              aria-describedby={touched.confirm && validationErrors.confirm ? 'confirm-error' : undefined}
              placeholder="Repeat your password"
              class="w-full pl-10 pr-4 py-3 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine"
            />
          </div>
          {#if touched.confirm && validationErrors.confirm}<p id="confirm-error" class="mt-1 text-xs text-red-700">{validationErrors.confirm}</p>{/if}
        </div>

        <button
          type="submit"
          disabled={submitting || verificationPending || cooldownSeconds > 0}
          class="w-full py-3 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-sm font-bold transition flex items-center justify-center gap-2 disabled:opacity-60"
        >
          {#if submitting}
            <Loader2 class="w-4 h-4 animate-spin" />
            <span>Creating account…</span>
          {:else if cooldownSeconds > 0}
            <span>Try again in {cooldownSeconds}s</span>
          {:else}
            <span>Create account</span>
            <ArrowRight class="w-4 h-4" />
          {/if}
        </button>
      </form>

      <p class="mt-4 rounded-xl bg-ifa-card-muted p-3 text-xs leading-relaxed text-ifa-text-secondary">
        {accountType === 'Educator'
          ? 'Educator accounts continue to a focused verification step. Learning and teaching tools stay locked until approval.'
          : 'Learner accounts can take courses immediately and apply for educator access later.'}
      </p>

      <p class="text-sm text-ifa-text-secondary mt-6 text-center">
        Already have an account?
        <a
          href={redirectTarget ? `/login?redirect=${encodeURIComponent(redirectTarget)}` : '/login'}
          class="font-semibold text-ifa-pine hover:text-ifa-pine-dark"
        >Sign in</a>
      </p>
    </div>
  </div>
</div>

<style>
  .register-page{display:grid;min-height:100svh;place-items:center;padding:2rem;background:linear-gradient(110deg,#17221c 0 42%,#f2efe7 42%)}.register-layout{display:grid;width:min(100%,72rem);grid-template-columns:.9fr 1.1fr;gap:5rem;align-items:center}.register-story{color:#f3f0e8}.register-story>div:first-child{margin-bottom:5rem!important}.register-story>div:first-child>div{border:1px solid #536057!important;border-radius:50%!important;background:#202a23!important}.register-story h1{max-width:34rem;margin-bottom:1.5rem;font-size:4.8rem;font-weight:420;line-height:.85;letter-spacing:-.055em}.register-story h1 span{color:#c7f464;font-weight:420}.register-story>p{max-width:31rem;color:#aeb8b1!important;font-size:.75rem;line-height:1.8}.register-story ul{margin-top:2.5rem}.register-story li{padding:.7rem 0;border-top:1px solid #3c4941;color:#b8c0ba!important;font-size:.65rem}.register-story li :global(svg){color:#c7f464!important}.register-card{padding:clamp(1.5rem,4vw,3rem);border:1px solid var(--wf-border);background:var(--wf-surface-raised);box-shadow:0 45px 100px -70px #17221c}.form-index{color:var(--wf-text-muted);font-size:.55rem!important;font-weight:850;letter-spacing:.14em}.register-card h2{margin:2rem 0 .35rem;font-size:3.5rem;font-weight:450;line-height:.9}.register-card>p:nth-of-type(2){margin-bottom:2rem!important;font-size:.68rem!important}.register-card form :global(label){font-size:.58rem!important;letter-spacing:.04em}.register-card form :global(input){border-radius:0!important;background:var(--wf-surface-canvas)!important;font-size:.7rem}.register-card form>button{border-radius:999px!important;color:#17221c!important;background:#c7f464!important}.register-card>p:nth-last-of-type(2){border-radius:0!important;border-left:3px solid #176b4a;background:var(--wf-surface-muted)!important}.register-card>p:last-child{font-size:.62rem!important}
  @media(max-width:1023px){.register-page{padding:1rem;background:var(--wf-surface-canvas)}.register-layout{display:block;width:min(100%,32rem)}.register-card{padding:1.5rem}.register-card h2{font-size:3rem}}
</style>
