<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { CheckCircle, AlertCircle, Loader2, ArrowRight, UserPlus } from 'lucide-svelte';
  import { coursesApi } from '$lib/api';
  import { initSession, session } from '$lib/stores/sessionStore';

  export let code: string;

  interface PreviewCourse {
    id: string;
    title: string;
    description: string;
    moduleCount: number;
    estimatedDuration: string;
  }

  let loading = true;
  let error = false;
  let errorMessage = '';
  let courseInfo: PreviewCourse | null = null;
  let enrolling = false;
  let authReady = false;

  onMount(async () => {
    // Establish the session first so we know whether to render the enroll
    // button or the "create an account" CTA.
    await initSession().catch(() => {});
    authReady = true;

    try {
      const course = await coursesApi.previewJoin(code);
      courseInfo = {
        id: course.id,
        title: course.title,
        description: course.description || 'Join this course and start learning.',
        moduleCount: course.moduleCount,
        estimatedDuration: course.estimatedDuration
      };
    } catch {
      // The preview endpoint may require auth (or the code may be invalid).
      // Anonymous visitors still get a generic invitation card.
      error = true;
    } finally {
      loading = false;
    }
  });

  async function handleEnroll() {
    if (enrolling) return;
    enrolling = true;
    errorMessage = '';
    try {
      const result = await coursesApi.join(code);
      // Paid courses answer with { paymentRequired: true } instead of enrolling —
      // send the learner to checkout.
      if ((result as { paymentRequired?: boolean; courseId?: string })?.paymentRequired && courseInfo?.id) {
        await goto(`/marketplace?course=${courseInfo.id}`);
        return;
      }
      const targetId = result?.courseId || courseInfo?.id;
      if (targetId) {
        await goto(`/courses/${targetId}`);
      }
    } catch (cause) {
      errorMessage = cause instanceof Error ? cause.message : 'Enrollment failed. Please try again.';
    } finally {
      enrolling = false;
    }
  }
</script>

<div class="min-h-screen flex items-center justify-center p-6">
  <div class="w-full max-w-md">
    {#if loading || !authReady}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-8 text-center">
        <Loader2 class="w-8 h-8 text-ifa-pine mx-auto mb-4 animate-spin" />
        <p class="text-ifa-text-secondary">Validating share code...</p>
      </div>
    {:else if error && $session.authenticated}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-8 text-center">
        <AlertCircle class="w-12 h-12 text-red-500 mx-auto mb-4" />
        <h2 class="text-xl font-bold text-ifa-text-primary mb-2">Invalid or Expired Link</h2>
        <p class="text-ifa-text-secondary mb-6">This share link is invalid or has expired. Please contact the person who shared it with you.</p>
        <a
          href="/home"
          class="inline-flex items-center gap-2 px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold hover:bg-emerald-800 transition"
        >
          <span>Go to IFA</span>
          <ArrowRight class="w-4 h-4" />
        </a>
      </div>
    {:else}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-8">
        <div class="text-center mb-6">
          <div class="w-16 h-16 rounded-full bg-emerald-100 flex items-center justify-center mx-auto mb-4">
            {#if $session.authenticated}
              <CheckCircle class="w-8 h-8 text-emerald-600" />
            {:else}
              <UserPlus class="w-8 h-8 text-emerald-600" />
            {/if}
          </div>
          <h2 class="text-xl font-bold text-ifa-text-primary mb-1">You're Invited!</h2>
          <p class="text-ifa-text-secondary">
            {$session.authenticated ? 'Join this course and start learning' : 'Create a free account to enroll in this course'}
          </p>
        </div>

        <!-- Course preview (real details when the lookup succeeds, generic otherwise) -->
        <div class="bg-ifa-card-muted rounded-xl p-5 mb-6">
          <h3 class="text-lg font-bold text-ifa-text-primary mb-2">
            {courseInfo?.title ?? "You've been invited to a course"}
          </h3>
          <p class="text-sm text-ifa-text-secondary mb-4">
            {courseInfo?.description ?? 'Someone shared an IFA course with you. Create an account (or sign in) to enroll with one click and start learning.'}
          </p>
          {#if courseInfo}
            <div class="flex items-center gap-4 text-xs text-ifa-text-muted">
              <span>{courseInfo.moduleCount} modules</span>
              <span>•</span>
              <span>{courseInfo.estimatedDuration}</span>
            </div>
          {/if}
        </div>

        {#if errorMessage}
          <p role="alert" class="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{errorMessage}</p>
        {/if}

        {#if $session.authenticated}
          <button
            type="button"
            on:click={handleEnroll}
            disabled={enrolling}
            class="w-full py-3 bg-ifa-pine text-white rounded-xl text-sm font-semibold flex items-center justify-center gap-2 hover:bg-emerald-800 transition disabled:opacity-60"
          >
            {#if enrolling}
              <Loader2 class="w-4 h-4 animate-spin" />
              <span>Enrolling…</span>
            {:else}
              <span>Enroll Now</span>
              <ArrowRight class="w-4 h-4" />
            {/if}
          </button>
        {:else}
          <a
            href={`/register?redirect=/courses/join/${encodeURIComponent(code)}`}
            class="w-full py-3 bg-ifa-pine text-white rounded-xl text-sm font-semibold flex items-center justify-center gap-2 hover:bg-emerald-800 transition"
          >
            <UserPlus class="w-4 h-4" />
            <span>Create an account to enroll</span>
          </a>
          <p class="text-xs text-ifa-text-muted text-center mt-3">
            Already have an account?
            <a href={`/login?redirect=/courses/join/${encodeURIComponent(code)}`} class="font-semibold text-ifa-pine hover:underline">Sign in</a>
          </p>
        {/if}

        <p class="text-xs text-ifa-text-muted text-center mt-4">
          By enrolling, you agree to IFA's terms of service
        </p>
      </div>
    {/if}
  </div>
</div>
