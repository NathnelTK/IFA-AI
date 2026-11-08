<script lang="ts">
  import { browser } from '$app/environment';
  import { Mic, MicOff, X } from 'lucide-svelte';

  export let isActive = false;
  export let onClose = () => {};
  export let onVoiceInput = (text: string) => {};

  let isListening = false;
  let recognizedText = '';
  let error = '';
  let recognition: any = null;

  function toggleListening() {
    if (isListening) {
      recognition?.stop();
      isListening = false;
      return;
    }
    error = '';
    if (!browser) return;
    const Recognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (!Recognition) {
      error = 'Voice recognition is not supported by this browser.';
      return;
    }
    recognition = new Recognition();
    recognition.continuous = false;
    recognition.interimResults = false;
    recognition.lang = 'en-US';
    recognition.onresult = (event: any) => {
      const text = event.results?.[0]?.[0]?.transcript?.trim();
      if (text) {
        recognizedText = text;
        isListening = false;
        onVoiceInput(text);
      }
    };
    recognition.onerror = () => {
      error = 'Voice recognition could not complete. You can continue with typed controls.';
      isListening = false;
    };
    recognition.onend = () => { isListening = false; };
    recognition.start();
    isListening = true;
  }

  function close() {
    recognition?.stop();
    isListening = false;
    onClose();
  }
</script>

{#if isActive}
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
    <div class="bg-ifa-card rounded-3xl border border-ifa-border shadow-elevated p-6 w-full max-w-md mx-4">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-full bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
            {#if isListening}
              <Mic class="w-5 h-5 animate-pulse" />
            {:else}
              <MicOff class="w-5 h-5" />
            {/if}
          </div>
          <div>
            <h3 class="text-sm font-bold text-ifa-text-primary">Voice Command</h3>
            <p class="text-xs text-ifa-text-secondary">
              {#if isListening}
                Listening...
              {:else}
                Ready to listen
              {/if}
            </p>
          </div>
        </div>
        <button
          type="button"
          on:click={close}
          aria-label="Close voice command"
          class="text-ifa-text-muted hover:text-ifa-pine transition"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <div class="flex items-center justify-center gap-1 mb-6 h-16" aria-hidden="true">
        {#each Array(20) as _}
          <div
            class="w-1 h-4 bg-ifa-pine rounded-full transition-opacity {isListening ? 'animate-pulse opacity-80' : 'opacity-30'}"
          ></div>
        {/each}
      </div>

      {#if error}
        <p role="alert" class="mb-4 text-center text-xs text-red-600">{error}</p>
      {/if}

      <!-- Recognized Text -->
      {#if recognizedText}
        <div class="bg-ifa-card-muted rounded-xl p-4 mb-4">
          <p class="text-xs text-ifa-text-secondary mb-1">Recognized:</p>
          <p class="text-sm font-semibold text-ifa-text-primary">"{recognizedText}"</p>
        </div>
      {/if}

      <!-- Controls -->
      <div class="flex gap-3">
        <button
          type="button"
          on:click={toggleListening}
          class="flex-1 py-3 rounded-xl {isListening
            ? 'bg-red-100 text-red-700 border border-red-200'
            : 'bg-ifa-pine text-white hover:bg-emerald-800'} transition flex items-center justify-center gap-2 text-sm font-semibold"
        >
          {#if isListening}
            <MicOff class="w-4 h-4" />
            <span>Stop Listening</span>
          {:else}
            <Mic class="w-4 h-4" />
            <span>Start Listening</span>
          {/if}
        </button>

        <button
          type="button"
          on:click={() => {
            isListening = false;
            recognition?.stop();
            recognizedText = '';
            error = '';
          }}
          class="px-4 py-3 rounded-xl border border-ifa-border bg-ifa-card-muted hover:bg-white transition text-sm font-semibold text-ifa-text-secondary"
        >
          Clear
        </button>
      </div>

      <!-- Tips -->
      <div class="mt-4 text-center">
        <p class="text-[10px] text-ifa-text-muted">
          Try: "Show my weak areas", "Continue with REST APIs", "Compare progress"
        </p>
      </div>
    </div>
  </div>
{/if}
