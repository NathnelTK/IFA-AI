<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { Share2, Copy, X, Lock, Loader2 } from 'lucide-svelte';
  import { coursesApi } from '$lib/api';

  export let isOpen = false;
  export let courseId = '';
  export let courseTitle = '';

  const dispatch = createEventDispatcher();

  let shareUrl = '';
  let shareCode = '';
  let copied = false;
  let generating = false;
  let error = '';
  let dialog: HTMLDialogElement;

  $: if (dialog) {
    if (isOpen && !dialog.open) dialog.showModal();
    else if (!isOpen && dialog.open) dialog.close();
  }

  async function handleGenerateShareLink() {
    if (!courseId) {
      error = 'This course cannot be shared yet.';
      return;
    }
    generating = true;
    error = '';
    const requestedCourse = courseId;
    try {
      // Ask the backend for a real, persisted share code, then build the
      // sendable URL from the current origin.
      const result = await coursesApi.share(courseId);
      if (!isOpen || requestedCourse !== courseId) return;
      shareCode = result.shareCode;
      const origin = typeof window !== 'undefined' ? window.location.origin : '';
      shareUrl = `${origin}/courses/join/${shareCode}`;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not create a share link.';
    } finally {
      generating = false;
    }
  }

  async function handleCopyLink() {
    try {
      await navigator.clipboard.writeText(shareUrl);
      copied = true;
      setTimeout(() => (copied = false), 2000);
    } catch {
      error = 'Clipboard access is unavailable. Select the link field to copy it manually.';
    }
  }

  function handleClose() {
    isOpen = false;
    shareUrl = '';
    shareCode = '';
    error = '';
    dispatch('close');
  }
</script>

    <dialog bind:this={dialog} on:cancel|preventDefault={handleClose} on:close={handleClose} aria-labelledby="share-course-title" class="w-[calc(100%_-_2rem)] max-w-md bg-ifa-card text-ifa-text-primary rounded-2xl border border-ifa-border shadow-2xl p-0">
      <!-- Header -->
      <div class="flex items-center justify-between px-6 py-4 border-b border-ifa-border">
        <div class="flex items-center gap-2">
          <Share2 class="w-5 h-5 text-ifa-pine" />
          <h3 id="share-course-title" class="text-lg font-bold text-ifa-text-primary">Share Course</h3>
        </div>
        <button
          type="button"
          aria-label="Close share dialog"
          on:click={handleClose}
          class="p-2 text-ifa-text-muted hover:text-ifa-text-primary transition"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Content -->
      <div class="p-6 space-y-4">
        <div>
          <p class="text-sm text-ifa-text-secondary mb-1">Course</p>
          <p class="text-sm font-semibold text-ifa-text-primary">{courseTitle}</p>
        </div>

        {#if error}
          <p role="alert" class="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{error}</p>
        {/if}

        {#if shareUrl}
          <div class="bg-ifa-card-muted rounded-lg p-4">
            <p class="text-xs text-ifa-text-secondary mb-2">Share Link</p>
            <div class="flex items-center gap-2">
              <input
                type="text"
                aria-label="Share link"
                readonly
                value={shareUrl}
                class="min-w-0 flex-1 px-3 py-2 bg-ifa-card border border-ifa-border rounded-lg text-sm text-ifa-text-primary"
              />
              <button
                type="button"
                on:click={handleCopyLink}
                class="px-3 py-2 bg-ifa-pine text-white rounded-lg text-sm font-semibold flex items-center gap-2 hover:bg-emerald-800 transition"
              >
                {#if copied}
                  <span>Copied!</span>
                {:else}
                  <Copy class="w-4 h-4" />
                  <span>Copy</span>
                {/if}
              </button>
            </div>
            <p class="text-xs text-ifa-text-muted mt-2">Share Code: <span class="font-mono font-semibold">{shareCode}</span></p>
          </div>
        {:else}
          <button
            type="button"
            on:click={handleGenerateShareLink}
            disabled={generating}
            class="w-full py-2.5 bg-ifa-pine text-white rounded-xl text-sm font-semibold flex items-center justify-center gap-2 hover:bg-emerald-800 transition disabled:opacity-60"
          >
            {#if generating}
              <Loader2 class="w-4 h-4 animate-spin" />
              <span>Generating…</span>
            {:else}
              <Share2 class="w-4 h-4" />
              <span>Generate Share Link</span>
            {/if}
          </button>
        {/if}

        <div class="flex items-center gap-2 text-xs text-ifa-text-muted">
          <Lock class="w-3 h-3" />
          <span>Enrollment still follows the course's access, approval, and payment rules. Only published courses can be shared.</span>
        </div>
      </div>
    </dialog>

<style>
  dialog::backdrop { background: rgb(0 0 0 / 0.5); }
</style>
