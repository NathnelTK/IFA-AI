<script lang="ts">
  import { ArrowRight, Sparkles, Send, X, Mic, MicOff, Loader2 } from 'lucide-svelte';
  import { goto } from '$app/navigation';
  import { aiApi, coursesApi, type IntakeProfileDto } from '$lib/api';
  import { voxideEnabled, voxideStatus, voxideMessages, toggleVoxide } from '$lib/voxide';

  interface ChatMsg {
    role: 'user' | 'assistant';
    content: string;
  }

  let isOpen = false;
  let chatMessage = '';
  let isThinking = false;
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
      error = 'Voice needs a Voxide publishable key. Set PUBLIC_VOXIDE_KEY (vox_pub_…) in client/.env.';
      return;
    }
    error = '';
    await toggleVoxide();
  }

  // Pipeline stage 1 — the learning advisor collects what the learner wants so
  // the research and generation stages have a profile to work from.
  let messages: ChatMsg[] = [
    {
      role: 'assistant',
      content:
        "Hi! I'm your IFA learning advisor. Tell me what you want to learn or build, and I'll scope a personalized course for you."
    }
  ];

  let profileReady = false;
  let profile: IntakeProfileDto | null = null;
  let suggestedTopics: string[] = [];
  let generating = false;

  async function sendMessage() {
    const text = chatMessage.trim();
    if (!text || isThinking) return;

    const history = messages.map((m) => ({ role: m.role, content: m.content }));
    messages = [...messages, { role: 'user', content: text }];
    chatMessage = '';
    error = '';
    isThinking = true;

    try {
      const response = await aiApi.intakeMessage(text, history);
      const reply = response.reply?.trim() || 'Could you add a little more detail so I can help?';
      messages = [...messages, { role: 'assistant', content: reply }];
      if (response.isProfileReady) {
        profileReady = true;
        profile = response.profile ?? null;
        suggestedTopics = response.suggestedResearchTopics ?? [];
      }
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The AI advisor is unavailable.';
      messages = [
        ...messages,
        {
          role: 'assistant',
          content: "Sorry, I couldn't reach the AI advisor just now. Please try again in a moment."
        }
      ];
    } finally {
      isThinking = false;
    }
  }

  async function generateCourse() {
    if (!profile || generating) return;
    generating = true;
    error = '';
    try {
      const preferredCreator = profile.preferredYouTubeChannels?.[0] || 'freeCodeCamp';
      const course = await coursesApi.create({
        goal: profile.learningGoal,
        hoursPerWeek: profile.weeklyStudyHours,
        preferredCreator
      });
      await goto(`/courses/${course.id}`);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not generate the course.';
    } finally {
      generating = false;
    }
  }
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
          <span>IFA Learning Advisor</span>
        </div>
        <p class="text-[10px] text-gray-300">Tell IFA what you want to learn.</p>
      </div>
    </div>

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
  <div class="fixed bottom-6 right-6 w-96 max-w-[calc(100vw-2rem)] rounded-3xl bg-ifa-card border border-ifa-border shadow-elevated z-50 overflow-hidden flex flex-col h-[560px] transition-all animate-in fade-in slide-in-from-bottom-5 duration-200">
    <!-- Header -->
    <div class="p-4 bg-gradient-to-r from-[#12161A] to-[#1E293B] text-white flex items-center justify-between">
      <div class="flex items-center gap-2.5">
        <div class="w-8 h-8 rounded-full bg-cyan-500/20 flex items-center justify-center text-cyan-400">
          <Sparkles class="w-4 h-4" />
        </div>
        <div>
          <h4 class="text-xs font-bold leading-tight">IFA Learning Advisor</h4>
          <p class="text-[10px] text-cyan-300 font-mono">
            {voiceActive ? voiceStatusLabel($voxideStatus) : 'Voice + text · ask or type'}
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

    <!-- Message List -->
    <div class="flex-1 p-4 overflow-y-auto space-y-3 bg-ifa-bg/50 text-xs">
      {#if voiceActive || $voxideMessages.length > 0}
        <div class="rounded-2xl border border-cyan-200 bg-cyan-50/70 p-3 space-y-2">
          <div class="flex items-center gap-1.5 text-[10px] font-bold text-cyan-800 uppercase tracking-wide">
            <Mic class="w-3 h-3" /> Voice · {voiceStatusLabel($voxideStatus)}
          </div>
          {#if $voxideMessages.length > 0}
            {#each $voxideMessages.slice(-4) as vm}
              <p class="text-[11px] {vm.role === 'user' ? 'text-ifa-text-primary font-semibold' : 'text-ifa-text-secondary'}">
                <span class="opacity-60">{vm.role === 'user' ? 'You' : 'IFA'}:</span> {vm.content}
              </p>
            {/each}
          {:else}
            <p class="text-[11px] text-cyan-900">Say things like “create a course on React”, “go to my courses”, or “open the tutor for recursion”.</p>
          {/if}
        </div>
      {/if}

      {#each messages as msg}
        <div class="flex {msg.role === 'user' ? 'justify-end' : 'justify-start'}">
          <div class="max-w-[85%] p-3 rounded-2xl leading-relaxed whitespace-pre-wrap {msg.role === 'user'
            ? 'bg-ifa-pine text-white rounded-tr-none'
            : 'bg-white border border-ifa-border text-ifa-text-primary rounded-tl-none shadow-soft'}">
            {msg.content}
          </div>
        </div>
      {/each}

      {#if isThinking}
        <div class="flex justify-start">
          <div class="p-3 rounded-2xl bg-white border border-ifa-border text-ifa-text-muted flex items-center gap-2 shadow-soft">
            <span class="w-1.5 h-1.5 rounded-full bg-cyan-500 animate-bounce"></span>
            <span class="w-1.5 h-1.5 rounded-full bg-cyan-500 animate-bounce [animation-delay:0.2s]"></span>
            <span class="w-1.5 h-1.5 rounded-full bg-cyan-500 animate-bounce [animation-delay:0.4s]"></span>
            <span class="text-[11px] font-mono text-cyan-700">Thinking…</span>
          </div>
        </div>
      {/if}

      {#if profileReady}
        <div class="rounded-2xl border border-emerald-200 bg-emerald-50 p-3 space-y-2">
          <p class="text-[11px] font-bold text-emerald-800">Your learning profile is ready</p>
          {#if profile}
            <p class="text-[11px] text-emerald-900">
              Goal: <strong>{profile.learningGoal}</strong><br />
              Level: {profile.currentLevel} · {profile.weeklyStudyHours}h / week
            </p>
          {/if}
          {#if suggestedTopics.length > 0}
            <div class="flex flex-wrap gap-1">
              {#each suggestedTopics.slice(0, 4) as topic}
                <span class="text-[9px] px-1.5 py-0.5 rounded bg-white border border-emerald-200 text-emerald-800">{topic}</span>
              {/each}
            </div>
          {/if}
          <button
            type="button"
            on:click={generateCourse}
            disabled={generating}
            class="w-full py-2 rounded-xl bg-ifa-pine text-white text-[11px] font-bold hover:bg-ifa-pine-light transition flex items-center justify-center gap-1.5 disabled:opacity-60"
          >
            {#if generating}
              <Loader2 class="w-3.5 h-3.5 animate-spin" />
              <span>Generating your course…</span>
            {:else}
              <Sparkles class="w-3.5 h-3.5" />
              <span>Generate my course</span>
            {/if}
          </button>
        </div>
      {/if}

      {#if error}
        <p role="alert" class="text-[11px] text-red-600">{error}</p>
      {/if}
    </div>

    <!-- Input Box -->
    <div class="p-3 bg-white border-t border-ifa-border flex items-center gap-2">
      <input
        type="text"
        bind:value={chatMessage}
        on:keydown={(e) => e.key === 'Enter' && sendMessage()}
        placeholder="Type what you want to learn…"
        disabled={isThinking}
        class="flex-1 text-xs px-3.5 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine disabled:opacity-60"
      />
      <button
        type="button"
        on:click={sendMessage}
        disabled={isThinking || !chatMessage.trim()}
        class="w-9 h-9 rounded-xl bg-ifa-pine text-white flex items-center justify-center hover:bg-ifa-pine-light transition disabled:opacity-50"
      >
        <Send class="w-4 h-4" />
      </button>
    </div>
  </div>
{/if}
