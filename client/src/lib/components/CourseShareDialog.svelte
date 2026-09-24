<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { Share2, Copy, X, Calendar, Lock } from 'lucide-svelte';

  export let isOpen = false;
  export let courseId = '';
  export let courseTitle = '';

  const dispatch = createEventDispatcher();

  let shareUrl = '';
  let shareCode = '';
  let expiresAt = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);
  let customMessage = '';
  let copied = false;

  function handleGenerateShareLink() {
    // In real implementation, call API to generate share link
    shareCode = Math.random().toString(36).substring(2, 10).toUpperCase();
    shareUrl = `${window.location.origin}/courses/join/${shareCode}`;
  }

  function handleCopyLink() {
    navigator.clipboard.writeText(shareUrl);
    copied = true;
    setTimeout(() => copied = false, 2000);
  }

  function handleClose() {
    isOpen = false;
    shareUrl = '';
    shareCode = '';
    dispatch('close');
  }
</script>

{#if isOpen}
  <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-50" on:click={handleClose}>
    <div
      class="w-full max-w-md bg-ifa-card rounded-2xl border border-ifa-border shadow-2xl overflow-hidden"
      on:click|stopPropagation={() => {}}
    >
      <!-- Header -->
      <div class="flex items-center justify-between px-6 py-4 border-b border-ifa-border">
        <div class="flex items-center gap-2">
          <Share2 class="w-5 h-5 text-ifa-pine" />
          <h3 class="text-lg font-bold text-ifa-text-primary">Share Course</h3>
        </div>
        <button
          type="button"
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

        {#if shareUrl}
          <div class="bg-ifa-card-muted rounded-lg p-4">
            <p class="text-xs text-ifa-text-secondary mb-2">Share Link</p>
            <div class="flex items-center gap-2">
              <input
                type="text"
                readonly
                value={shareUrl}
                class="flex-1 px-3 py-2 bg-white border border-ifa-border rounded-lg text-sm text-ifa-text-primary"
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
          <div>
            <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Expiration Date</label>
            <div class="flex items-center gap-2 px-4 py-2.5 bg-ifa-card-muted border border-ifa-border rounded-lg">
              <Calendar class="w-4 h-4 text-ifa-text-muted" />
              <input
                type="date"
                bind:value={expiresAt}
                class="flex-1 bg-transparent text-sm text-ifa-text-primary focus:outline-none"
              />
            </div>
          </div>

          <div>
            <label class="block text-sm font-semibold text-ifa-text-primary mb-2">Message (Optional)</label>
            <textarea
              bind:value={customMessage}
              placeholder="Add a personal message..."
              class="w-full px-4 py-2.5 bg-ifa-card-muted border border-ifa-border rounded-lg text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine resize-none"
              rows="3"
            ></textarea>
          </div>

          <button
            type="button"
            on:click={handleGenerateShareLink}
            class="w-full py-2.5 bg-ifa-pine text-white rounded-xl text-sm font-semibold flex items-center justify-center gap-2 hover:bg-emerald-800 transition"
          >
            <Share2 class="w-4 h-4" />
            <span>Generate Share Link</span>
          </button>
        {/if}

        <div class="flex items-center gap-2 text-xs text-ifa-text-muted">
          <Lock class="w-3 h-3" />
          <span>Share links expire after 7 days for security</span>
        </div>
      </div>
    </div>
  </div>
{/if}
