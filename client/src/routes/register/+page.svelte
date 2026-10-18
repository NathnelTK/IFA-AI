<script lang="ts">
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { ArrowRight, Code2, Lock, Mail, User, CheckCircle2, GraduationCap, Loader2 } from 'lucide-svelte';
  import { register } from '$lib/stores/sessionStore';

  // Optional ?redirect=/path — after registration we send the new learner back
  // where they started (e.g. a shared course link or the chat page).
  $: redirectTarget = $page.url.searchParams.get('redirect') || '/home';

  function safeRedirect(target: string): string {
    return target.startsWith('/') && !target.startsWith('//') ? target : '/home';
  }

  let name = '';
  let email = '';
  let password = '';
  let confirm = '';
  let role: 'Learner' | 'Educator' = 'Learner';
  let submitting = false;
  let error = '';

  const roleOptions: Array<{ value: 'Learner' | 'Educator'; title: string; description: string }> = [
    {
      value: 'Learner',
      title: 'Learner',
      description: 'Chat with the AI advisor, take courses and track your skills.'
    },
    {
      value: 'Educator',
      title: 'Educator',
      description: 'Turn your materials into structured courses in the Studio.'
    }
  ];

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
      await register(name.trim(), email.trim(), password, role);
      await goto(safeRedirect(redirectTarget));
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
        <li class="flex items-center gap-2"><CheckCircle2 class="w-4 h-4 text-ifa-pine" /> Free, adaptive course generation</li>
        <li class="flex items-center gap-2"><CheckCircle2 class="w-4 h-4 text-ifa-pine" /> Module quizzes and progress tracking</li>
        <li class="flex items-center gap-2"><CheckCircle2 class="w-4 h-4 text-ifa-pine" /> Study alongside the community</li>
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
        <!-- Role picker -->
        <div>
          <span class="block text-xs font-semibold text-ifa-text-secondary mb-1.5">I am joining as…</span>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {#each roleOptions as option (option.value)}
              <button
                type="button"
                on:click={() => (role = option.value)}
                aria-pressed={role === option.value}
                class="text-left p-3 rounded-xl border transition {role === option.value
                  ? 'border-ifa-pine bg-ifa-pine/5 ring-1 ring-ifa-pine/30'
                  : 'border-ifa-border bg-ifa-card-muted hover:bg-ifa-border/30'}"
              >
                <span class="flex items-center gap-2 text-sm font-bold {role === option.value ? 'text-ifa-pine' : 'text-ifa-text-primary'}">
                  {#if option.value === 'Educator'}
                    <GraduationCap class="w-4 h-4" />
                  {:else}
                    <User class="w-4 h-4" />
                  {/if}
                  {option.title}
                </span>
                <span class="block text-[11px] text-ifa-text-secondary mt-1 leading-snug">{option.description}</span>
              </button>
            {/each}
          </div>
        </div>

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
        <a
          href={redirectTarget !== '/home' ? `/login?redirect=${encodeURIComponent(redirectTarget)}` : '/login'}
          class="font-semibold text-ifa-pine hover:text-ifa-pine-dark"
        >Sign in</a>
      </p>
    </div>
  </div>
</div>
