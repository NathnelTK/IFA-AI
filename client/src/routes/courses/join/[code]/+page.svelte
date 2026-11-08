<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { CheckCircle, AlertCircle, Loader2, ArrowRight, UserPlus } from 'lucide-svelte';
  import { apiErrorMessage, coursesApi, paymentsApi, type CourseInvitePreviewDto } from '$lib/api';
  import { initSession, session } from '$lib/stores/sessionStore';

  $: code = $page.params.code ?? '';

  let loading = true;
  let error = false;
  let errorMessage = '';
  let courseInfo: CourseInvitePreviewDto | null = null;
  let enrolling = false;
  let authReady = false;
  let requestedCode = '';
  let enrollmentStatus = '';

  onMount(async () => {
    // Establish the session first so we know whether to render the enroll
    // button or the "create an account" CTA.
    await initSession().catch(() => {});
    authReady = true;
  });

  $: if (authReady && code && code !== requestedCode) {
    requestedCode = code;
    void loadPreview();
  }

  async function loadPreview() {
    loading = true; error = false; errorMessage = ''; courseInfo = null; enrollmentStatus = '';
    const requested = code;
    try {
      const course = await coursesApi.previewJoin(requested);
      if (code === requested) courseInfo = course;
    } catch (cause) {
      if (code === requested) { error = true; errorMessage = apiErrorMessage(cause, 'This invitation is unavailable.'); }
    } finally {
      if (code === requested) loading = false;
    }
  }

  async function handleEnroll() {
    if (enrolling || !courseInfo || !$session.authenticated || $session.role !== 'Learner') return;
    enrolling = true;
    errorMessage = '';
    try {
      const result = await coursesApi.join(code);
      // Paid courses answer with { paymentRequired: true } instead of enrolling —
      // send the learner to checkout.
      enrollmentStatus = result.status;
      if (result.paymentRequired) {
        const payment = await paymentsApi.initialize(courseInfo.id, `${window.location.origin}/marketplace`);
        if (payment.enrollmentStatus === 'PendingApproval') {
          enrollmentStatus = payment.enrollmentStatus;
          return;
        }
        if (!payment.checkoutUrl) throw new Error('The payment provider did not return a checkout URL.');
        window.location.assign(payment.checkoutUrl);
        return;
      }
      if (result.status === 'PendingApproval') {
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
    {:else if error}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-8 text-center">
        <AlertCircle class="w-12 h-12 text-red-500 mx-auto mb-4" />
        <h2 class="text-xl font-bold text-ifa-text-primary mb-2">Invitation Unavailable</h2>
        <p role="alert" class="text-ifa-text-secondary mb-6">{errorMessage}</p>
        <a
          href="/marketplace"
          class="inline-flex items-center gap-2 px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold hover:bg-emerald-800 transition"
        >
          <span>Browse Courses</span>
          <ArrowRight class="w-4 h-4" />
        </a>
        <button type="button" on:click={loadPreview} class="ml-3 text-sm text-ifa-pine underline">Retry</button>
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
            {courseInfo?.description ?? 'Someone shared a Witforge course with you. Create an account or sign in to request access and start learning.'}
          </p>
          {#if courseInfo}
            <div class="flex items-center gap-4 text-xs text-ifa-text-muted">
              <span>{courseInfo.moduleCount} modules</span>
              <span>•</span>
              <span>{courseInfo.estimatedDuration}</span>
            </div>
            {#if courseInfo.isPaid}<p class="mt-3 text-sm font-semibold text-ifa-text-primary">{courseInfo.currency} {courseInfo.priceAmount} through secure checkout</p>{/if}
            {#if courseInfo.expiresAt}<p class="mt-2 text-xs text-ifa-text-muted">Invitation expires {new Date(courseInfo.expiresAt).toLocaleString()}.</p>{/if}
          {/if}
        </div>

        {#if errorMessage}
          <p role="alert" class="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{errorMessage}</p>
        {/if}

        {#if enrollmentStatus === 'PendingApproval'}
          <p role="status" class="rounded-xl border border-ifa-border bg-ifa-card-muted p-4 text-sm text-ifa-text-primary">Enrollment requested. Your educator must approve it before course content becomes available.</p>
        {:else if $session.authenticated && $session.role !== 'Learner'}
          <p class="text-sm text-ifa-text-secondary">Enrollment is for learner accounts. Educators can preview their saved courses in Studio.</p>
        {:else if $session.authenticated}
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
              <span>{courseInfo?.isPaid ? 'Continue to Checkout' : courseInfo?.enrollmentMode === 'ApprovalRequired' ? 'Request Enrollment' : 'Enroll Now'}</span>
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
          By enrolling, you agree to Witforge's terms of service
        </p>
      </div>
    {/if}
  </div>
</div>
