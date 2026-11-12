<script lang="ts">
  import { onDestroy, onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { ArrowRight, Check, Loader2, Lock, Mail, ShieldCheck } from 'lucide-svelte';
  import { ApiError, apiErrorMessage, authApi, type DemoAccountDto } from '$lib/api';
  import { AUTH_LIMITS, validateLogin, validateMfaCode, type LoginField } from '$lib/auth/validation';
  import { signIn, continueAsDemo, completeMfaSignIn, MfaRequiredError } from '$lib/stores/sessionStore';

  $: redirectTarget = $page.url.searchParams.get('redirect');
  let email = '';
  let password = '';
  let mfaCode = '';
  let mfaChallenge = '';
  let submitting = false;
  let error = '';
  let notice = '';
  let verificationRequired = false;
  let cooldownSeconds = 0;
  let cooldownTimer: ReturnType<typeof setInterval> | undefined;
  let touched: Record<LoginField, boolean> = { email: false, password: false };
  let mfaTouched = false;
  let demoAccounts: DemoAccountDto[] = [];
  let demoLoading = true;
  $: loginErrors = validateLogin({ email, password });
  $: mfaError = validateMfaCode(mfaCode);

  onMount(async () => {
    try { demoAccounts = await authApi.demoAccounts(); }
    catch { demoAccounts = []; }
    finally { demoLoading = false; }
  });

  function touch(field: LoginField) {
    touched = { ...touched, [field]: true };
  }

  function edit() {
    error = '';
    notice = '';
    verificationRequired = false;
  }

  function focusFirstError() {
    if (mfaChallenge) {
      document.getElementById('mfa-code')?.focus();
      return;
    }
    const first = (['email', 'password'] as LoginField[]).find((field) => loginErrors[field]);
    if (first) document.getElementById(first)?.focus();
  }

  function responseCode(cause: unknown): string | undefined {
    if (!(cause instanceof ApiError) || !cause.body || typeof cause.body !== 'object') return undefined;
    const code = (cause.body as Record<string, unknown>).code;
    return typeof code === 'string' ? code : undefined;
  }

  function cancelMfa() {
    mfaChallenge = '';
    mfaCode = '';
    mfaTouched = false;
    error = '';
    notice = '';
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

  async function submit() {
    if (submitting) return;
    if (mfaChallenge) {
      mfaTouched = true;
      if (mfaError) {
        focusFirstError();
        return;
      }
    } else {
      touched = { email: true, password: true };
      if (Object.keys(loginErrors).length > 0) {
        focusFirstError();
        return;
      }
    }
    submitting = true;
    error = '';
    notice = '';
    verificationRequired = false;
    try {
      if (mfaChallenge) await completeMfaSignIn(mfaChallenge, mfaCode.trim());
      else await signIn(email.trim(), password);
      const target = redirectTarget?.startsWith('/') && !redirectTarget.startsWith('//') ? redirectTarget : '/app';
      await goto(target, { replaceState: true });
    } catch (cause) {
      if (cause instanceof MfaRequiredError) {
        mfaChallenge = cause.challengeToken;
        password = '';
        mfaCode = '';
        mfaTouched = false;
        notice = cause.message;
      } else {
        applyRateLimit(cause);
        verificationRequired = responseCode(cause) === 'email_verification_required';
        error = apiErrorMessage(cause, 'Sign in failed.');
      }
    } finally { submitting = false; }
  }

  function useDemo(account: DemoAccountDto) {
    email = account.email;
    password = 'ifa12345';
    touched = { email: false, password: false };
    edit();
  }

  async function resendVerification() {
    submitting = true;
    error = '';
    try {
      const result = await authApi.resendVerification(email.trim());
      verificationRequired = false;
      notice = result.message;
    } catch (cause) {
      applyRateLimit(cause);
      error = apiErrorMessage(cause, 'The verification email could not be sent.');
    } finally {
      submitting = false;
    }
  }

  async function demo() {
    submitting = true;
    edit();
    try {
      await continueAsDemo();
      const target = redirectTarget?.startsWith('/') && !redirectTarget.startsWith('//') ? redirectTarget : '/app';
      await goto(target);
    } catch (cause) {
      applyRateLimit(cause);
      error = apiErrorMessage(cause, 'Demo sign in failed.');
    }
    finally { submitting = false; }
  }
</script>

<svelte:head><title>Sign in · Witforge</title></svelte:head>

<main class="auth-page">
  <section class="auth-story">
    <a href="/" class="auth-brand"><span><i></i><i></i><i></i></span>Witforge</a>
    <div class="story-copy">
      <p class="wf-kicker">Return to the work</p>
      <h1 class="wf-display">The next useful step is already waiting.</h1>
      <p>Continue a release-bound course, return to an educator draft, or move your organization’s learning program forward.</p>
    </div>
    <div class="story-evidence">
      <div><Check /><span><strong>One identity</strong><small>Learner and educator capabilities can coexist.</small></span></div>
      <div><Check /><span><strong>Scoped sessions</strong><small>Each host receives its own protected session.</small></span></div>
      <div><Check /><span><strong>Stable history</strong><small>Your progress stays tied to the release you joined.</small></span></div>
    </div>
    <p class="story-quote wf-display">“Build carefully. Learn deeply.”</p>
  </section>

  <section class="auth-form-side">
    <div class="mobile-brand"><a href="/">Witforge</a></div>
    <div class="auth-form-wrap">
      <p class="form-index">01 / SIGN IN</p>
      <h2 class="wf-display">Welcome back.</h2>
      <p class="form-intro">Use your account credentials to continue securely.</p>

      {#if error}<p role="alert" class="auth-error">{error}</p>{/if}
      {#if notice}<p role="status" class="auth-notice">{notice}</p>{/if}
      {#if verificationRequired}<button type="button" class="verification-button" on:click={resendVerification} disabled={submitting || cooldownSeconds > 0}>Resend verification email</button>{/if}
      <form novalidate on:submit|preventDefault={submit}>
        {#if mfaChallenge}
          <label for="mfa-code">Authenticator or recovery code</label>
          <div class="auth-input"><ShieldCheck /><input id="mfa-code" bind:value={mfaCode} on:blur={() => mfaTouched = true} on:input={edit} autocomplete="one-time-code" maxlength={AUTH_LIMITS.mfaCode} aria-invalid={mfaTouched && mfaError ? 'true' : undefined} aria-describedby={mfaTouched && mfaError ? 'mfa-error' : undefined} placeholder="123456" /></div>
          {#if mfaTouched && mfaError}<p id="mfa-error" class="field-error">{mfaError}</p>{/if}
          <button type="button" class="mfa-back" on:click={cancelMfa}>Use a different account</button>
        {:else}
          <label for="email">Email address</label>
          <div class="auth-input"><Mail /><input id="email" type="email" bind:value={email} on:blur={() => touch('email')} on:input={edit} autocomplete="email" maxlength={AUTH_LIMITS.email} aria-invalid={touched.email && loginErrors.email ? 'true' : undefined} aria-describedby={touched.email && loginErrors.email ? 'login-email-error' : undefined} placeholder="you@example.com" /></div>
          {#if touched.email && loginErrors.email}<p id="login-email-error" class="field-error">{loginErrors.email}</p>{/if}
          <div class="password-label"><label for="password">Password</label><a href="/forgot-password">Forgot password?</a></div>
          <div class="auth-input"><Lock /><input id="password" type="password" bind:value={password} on:blur={() => touch('password')} on:input={edit} autocomplete="current-password" maxlength={AUTH_LIMITS.passwordMax} aria-invalid={touched.password && loginErrors.password ? 'true' : undefined} aria-describedby={touched.password && loginErrors.password ? 'login-password-error' : undefined} placeholder="Your password" /></div>
          {#if touched.password && loginErrors.password}<p id="login-password-error" class="field-error">{loginErrors.password}</p>{/if}
        {/if}
        <button class="submit-button" disabled={submitting || cooldownSeconds > 0}>
          {#if submitting}<Loader2 class="animate-spin" />{/if}{cooldownSeconds > 0 ? `Try again in ${cooldownSeconds}s` : mfaChallenge ? 'Verify and continue' : 'Sign in'}<ArrowRight />
        </button>
      </form>

      <p class="create-account">New to Witforge? <a href={`/register${redirectTarget ? `?redirect=${encodeURIComponent(redirectTarget)}` : ''}`}>Create an account</a></p>

      {#if !demoLoading && demoAccounts.length > 0 && !mfaChallenge}
        <div class="demo-panel">
          <div><span>Development access</span><button on:click={demo} disabled={submitting || cooldownSeconds > 0}>Enter demo</button></div>
          <div class="demo-accounts">{#each demoAccounts.slice(0, 2) as account}<button on:click={() => useDemo(account)}><strong>{account.role}</strong><small>{account.email}</small></button>{/each}</div>
        </div>
      {/if}
    </div>
    <footer><span>Protected by host-scoped sessions</span><a href="/">Back to Witforge</a></footer>
  </section>
</main>

<style>
  .auth-page{display:grid;grid-template-columns:minmax(28rem,.92fr) minmax(30rem,1.08fr);min-height:100svh;background:var(--wf-surface-canvas)}.auth-story{position:relative;display:flex;flex-direction:column;justify-content:space-between;overflow:hidden;padding:2.2rem 3.2rem 2.8rem;color:#f3f0e8;background:#17221c}.auth-story:after{position:absolute;right:-20%;bottom:-23%;width:32rem;height:32rem;border:1px solid #526057;border-radius:50%;box-shadow:0 0 0 4rem #17221c,0 0 0 calc(4rem + 1px) #3c4941,0 0 0 8rem #17221c,0 0 0 calc(8rem + 1px) #3c4941;content:''}.auth-brand{z-index:1;display:flex;align-items:center;gap:.65rem;width:max-content;font-size:.9rem;font-weight:850}.auth-brand>span{display:inline-flex;align-items:end;gap:2px;width:1.8rem;height:1.8rem;padding:5px;border:1px solid #77837a;border-radius:50%}.auth-brand i{width:3px;border-radius:2px;background:#c7f464}.auth-brand i:nth-child(1){height:35%}.auth-brand i:nth-child(2){height:80%}.auth-brand i:nth-child(3){height:55%}.story-copy{z-index:1;max-width:42rem;margin:7rem 0 4rem}.story-copy .wf-kicker{color:#c7f464}.story-copy h1{margin:1.5rem 0;font-size:clamp(4rem,6vw,7rem);font-weight:420;line-height:.85;letter-spacing:-.06em}.story-copy>p:last-child{max-width:35rem;color:#aeb8b1;font-size:.8rem;line-height:1.8}.story-evidence{z-index:1;display:grid;gap:1px;border:1px solid #3c4941;background:#3c4941}.story-evidence>div{display:flex;gap:.7rem;padding:.9rem;background:#202a23}.story-evidence :global(svg){width:.9rem;color:#c7f464}.story-evidence span{display:flex;flex-direction:column}.story-evidence strong{font-size:.64rem}.story-evidence small{margin-top:.18rem;color:#8f9b92;font-size:.54rem}.story-quote{z-index:1;margin-top:2rem;color:#8f9b92;font-size:.75rem;font-style:italic}
  .auth-form-side{display:flex;min-height:100svh;flex-direction:column;padding:2.2rem 3.2rem 1.4rem}.mobile-brand{display:none}.auth-form-wrap{width:min(100%,29rem);margin:auto}.form-index{color:var(--wf-text-muted);font-size:.56rem;font-weight:850;letter-spacing:.14em}.auth-form-wrap h2{margin-top:2.4rem;font-size:3.6rem;font-weight:450;line-height:.9}.form-intro{margin:1rem 0 2.5rem;color:var(--wf-text-muted);font-size:.7rem}.auth-error{margin-bottom:1rem;padding:.75rem;border-left:3px solid #b42318;color:#8c1d18;background:#fee4e2;font-size:.65rem}.auth-form-wrap form{display:flex;flex-direction:column}.auth-form-wrap label{margin:1rem 0 .45rem;font-size:.6rem;font-weight:850;letter-spacing:.04em}.password-label{display:flex;align-items:center;justify-content:space-between}.password-label label{margin-bottom:.45rem}.password-label a{font-size:.57rem;font-weight:750}.auth-input{display:grid;grid-template-columns:1rem 1fr;align-items:center;gap:.65rem;padding:.8rem .9rem;border:1px solid var(--wf-border);background:var(--wf-surface-raised)}.auth-input:focus-within{border-color:#176b4a;box-shadow:0 0 0 3px rgba(23,107,74,.12)}.auth-input :global(svg){width:1rem;color:var(--wf-text-muted)}.auth-input input{min-width:0;background:transparent;font-size:.72rem;outline:none}.submit-button{display:grid;grid-template-columns:1rem 1fr 1rem;align-items:center;gap:.5rem;min-height:3.2rem;margin-top:1.3rem;padding:0 1rem;border-radius:999px;color:#17221c;background:#c7f464;font-size:.68rem;font-weight:850}.submit-button :global(svg){width:.9rem}.create-account{margin-top:1.4rem;color:var(--wf-text-muted);font-size:.63rem;text-align:center}.create-account a{color:var(--wf-text);font-weight:800}.demo-panel{margin-top:2rem;padding-top:1.2rem;border-top:1px solid var(--wf-border)}.demo-panel>div:first-child{display:flex;align-items:center;justify-content:space-between;color:var(--wf-text-muted);font-size:.55rem;font-weight:800;text-transform:uppercase}.demo-panel>div:first-child button{font-size:.55rem}.demo-accounts{display:grid;grid-template-columns:1fr 1fr;gap:.5rem;margin-top:.65rem}.demo-accounts button{display:flex;flex-direction:column;padding:.7rem;border:1px solid var(--wf-border);background:var(--wf-surface-raised);text-align:left}.demo-accounts strong{font-size:.57rem}.demo-accounts small{margin-top:.2rem;overflow:hidden;color:var(--wf-text-muted);font-size:.5rem;text-overflow:ellipsis}.auth-form-side footer{display:flex;justify-content:space-between;color:var(--wf-text-muted);font-size:.52rem}.auth-form-side footer a{font-weight:800}
  .auth-error{white-space:pre-line}.auth-notice{margin-bottom:1rem;padding:.75rem;border-left:3px solid #176b4a;color:#145c40;background:#e8f5ee;font-size:.65rem}.verification-button,.mfa-back{width:max-content;color:#176b4a;font-size:.6rem;font-weight:800;text-decoration:underline}.mfa-back{margin-top:.7rem}.field-error{margin-top:.35rem;color:#b42318;font-size:.58rem}
  @media(max-width:900px){.auth-page{grid-template-columns:1fr}.auth-story{display:none}.auth-form-side{padding:1.3rem}.mobile-brand{display:block}.mobile-brand a{font-weight:850}.auth-form-wrap{margin:5rem auto}.auth-form-side footer{margin-top:auto}}
</style>
