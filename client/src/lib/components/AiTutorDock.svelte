<script lang="ts">
  import { ArrowRight, Sparkles, X, Mic, MicOff, Loader2, Navigation, Compass } from 'lucide-svelte';
  import { voxideEnabled, voxideStatus, voxideMessages, toggleVoxide } from '$lib/voxide';

  /**
   * Voxide voice console — navigation + common actions, not a chat.
   *
   * The dock used to run a text intake chat that scoped a course. Chat is gone:
   * Voxide is voice-only and drives the app through the capabilities registered
   * in `$lib/voxide` (navigate to a section, open the tutor, create a course).
   * The live transcript below is read back from the Voxide snapshot — the user
   * cannot type here. Course *scoping* still lives in the hero on /home.
   */

  let isOpen = false;
  let error = '';

  // Voice session state (Voxide). "listening/thinking/speaking/executing" mean a
  // live voice session is active and controlling the app.
  $: voiceActive = ['armed', 'connecting', 'listening', 'thinking', 'speaking', 'executing'].includes(
    $voxideStatus
  );
  $: voiceBusy = ['connecting', 'thinking', 'executing'].includes($voxideStatus);

  function voiceStatusLabel(status: string): string {
    switch (status) {
      case 'listening': return 'Listening…';
      case 'thinking': return 'Thinking…';
      case 'speaking': return 'Speaking…';
      case 'executing': return 'Doing it…';
      case 'connecting': return 'Connecting…';
      case 'armed': return 'Ready';
      case 'error': return 'Voice error';
      case 'disabled': return 'Voice needs a key';
      default: return 'Tap to talk';
    }
  }

  async function handleMic() {
    if (!voxideEnabled) {
      error = 'Voice needs a Voxide publishable key. Set VITE_VOXIDE_KEY (vox_pub_…) in client/.env.';
      return;
    }
    error = '';
    await toggleVoxide();
  }

  // What Voxide can do. Rendered as guidance (voice-first — no typed commands
  // are sent from here); the actual handlers live in `$lib/voxide`.
  const commands: Array<{ group: string; icon: typeof Navigation; items: string[] }> = [
    {
      group: 'Go to',
      icon: Navigation,
      items: ['My Courses', 'Marketplace', 'Recommendations', 'My Skills', 'Progress', 'Research', 'Settings']
    },
    {
      group: 'Do',
      icon: Compass,
      items: ['Create a course on …', 'Open the tutor for …']
    }
  ];
</script>

<!-- Docked Widget -->
{#if !isOpen}
  <div class="relative overflow-hidden rounded-2xl bg-gradient-to-r from-[#12161A] via-[#161D24] to-[#12161A] p-4 text-white shadow-elevated border border-white/10 flex items-center justify-between gap-4">
    <div class="flex items-center gap-3">
      <div class="relative w-10 h-10 rounded-full flex items-center justify-center shrink-0">
        <div class="absolute inset-0 rounded-full bg-cyan-500/30 blur-md animate-pulse"></div>
        <div class="w-8 h-8 rounded-full bg-gradient-to-tr from-cyan-600 to-blue-500 flex items-center justify-center shadow-inner">
          <Sparkles class="w-4 h-4 text-white" />
        </div>
      </div>

      <div>
        <div class="flex items-center gap-1.5 text-xs font-bold text-white tracking-tight">
          <span class="text-cyan-400 font-mono text-[10px]">•||•</span>
          <span>IFA Voice Assistant</span>
        </div>
        <p class="text-[10px] text-gray-300">Navigate &amp; act — just speak.</p>
      </div>
    </div>

    <button
      type="button"
      on:click={() => (isOpen = true)}
      class="px-3 py-1.5 rounded-full bg-white/10 hover:bg-white/20 border border-white/15 text-xs font-semibold text-white transition flex items-center gap-1.5 shrink-0 hover:scale-105"
    >
      <span>Speak</span>
      <ArrowRight class="w-3 h-3" />
    </button>
  </div>
{:else}
  <div class="fixed bottom-6 right-6 w-96 max-w-[calc(100vw-2rem)] rounded-3xl bg-ifa-card border border-ifa-border shadow-elevated z-50 overflow-hidden flex flex-col h-[560px] transition-all animate-in fade-in slide-in-from-bottom-5 duration-200">
    <!-- Header -->
    <div class="p-4 bg-gradient-to-r from-[#12161A] to-[#1E293B] text-white flex items-center justify-between">
      <div class="flex items-center gap-2.5">
        <div class="w-8 h-8 rounded-full bg-cyan-500/20 flex items-center justify-center text-cyan-400">
          <Sparkles class="w-4 h-4" />
        </div>
        <div>
          <h4 class="text-xs font-bold leading-tight">IFA Voice Assistant</h4>
          <p class="text-[10px] text-cyan-300 font-mono">
            {voiceActive ? voiceStatusLabel($voxideStatus) : 'Voice commands · navigate & act'}
          </p>
        </div>
      </div>
      <div class="flex items-center gap-1">
        <button
          type="button"
          on:click={handleMic}
          title={voxideEnabled ? 'Talk to IFA (voice)' : 'Voice needs a Voxide key'}
          class="w-7 h-7 rounded-full flex items-center justify-center transition {voiceActive
            ? 'bg-cyan-500 text-white animate-pulse'
            : 'bg-white/10 text-cyan-400 hover:bg-white/20'}"
        >
          {#if voiceBusy}
            <Loader2 class="w-3.5 h-3.5 animate-spin" />
          {:else if voiceActive}
            <MicOff class="w-3.5 h-3.5" />
          {:else}
            <Mic class="w-3.5 h-3.5" />
          {/if}
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

    <!-- Voice transcript + what Voxide can do -->
    <div class="flex-1 p-4 overflow-y-auto space-y-3 bg-ifa-bg/50 text-xs">
      {#if voiceActive || $voxideMessages.length > 0}
        <div class="rounded-2xl border border-cyan-200 bg-cyan-50/70 p-3 space-y-2">
          <div class="flex items-center gap-1.5 text-[10px] font-bold text-cyan-800 uppercase tracking-wide">
            <Mic class="w-3 h-3" /> Voice · {voiceStatusLabel($voxideStatus)}
          </div>
          {#if $voxideMessages.length > 0}
            {#each $voxideMessages.slice(-6) as vm}
              <p class="text-[11px] {vm.role === 'user' ? 'text-ifa-text-primary font-semibold' : 'text-ifa-text-secondary'}">
                <span class="opacity-60">{vm.role === 'user' ? 'You' : 'IFA'}:</span> {vm.content}
              </p>
            {/each}
          {:else}
            <p class="text-[11px] text-cyan-900">
              Listening for a command — say something like “go to my courses” or “open the tutor for recursion”.
            </p>
          {/if}
        </div>
      {/if}

      <!-- Command reference: this dock is for navigating and acting, not chatting. -->
      <div class="rounded-2xl border border-ifa-border bg-white p-3 space-y-3">
        <p class="text-[10px] font-bold text-ifa-text-muted uppercase tracking-wide">
          What I can do by voice
        </p>
        {#each commands as cmd}
          <div class="space-y-1.5">
            <div class="flex items-center gap-1.5 text-[10px] font-bold text-ifa-text-secondary">
              <svelte:component this={cmd.icon} class="w-3 h-3 text-cyan-600" />
              {cmd.group}
            </div>
            <div class="flex flex-wrap gap-1.5">
              {#each cmd.items as item}
                <span class="text-[10px] px-2 py-0.5 rounded-full bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary">
                  {item}
                </span>
              {/each}
            </div>
          </div>
        {/each}
        <p class="text-[10px] text-ifa-text-muted leading-relaxed">
          Voxide handles navigation and quick actions. To scope and generate a new
          course, use the advisor on the home page.
        </p>
      </div>

      {#if error}
        <p role="alert" class="text-[11px] text-red-600">{error}</p>
      {/if}
    </div>

    <!-- Voice control footer (no text input — this assistant is voice-only) -->
    <div class="p-3 bg-white border-t border-ifa-border">
      <button
        type="button"
        on:click={handleMic}
        disabled={voiceBusy}
        class="w-full py-2.5 rounded-xl text-xs font-bold transition flex items-center justify-center gap-2 disabled:opacity-60 {voiceActive
          ? 'bg-cyan-600 text-white hover:bg-cyan-700'
          : 'bg-ifa-pine text-white hover:bg-ifa-pine-light'}"
      >
        {#if voiceBusy}
          <Loader2 class="w-4 h-4 animate-spin" />
          <span>{voiceStatusLabel($voxideStatus)}</span>
        {:else if voiceActive}
          <MicOff class="w-4 h-4" />
          <span>Stop listening</span>
        {:else}
          <Mic class="w-4 h-4" />
          <span>Talk to IFA</span>
        {/if}
      </button>
      {#if !voxideEnabled}
        <p class="text-[10px] text-ifa-text-muted text-center mt-2">
          Add a Voxide publishable key to enable voice.
        </p>
      {/if}
    </div>
  </div>
{/if}
