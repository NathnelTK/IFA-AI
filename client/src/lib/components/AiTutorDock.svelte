<script lang="ts">
  import { ArrowRight, Sparkles, Send, X, Mic, Volume2 } from 'lucide-svelte';
  import VoiceCommandOverlay from './VoiceCommandOverlay.svelte';

  let isOpen = false;
  let chatMessage = '';
  let isThinking = false;
  let isSpeaking = false;
  let showVoiceOverlay = false;

  let messages = [
    {
      sender: 'ifa',
      text: 'Hello Nathnel! I am your IFA AI tutor. How can I help you master your C# Backend or exam prep today?'
    }
  ];

  function sendMessage() {
    if (!chatMessage.trim()) return;
    const userText = chatMessage;
    messages = [...messages, { sender: 'user', text: userText }];
    chatMessage = '';
    isThinking = true;

    setTimeout(() => {
      isThinking = false;
      let reply = 'In ASP.NET Core, Minimal APIs provide a lightweight approach to building HTTP APIs with minimal overhead. When using JWT authentication, the `UseAuthentication()` and `UseAuthorization()` middleware validate the bearer token on incoming requests before executing your endpoint handler.';
      if (userText.toLowerCase().includes('exit exam') || userText.toLowerCase().includes('cs')) {
        reply = 'For your Computer Science exit exam, make sure to thoroughly review: 1) REST API constraints, 2) SQL indexing & normalization, 3) ACID properties, and 4) Clean/Onion architecture principles. Would you like a 2-minute practice quiz?';
      }
      messages = [...messages, { sender: 'ifa', text: reply }];
    }, 1200);
  }

  function handleVoiceInput(text: string) {
    chatMessage = text;
    sendMessage();
  }
</script>

<!-- Docked Widget (Matches ifa.png bottom right) -->
{#if !isOpen}
  <div class="relative overflow-hidden rounded-2xl bg-gradient-to-r from-[#12161A] via-[#161D24] to-[#12161A] p-4 text-white shadow-elevated border border-white/10 flex items-center justify-between gap-4">
    <!-- Glowing orb & audio visualizer -->
    <div class="flex items-center gap-3">
      <div class="relative w-10 h-10 rounded-full flex items-center justify-center shrink-0">
        <!-- Pulse glow effects -->
        <div class="absolute inset-0 rounded-full bg-cyan-500/30 blur-md animate-pulse"></div>
        <div class="w-8 h-8 rounded-full bg-gradient-to-tr from-cyan-600 to-blue-500 flex items-center justify-center shadow-inner">
          <Sparkles class="w-4 h-4 text-white" />
        </div>
      </div>

      <div>
        <div class="flex items-center gap-1.5 text-xs font-bold text-white tracking-tight">
          <span class="text-cyan-400 font-mono text-[10px]">•||•</span>
          <span>AI Tutor</span>
        </div>
        <p class="text-[10px] text-gray-300">Ask IFA anything about your course.</p>
      </div>
    </div>

    <!-- Start Chat Button -->
    <button
      type="button"
      on:click={() => (isOpen = true)}
      class="px-3 py-1.5 rounded-full bg-white/10 hover:bg-white/20 border border-white/15 text-xs font-semibold text-white transition flex items-center gap-1.5 shrink-0 hover:scale-105"
    >
      <span>Start Chat</span>
      <ArrowRight class="w-3 h-3" />
    </button>
  </div>
{:else}
  <!-- Expanded Interactive Chat Slide-Over / Window -->
  <div class="fixed bottom-6 right-6 w-96 max-w-[calc(100vw-2rem)] rounded-3xl bg-ifa-card border border-ifa-border shadow-elevated z-50 overflow-hidden flex flex-col h-[520px] transition-all animate-in fade-in slide-in-from-bottom-5 duration-200">
    <!-- Header -->
    <div class="p-4 bg-gradient-to-r from-[#12161A] to-[#1E293B] text-white flex items-center justify-between">
      <div class="flex items-center gap-2.5">
        <div class="w-8 h-8 rounded-full bg-cyan-500/20 flex items-center justify-center text-cyan-400">
          <Sparkles class="w-4 h-4" />
        </div>
        <div>
          <h4 class="text-xs font-bold leading-tight">IFA Interactive AI Tutor</h4>
          <p class="text-[10px] text-cyan-300 font-mono">Voxide Voice & Free-Tier LLM Active</p>
        </div>
      </div>
      <div class="flex items-center gap-1">
        <button
          type="button"
          on:click={() => showVoiceOverlay = true}
          title="Voice Command"
          class="w-7 h-7 rounded-full bg-white/10 flex items-center justify-center text-cyan-400 hover:bg-white/20"
        >
          <Mic class="w-3.5 h-3.5" />
        </button>
        <button
          type="button"
          on:click={() => (isOpen = false)}
          class="w-7 h-7 rounded-full bg-white/10 flex items-center justify-center text-white hover:bg-white/20"
        >
          <X class="w-3.5 h-3.5" />
        </button>
      </div>
    </div>

    <!-- Message List -->
    <div class="flex-1 p-4 overflow-y-auto space-y-3 bg-ifa-bg/50 text-xs">
      {#each messages as msg}
        <div class="flex {msg.sender === 'user' ? 'justify-end' : 'justify-start'}">
          <div class="max-w-[85%] p-3 rounded-2xl leading-relaxed {msg.sender === 'user'
            ? 'bg-ifa-pine text-white rounded-tr-none'
            : 'bg-white border border-ifa-border text-ifa-text-primary rounded-tl-none shadow-soft'}">
            {msg.text}
          </div>
        </div>
      {/each}

      {#if isThinking}
        <div class="flex justify-start">
          <div class="p-3 rounded-2xl bg-white border border-ifa-border text-ifa-text-muted flex items-center gap-2 shadow-soft">
            <span class="w-1.5 h-1.5 rounded-full bg-cyan-500 animate-bounce"></span>
            <span class="w-1.5 h-1.5 rounded-full bg-cyan-500 animate-bounce [animation-delay:0.2s]"></span>
            <span class="w-1.5 h-1.5 rounded-full bg-cyan-500 animate-bounce [animation-delay:0.4s]"></span>
            <span class="text-[11px] font-mono text-cyan-700">Synthesizing...</span>
          </div>
        </div>
      {/if}
    </div>

    <!-- Input Box -->
    <div class="p-3 bg-white border-t border-ifa-border flex items-center gap-2">
      <input
        type="text"
        bind:value={chatMessage}
        on:keydown={(e) => e.key === 'Enter' && sendMessage()}
        placeholder="Ask a question or enter prompt..."
        class="flex-1 text-xs px-3.5 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      />
      <button
        type="button"
        on:click={sendMessage}
        class="w-9 h-9 rounded-xl bg-ifa-pine text-white flex items-center justify-center hover:bg-ifa-pine-light transition"
      >
        <Send class="w-4 h-4" />
      </button>
    </div>
  </div>
{/if}

<!-- Voice Command Overlay -->
<VoiceCommandOverlay
  isActive={showVoiceOverlay}
  onClose={() => showVoiceOverlay = false}
  onVoiceInput={handleVoiceInput}
/>
