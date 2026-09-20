<!--
  =============================================================================
  ASSIGNED TO FRONTEND TEAM (Ermiyas / Frontend Lead)
  =============================================================================
  RESPONSIBILITY:
  Interactive Voice Input button & visualizer utilizing the Voxide Voice Engine.
  Allows the user to speak their learning goals or commands (e.g. "I want to learn C#",
  "Take me to my skill profile").

  STEP-BY-STEP IMPLEMENTATION INSTRUCTIONS:
  1. Audio Capture:
     - On mic button click, request navigator.mediaDevices.getUserMedia({ audio: true }).
     - Instantiate MediaRecorder or Web Audio API AnalyserNode for live amplitude feedback.
  2. Streaming / Transcription:
     - When recording completes, convert the audio blob into base64 or pass to
       /api/voice/intent via POST { audioBase64OrTranscript, language: 'en' }.
  3. Dispatch Event:
     - Dispatch 'intentDetected' event with { intentName, targetTopic, transcript }
       so parent HeroSection or AiTutorDock can respond immediately.
  4. Amharic Support:
     - Allow toggling language between 'en' (English) and 'am' (Amharic) for Ethiopian
       context evaluation.
  =============================================================================
-->

<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { Mic, MicOff, Loader2 } from 'lucide-svelte';

  export let isListening = false;
  export let language: 'en' | 'am' = 'en';

  const dispatch = createEventDispatcher<{
    intentDetected: { intentName: string; targetTopic: string; rawTranscript: string };
    transcriptionStarted: void;
    transcriptionEnded: void;
  }>();

  let isProcessing = false;
  let simulatedTranscript = '';

  async function toggleVoiceRecording() {
    if (isListening) {
      // Stop recording and process
      isListening = false;
      isProcessing = true;
      dispatch('transcriptionEnded');

      try {
        // TODO (Frontend Team): Replace with live audio recorder blob upload.
        // For hackathon mock / testing, call backend intent endpoint:
        const response = await fetch('/api/voice/intent', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            audioBase64OrTranscript: simulatedTranscript || 'I want to learn C# for backend development',
            language
          })
        });

        if (response.ok) {
          const result = await response.json();
          dispatch('intentDetected', result);
        }
      } catch (err) {
        console.warn('Voice intent fallback triggered:', err);
      } finally {
        isProcessing = false;
      }
    } else {
      // Start recording
      isListening = true;
      dispatch('transcriptionStarted');
    }
  }
</script>

<div class="relative inline-flex items-center">
  <button
    type="button"
    on:click={toggleVoiceRecording}
    class="relative p-3 rounded-full transition-all duration-300 flex items-center justify-center {isListening
      ? 'bg-rose-500 text-white shadow-lg shadow-rose-500/30 scale-105 animate-pulse'
      : isProcessing
      ? 'bg-brand-green/20 text-brand-green'
      : 'bg-brand-cream hover:bg-brand-green/10 text-brand-green'}"
    title={isListening ? 'Click to stop listening' : 'Start voice input (Voxide)'}
    aria-label="Voice Input"
  >
    {#if isProcessing}
      <Loader2 class="w-5 h-5 animate-spin" />
    {:else if isListening}
      <MicOff class="w-5 h-5" />
    {:else}
      <Mic class="w-5 h-5" />
    {/if}
  </button>

  {#if isListening}
    <span class="absolute -bottom-6 left-1/2 -translate-x-1/2 text-[10px] font-semibold text-rose-600 uppercase tracking-wider whitespace-nowrap">
      Listening...
    </span>
  {/if}
</div>
