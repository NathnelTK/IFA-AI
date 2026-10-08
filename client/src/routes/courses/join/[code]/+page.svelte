<script lang="ts">
  import { onMount } from 'svelte';
  import { CheckCircle, AlertCircle, Loader2, ArrowRight } from 'lucide-svelte';

  export let code: string;

  let loading = true;
  let error = false;
  let success = false;
  let courseInfo: any = null;

  onMount(async () => {
    // Simulate API call to validate share code and get course info
    setTimeout(() => {
      loading = false;
      // In real implementation, validate share code with backend
      if (code.length === 8) {
        success = true;
        courseInfo = {
          title: 'C# Backend Development',
          description: 'Master C# backend development with ASP.NET Core, REST APIs, and database design.',
          modules: 4,
          duration: '8 weeks'
        };
      } else {
        error = true;
      }
    }, 1000);
  });

  function handleEnroll() {
    // In real implementation, call enrollment API
    console.log('Enrolling in course with code:', code);
    success = true;
  }
</script>

<div class="min-h-screen flex items-center justify-center p-6">
  <div class="w-full max-w-md">
    {#if loading}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-8 text-center">
        <Loader2 class="w-8 h-8 text-ifa-pine mx-auto mb-4 animate-spin" />
        <p class="text-ifa-text-secondary">Validating share code...</p>
      </div>
    {:else if error}
      <div class="bg-ifa-card rounded-2xl border border-red-200 p-8 text-center">
        <AlertCircle class="w-12 h-12 text-red-500 mx-auto mb-4" />
        <h2 class="text-xl font-bold text-ifa-text-primary mb-2">Invalid or Expired Link</h2>
        <p class="text-ifa-text-secondary mb-6">This share link is invalid or has expired. Please contact the person who shared it with you.</p>
        <a
          href="/"
          class="inline-flex items-center gap-2 px-4 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold hover:bg-emerald-800 transition"
        >
          <span>Go to IFA</span>
          <ArrowRight class="w-4 h-4" />
        </a>
      </div>
    {:else if success && courseInfo}
      <div class="bg-ifa-card rounded-2xl border border-ifa-border p-8">
        <div class="text-center mb-6">
          <div class="w-16 h-16 rounded-full bg-emerald-100 flex items-center justify-center mx-auto mb-4">
            <CheckCircle class="w-8 h-8 text-emerald-600" />
          </div>
          <h2 class="text-xl font-bold text-ifa-text-primary mb-1">You're Invited!</h2>
          <p class="text-ifa-text-secondary">Join this course and start learning</p>
        </div>

        <div class="bg-ifa-card-muted rounded-xl p-5 mb-6">
          <h3 class="text-lg font-bold text-ifa-text-primary mb-2">{courseInfo.title}</h3>
          <p class="text-sm text-ifa-text-secondary mb-4">{courseInfo.description}</p>
          <div class="flex items-center gap-4 text-xs text-ifa-text-muted">
            <span>{courseInfo.modules} modules</span>
            <span>•</span>
            <span>{courseInfo.duration}</span>
          </div>
        </div>

        <button
          type="button"
          on:click={handleEnroll}
          class="w-full py-3 bg-ifa-pine text-white rounded-xl text-sm font-semibold flex items-center justify-center gap-2 hover:bg-emerald-800 transition"
        >
          <span>Enroll Now</span>
          <ArrowRight class="w-4 h-4" />
        </button>

        <p class="text-xs text-ifa-text-muted text-center mt-4">
          By enrolling, you agree to IFA's terms of service
        </p>
      </div>
    {/if}
  </div>
</div>
