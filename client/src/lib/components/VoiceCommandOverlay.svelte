<script lang="ts">
  import { Mic, MicOff, X, Volume2, Loader2 } from 'lucide-svelte';

  export let isActive = false;
  export let onClose = () => {};
  export let onVoiceInput = (text: string) => {};

  let isListening = false;
  let isProcessing = false;
  let recognizedText = '';
  let audioLevel = 0;

  function toggleListening() {
    isListening = !isListening;
    if (isListening) {
      // Simulate voice recognition
      simulateVoiceRecognition();
    }
  }

  function simulateVoiceRecognition() {
    const phrases = [
      'Show my weak areas',
      'Continue with REST APIs',
      'Compare my progress with Ermiyas',
      'Practice SQL joins',
      'Generate a new course for Python'
    ];

    let progress = 0;
    const interval = setInterval(() => {
      if (!isListening) {
        clearInterval(interval);
        return;
      }

      progress += Math.random() * 15;
      audioLevel = Math.random() * 100;

      if (progress >= 100) {
        clearInterval(interval);
        recognizedText = phrases[Math.floor(Math.random() * phrases.length)];
        isListening = false;
        isProcessing = true;
        audioLevel = 0;

        setTimeout(() => {
          onVoiceInput(recognizedText);
          isProcessing = false;
          recognizedText = '';
        }, 1500);
      }
    }, 200);
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
            {:else if isProcessing}
              <Loader2 class="w-5 h-5 animate-spin" />
            {:else}
              <MicOff class="w-5 h-5" />
            {/if}
          </div>
          <div>
            <h3 class="text-sm font-bold text-ifa-text-primary">Voice Command</h3>
            <p class="text-xs text-ifa-text-secondary">
              {#if isListening}
                Listening...
              {:else if isProcessing}
                Processing...
              {:else}
                Ready to listen
              {/if}
            </p>
          </div>
        </div>
        <button
          type="button"
          on:click={onClose}
          class="text-ifa-text-muted hover:text-ifa-pine transition"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Audio Visualizer -->
      <div class="flex items-center justify-center gap-1 mb-6 h-16">
        {#each Array(20) as _}
          <div
            class="w-1 bg-ifa-pine rounded-full transition-all duration-100"
            style="height: {isListening ? Math.random() * 100 : 10}%"
          ></div>
        {/each}
      </div>

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
          disabled={isProcessing}
          class="flex-1 py-3 rounded-xl {isListening
            ? 'bg-red-100 text-red-700 border border-red-200'
            : 'bg-ifa-pine text-white hover:bg-emerald-800'} transition flex items-center justify-center gap-2 text-sm font-semibold disabled:opacity-50 disabled:cursor-not-allowed"
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
            audioLevel = 0;
          }}
          disabled={isProcessing}
          class="px-4 py-3 rounded-xl border border-ifa-border bg-ifa-card-muted hover:bg-white transition text-sm font-semibold text-ifa-text-secondary disabled:opacity-50 disabled:cursor-not-allowed"
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
