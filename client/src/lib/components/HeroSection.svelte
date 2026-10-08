<script lang="ts">
  import { afterUpdate } from 'svelte';
  import { Mic, ArrowRight, Sparkles, Check, Edit2, BookOpen, Clock, Youtube, Loader2, Link2, ImageIcon, User, Send } from 'lucide-svelte';
  import { goto } from '$app/navigation';
  import { aiApi, coursesApi } from '$lib/api';
  import { loadCourseDetail } from '$lib/stores/coursesStore';
  import type { PipelineModuleProposal } from '../types';
  import type { IntakeHistoryMessage, IntakeProfileDto } from '../api/types';

  export let onGoalSubmit = (goal: string) => {};

  let goalInput = '';
  let isListening = false;
  let conversationState: 'idle' | 'chatting' | 'scoping' | 'pipeline_ready' | 'generating' = 'idle';
  let generatingPhase: 'blueprint' | 'module1' = 'blueprint';
  let error = '';

  // Chat state (Model 1 intake conversation)
  interface ChatMessage { role: 'user' | 'assistant'; content: string }
  let messages: ChatMessage[] = [];
  let chatBusy = false;
  let chatSeen = false;
  let chatListEl: HTMLElement | null = null;

  // Scoping inputs (pre-filled from the intake profile when available)
  let hoursPerWeek = '5';
  let preferredChannel = 'freeCodeCamp';

  // Blueprint-relevant extras
  let materialsText = '';      // one link per line
  let coverImageUrl = '';
  let autoGenerateCover = false;

  // Pipeline proposal modules (filled by the Course Architect AI)
  let pipelineModules: PipelineModuleProposal[] = [];

  afterUpdate(() => {
    chatListEl?.scrollTo({ top: chatListEl.scrollHeight });
  });

  function openChat(seed?: string) {
    chatSeen = true;
    conversationState = 'chatting';
    if (messages.length === 0) {
      messages = [{
        role: 'assistant',
        content: "Hi, I'm IFA! Tell me what you want to learn or master — I'll ask a couple of quick questions and then design a course around your goal."
      }];
    }
    if (seed) void sendChat(seed);
  }

  async function sendChat(text?: string) {
    const msg = (text ?? goalInput).trim();
    if (!msg || chatBusy) return;
    goalInput = '';
    error = '';
    chatBusy = true;
    conversationState = 'chatting';

    const history: IntakeHistoryMessage[] = messages.map((m) => ({ role: m.role, content: m.content }));
    messages = [...messages, { role: 'user', content: msg }];

    try {
      const res = await aiApi.intakeMessage(msg, history);
      const reply = res.reply || res.followUpQuestions?.[0] || 'Could you tell me a bit more?';
      messages = [...messages, { role: 'assistant', content: reply }];

      if (res.isProfileReady && res.profile) {
        applyProfile(res.profile);
        messages = [...messages, {
          role: 'assistant',
          content: 'I have everything I need — I pre-filled the course setup below. Adjust anything you like, then design your blueprint.'
        }];
        conversationState = 'scoping';
      }
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'IFA could not reply. Is the API running?';
    } finally {
      chatBusy = false;
    }
  }

  function applyProfile(profile: IntakeProfileDto) {
    if (profile.learningGoal) goalInput = profile.learningGoal;
    if (profile.weeklyStudyHours > 0) hoursPerWeek = String(profile.weeklyStudyHours);
    const channel = profile.preferredYouTubeChannels?.map((c) => (c ?? '').trim()).find((c) => c.length > 0);
    if (channel) preferredChannel = channel;
  }

  function handleStartScoping(prompt?: string) {
    if (prompt) goalInput = prompt;
    if (!goalInput.trim()) return;
    conversationState = 'scoping';
  }

  function parseMaterials(): string[] {
    return materialsText
      .split(/\r?\n|,/)
      .map((line) => line.trim())
      .filter((line) => line.length > 0);
  }

  function isValidGoal(goal: string): boolean {
    const words = goal.trim().split(/\s+/).filter(Boolean);
    return goal.trim().length >= 8 && words.length >= 2;
  }

  function buildGeneratedCover(title: string): string {
    // Deterministic gradient cover with the goal's initials — no external
    // service needed, and it renders anywhere an <img> is used.
    const palette = [
      ['#1B3D2F', '#2A9D68'],
      ['#0F766E', '#22D3EE'],
      ['#4C1D95', '#8B5CF6'],
      ['#9A3412', '#F59E0B'],
      ['#1E3A8A', '#3B82F6']
    ];
    let hash = 0;
    for (const ch of title) hash = (hash * 31 + ch.charCodeAt(0)) >>> 0;
    const [from, to] = palette[hash % palette.length];
    const initials = title
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((w) => w[0]?.toUpperCase())
      .join('') || 'IF';
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="360" viewBox="0 0 640 360"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${from}"/><stop offset="1" stop-color="${to}"/></linearGradient></defs><rect width="640" height="360" fill="url(#g)"/><circle cx="540" cy="60" r="140" fill="#ffffff" opacity="0.08"/><circle cx="90" cy="320" r="110" fill="#ffffff" opacity="0.07"/><text x="320" y="196" font-family="Segoe UI, Arial, sans-serif" font-size="96" font-weight="800" fill="#ffffff" text-anchor="middle" opacity="0.92">${initials}</text></svg>`;
    return `data:image/svg+xml,${encodeURIComponent(svg)}`;
  }

  function resolvedCoverUrl(): string | undefined {
    if (autoGenerateCover && goalInput.trim()) return buildGeneratedCover(goalInput.trim());
    const url = coverImageUrl.trim();
    if (!url) return undefined;
    return url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:image/') ? url : undefined;
  }

  async function proposePipeline() {
    if (!goalInput.trim()) return;
    if (!isValidGoal(goalInput)) {
      error = 'That goal is too short to build a course from. Tell me a bit more — e.g. "Learn SQL from scratch for data analysis" — or chat with me first.';
      return;
    }
    generatingPhase = 'blueprint';
    conversationState = 'generating';
    error = '';
    try {
      const proposal = await aiApi.proposeCourse({
        goal: goalInput.trim(),
        hoursPerWeek: Number(hoursPerWeek) || 5,
        preferredCreator: preferredChannel,
        materials: parseMaterials()
      });
      pipelineModules = (proposal.modules ?? []).map((m) => ({
        id: m.moduleNumber,
        title: m.title,
        summary: m.summary,
        estimatedHours: m.estimatedHours,
        topics: m.keyTopics ?? []
      }));
      conversationState = 'pipeline_ready';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not design a course blueprint.';
      conversationState = 'scoping';
    }
  }

  async function handleApprovePipeline() {
    generatingPhase = 'module1';
    conversationState = 'generating';
    error = '';
    try {
      const course = await coursesApi.create({
        goal: goalInput.trim(),
        hoursPerWeek: Number(hoursPerWeek) || 5,
        preferredCreator: preferredChannel,
        materials: parseMaterials(),
        coverImageUrl: resolvedCoverUrl()
      });
      onGoalSubmit(goalInput.trim());
      // Merge the fresh course into the store so it shows in My Courses
      // immediately (the /courses page reads this store).
      await loadCourseDetail(course.id).catch(() => {});
      await goto(`/courses/${course.id}`);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not generate the course.';
      conversationState = 'pipeline_ready';
    }
  }

  function toggleVoice() {
    isListening = !isListening;
    if (isListening) {
      goalInput = 'I want to prepare for my Computer Science exit exam, focusing on C# backend APIs.';
      setTimeout(() => {
        isListening = false;
        openChat(goalInput);
        goalInput = '';
      }, 1200);
    }
  }
</script>

<div class="relative overflow-hidden rounded-3xl border border-ifa-border bg-ifa-card shadow-card">
  <!-- Animated wave background (no photo): three drifting wave layers -->
  <div class="absolute inset-0 ifa-wave-field pointer-events-none" aria-hidden="true">
    <div class="absolute inset-0 ifa-wave-glow"></div>
    <svg class="ifa-wave ifa-wave-slow" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
      <path class="ifa-wave-path-1" d="M0 160 C 360 260 720 60 1080 160 C 1440 260 1800 60 2160 160 C 2520 260 2700 110 2880 160 L 2880 320 L 0 320 Z" />
    </svg>
    <svg class="ifa-wave ifa-wave-fast" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
      <path class="ifa-wave-path-2" d="M0 200 C 480 100 960 300 1440 200 C 1920 100 2400 300 2880 200 L 2880 320 L 0 320 Z" />
    </svg>
    <svg class="ifa-wave ifa-wave-mid" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
      <path class="ifa-wave-path-3" d="M0 240 C 720 160 1440 320 2160 240 C 2520 200 2700 260 2880 240 L 2880 320 L 0 320 Z" />
    </svg>
  </div>

  <!-- Readability scrim (theme-aware) so text always contrasts with the waves -->
  <div class="absolute inset-0 ifa-hero-scrim pointer-events-none"></div>

  <!-- Content Container -->
  <div class="relative z-10 p-8 md:p-10 max-w-2xl">
    <div class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md ifa-hero-chip text-[11px] font-mono tracking-wider font-semibold uppercase mb-3">
      // AI LEARNING ASSISTANT
    </div>

    <h2 class="text-3xl font-extrabold tracking-tight text-ifa-pine leading-tight mb-2">
      What do you want to learn today?
    </h2>
    <p class="text-xs text-ifa-text-secondary font-medium leading-relaxed mb-6">
      Chat with IFA about your goal — it asks the right questions, then builds a personalized course.
    </p>

    {#if error}
      <p role="alert" class="mb-4 rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700">{error}</p>
    {/if}

    <!-- State 1: Input Box -->
    {#if conversationState === 'idle'}
      <div class="relative flex items-center mb-4">
        <button
          type="button"
          on:click={toggleVoice}
          title="Speak with Voxide"
          class="absolute left-3 w-8 h-8 rounded-full flex items-center justify-center transition-all {isListening
            ? 'bg-red-500 text-white animate-pulse'
            : 'bg-emerald-100 text-emerald-800 hover:bg-emerald-200'}"
        >
          <Mic class="w-4 h-4" />
        </button>

        <input
          type="text"
          bind:value={goalInput}
          on:keydown={(e) => e.key === 'Enter' && openChat()}
          placeholder="Type or speak your goal..."
          class="w-full pl-13 pr-14 py-3.5 text-xs rounded-full bg-ifa-card/95 backdrop-blur-sm border border-ifa-border text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/30 shadow-soft"
        />

        <button
          type="button"
          on:click={() => openChat()}
          class="absolute right-2 w-8 h-8 rounded-full bg-ifa-pine text-white flex items-center justify-center hover:bg-ifa-pine-light transition shadow-sm"
        >
          <ArrowRight class="w-4 h-4" />
        </button>
      </div>

      <!-- Quick Action Suggestion Chips — they open the chat -->
      <div class="flex flex-wrap items-center gap-2">
        <button
          type="button"
          on:click={() => openChat('Prepare for my exit exam')}
          class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft"
        >
          Prepare for my exit exam
        </button>
        <button
          type="button"
          on:click={() => openChat('Learn C# from scratch')}
          class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft"
        >
          Learn C# from scratch
        </button>
        <button
          type="button"
          on:click={() => openChat('Improve my math skills')}
          class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft"
        >
          Improve my math skills
        </button>
        <button
          type="button"
          on:click={() => goto('/courses')}
          class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft"
        >
          Explore public courses
        </button>
      </div>

    <!-- State 2: Real conversation with Model 1 -->
    {:else if conversationState === 'chatting'}
      <div class="bg-ifa-card/95 backdrop-blur-md rounded-2xl border border-ifa-border shadow-elevated overflow-hidden">
        <div class="flex items-center justify-between px-4 py-2.5 border-b border-ifa-border-light">
          <div class="flex items-center gap-2 text-xs font-bold text-ifa-pine">
            <Sparkles class="w-4 h-4 text-emerald-600" />
            <span>Chat with IFA</span>
          </div>
          <div class="flex items-center gap-3">
            <button on:click={() => (conversationState = 'scoping')} class="text-[11px] font-semibold text-ifa-pine hover:underline">
              Set up course now →
            </button>
            <button on:click={() => (conversationState = 'idle')} class="text-[11px] text-ifa-text-muted hover:text-ifa-text-primary">
              Close
            </button>
          </div>
        </div>

        <div bind:this={chatListEl} class="px-4 py-3 space-y-2.5 max-h-60 overflow-y-auto">
          {#each messages as m}
            <div class="flex gap-2 {m.role === 'user' ? 'flex-row-reverse' : ''}">
              <div class="w-6 h-6 rounded-full shrink-0 flex items-center justify-center {m.role === 'user' ? 'bg-ifa-pine/15 text-ifa-pine' : 'bg-emerald-100 text-emerald-700'}">
                {#if m.role === 'user'}
                  <User class="w-3.5 h-3.5" />
                {:else}
                  <Sparkles class="w-3.5 h-3.5" />
                {/if}
              </div>
              <div class="max-w-[80%] px-3 py-2 rounded-2xl text-xs leading-relaxed whitespace-pre-wrap {m.role === 'user'
                ? 'bg-ifa-pine text-white rounded-tr-sm'
                : 'bg-ifa-card-muted text-ifa-text-primary rounded-tl-sm border border-ifa-border-light'}">
                {m.content}
              </div>
            </div>
          {/each}
          {#if chatBusy}
            <div class="flex gap-2">
              <div class="w-6 h-6 rounded-full shrink-0 flex items-center justify-center bg-emerald-100 text-emerald-700">
                <Sparkles class="w-3.5 h-3.5" />
              </div>
              <div class="px-4 py-3 rounded-2xl bg-ifa-card-muted border border-ifa-border-light flex items-center gap-1">
                <span class="w-1.5 h-1.5 rounded-full bg-ifa-text-muted animate-bounce" style="animation-delay: 0ms;"></span>
                <span class="w-1.5 h-1.5 rounded-full bg-ifa-text-muted animate-bounce" style="animation-delay: 120ms;"></span>
                <span class="w-1.5 h-1.5 rounded-full bg-ifa-text-muted animate-bounce" style="animation-delay: 240ms;"></span>
              </div>
            </div>
          {/if}
        </div>

        <div class="relative flex items-center border-t border-ifa-border-light p-2.5">
          <input
            type="text"
            bind:value={goalInput}
            on:keydown={(e) => e.key === 'Enter' && sendChat()}
            placeholder="Answer IFA, or describe your goal..."
            disabled={chatBusy}
            class="w-full py-2.5 pl-3.5 pr-11 text-xs rounded-full bg-ifa-card-muted border border-ifa-border text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/30"
          />
          <button
            type="button"
            on:click={() => sendChat()}
            disabled={chatBusy || !goalInput.trim()}
            class="absolute right-4 w-8 h-8 rounded-full bg-ifa-pine text-white flex items-center justify-center hover:bg-ifa-pine-light transition disabled:opacity-40"
          >
            {#if chatBusy}
              <Loader2 class="w-4 h-4 animate-spin" />
            {:else}
              <Send class="w-3.5 h-3.5" />
            {/if}
          </button>
        </div>
      </div>

    <!-- State 3: Course Setup Form (pre-filled from the chat profile) -->
    {:else if conversationState === 'scoping'}
      <div class="bg-ifa-card/95 backdrop-blur-md rounded-2xl p-5 border border-ifa-border shadow-elevated space-y-4">
        <div class="flex items-center justify-between border-b border-ifa-border-light pb-2.5">
          <div class="flex items-center gap-2 text-xs font-bold text-ifa-pine">
            <Sparkles class="w-4 h-4 text-emerald-600" />
            <span>Course Setup</span>
          </div>
          <button on:click={() => openChat()} class="text-[11px] text-ifa-text-muted hover:text-ifa-text-primary">
            Back to chat
          </button>
        </div>

        <div>
          <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">Learning Goal</label>
          <textarea
            bind:value={goalInput}
            rows="2"
            placeholder='e.g. "Learn SQL from scratch to analyze data"'
            class="w-full text-xs rounded-xl bg-ifa-card-muted border border-ifa-border p-2.5 text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine resize-none"
          ></textarea>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">
              <Clock class="w-3 h-3 inline mr-1 text-ifa-accent-amber" /> Weekly Time Commitment
            </label>
            <select
              bind:value={hoursPerWeek}
              class="w-full text-xs rounded-xl bg-ifa-card-muted border border-ifa-border p-2 text-ifa-text-primary focus:ring-1 focus:ring-ifa-pine"
            >
              <option value="3">3 hours / week (Paced)</option>
              <option value="5">5 hours / week (Standard)</option>
              <option value="10">10 hours / week (Intensive)</option>
            </select>
          </div>

          <div>
            <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">
              <Youtube class="w-3 h-3 inline mr-1 text-red-600" /> Preferred Video Creator
            </label>
            <input
              type="text"
              bind:value={preferredChannel}
              placeholder="e.g. freeCodeCamp, Traversy Media"
              class="w-full text-xs rounded-xl bg-ifa-card-muted border border-ifa-border p-2 text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine"
            />
          </div>
        </div>

        <div>
          <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">
            <Link2 class="w-3 h-3 inline mr-1 text-ifa-accent-blue" /> External Materials (optional)
          </label>
          <textarea
            bind:value={materialsText}
            rows="2"
            placeholder="Paste links one per line — docs, repos, slides, papers. The Course Architect will fold them into your blueprint."
            class="w-full text-xs rounded-xl bg-ifa-card-muted border border-ifa-border p-2.5 text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine resize-none"
          ></textarea>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 items-end">
          <div>
            <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">
              <ImageIcon class="w-3 h-3 inline mr-1 text-ifa-accent-purple" /> Cover Image URL (optional)
            </label>
            <input
              type="text"
              bind:value={coverImageUrl}
              disabled={autoGenerateCover}
              placeholder="https://…/cover.jpg"
              class="w-full text-xs rounded-xl bg-ifa-card-muted border border-ifa-border p-2 text-ifa-text-primary placeholder-ifa-text-muted focus:ring-1 focus:ring-ifa-pine disabled:opacity-50"
            />
          </div>
          <label class="flex items-center gap-2 text-xs text-ifa-text-secondary select-none pb-1.5">
            <input type="checkbox" bind:checked={autoGenerateCover} class="accent-ifa-pine w-3.5 h-3.5" />
            Generate a simple cover for me
          </label>
        </div>

        {#if resolvedCoverUrl()}
          <div class="flex items-center gap-3">
            <img src={resolvedCoverUrl()} alt="Cover preview" class="w-24 h-14 object-cover rounded-lg border border-ifa-border" />
            <span class="text-[10px] text-ifa-text-muted">Cover preview</span>
          </div>
        {/if}

        <button
          type="button"
          on:click={proposePipeline}
          disabled={chatBusy}
          class="w-full py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center justify-center gap-2 disabled:opacity-60"
        >
          <span>Design my course blueprint</span>
          <ArrowRight class="w-3.5 h-3.5" />
        </button>
      </div>

    <!-- State 4: Interactive Pipeline Card -->
    {:else if conversationState === 'pipeline_ready'}
      <div class="bg-ifa-card/95 backdrop-blur-md rounded-2xl p-5 border border-ifa-border shadow-elevated space-y-3">
        <div class="flex items-center justify-between border-b border-ifa-border-light pb-2">
          <span class="text-xs font-bold text-ifa-pine flex items-center gap-1.5">
            <BookOpen class="w-3.5 h-3.5 text-emerald-600" /> Proposed Course Blueprint (JIT Generation)
          </span>
          <span class="text-[10px] px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 font-semibold">
            Module 1 ready on start
          </span>
        </div>

        <div class="space-y-2 max-h-48 overflow-y-auto pr-1">
          {#each pipelineModules as mod}
            <div class="p-2.5 rounded-xl border border-ifa-border bg-ifa-card-muted/50 flex items-start justify-between gap-2">
              <div>
                <p class="text-xs font-bold text-ifa-text-primary">
                  Module {mod.id}: {mod.title}
                </p>
                <p class="text-[11px] text-ifa-text-secondary line-clamp-1">{mod.summary}</p>
                <div class="flex gap-1.5 mt-1 flex-wrap">
                  {#each mod.topics.slice(0, 3) as topic}
                    <span class="text-[9px] px-1.5 py-0.5 bg-ifa-card rounded border border-ifa-border text-ifa-text-muted">
                      {topic}
                    </span>
                  {/each}
                </div>
              </div>
              <span class="text-[10px] font-mono text-ifa-text-muted shrink-0">{mod.estimatedHours}h</span>
            </div>
          {/each}
        </div>

        <div class="flex items-center gap-2 pt-1">
          <button
            type="button"
            on:click={handleApprovePipeline}
            class="flex-1 py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center justify-center gap-1.5"
          >
            <Check class="w-3.5 h-3.5" />
            <span>Approve &amp; generate course</span>
          </button>
          <button
            type="button"
            on:click={() => (conversationState = 'scoping')}
            class="px-3 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary hover:text-ifa-text-primary text-xs font-semibold"
          >
            <Edit2 class="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

    <!-- State 5: Generating -->
    {:else if conversationState === 'generating'}
      <div class="bg-ifa-card/95 backdrop-blur-md rounded-2xl p-6 border border-ifa-border shadow-elevated text-center space-y-3">
        <div class="w-10 h-10 mx-auto rounded-full bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
          <Loader2 class="w-5 h-5 animate-spin" />
        </div>
        {#if generatingPhase === 'blueprint'}
          <p class="text-xs font-bold text-ifa-pine">Designing your course blueprint…</p>
          <p class="text-[11px] text-ifa-text-secondary">The Course Architect is structuring your modules.</p>
        {:else}
          <p class="text-xs font-bold text-ifa-pine">Generating module 1…</p>
          <p class="text-[11px] text-ifa-text-secondary">Research is attached and the builder is writing your first lesson.</p>
        {/if}
      </div>
    {/if}
  </div>
</div>

<style>
  /* Animated wave field — light and dark aware */
  .ifa-wave-field {
    background: linear-gradient(160deg, #eef3ea 0%, #f8f6f0 55%, #e7f0e6 100%);
  }
  :global(.dark) .ifa-wave-field {
    background: linear-gradient(160deg, #0d1a12 0%, #0a0a0a 55%, #101c14 100%);
  }

  .ifa-wave-glow {
    position: absolute;
    inset: 0;
    background: radial-gradient(60% 90% at 85% 10%, rgba(42, 157, 104, 0.18), transparent 70%);
  }
  :global(.dark) .ifa-wave-glow {
    background: radial-gradient(60% 90% at 85% 10%, rgba(34, 197, 94, 0.12), transparent 70%);
  }

  .ifa-wave {
    position: absolute;
    bottom: -2px;
    left: 0;
    width: 200%;
    height: 62%;
    will-change: transform;
  }
  .ifa-wave-slow { animation: ifa-wave-drift 26s linear infinite; }
  .ifa-wave-mid { animation: ifa-wave-drift 18s linear infinite reverse; height: 48%; opacity: 0.75; }
  .ifa-wave-fast { animation: ifa-wave-drift 12s linear infinite; height: 34%; opacity: 0.55; }

  @keyframes ifa-wave-drift {
    from { transform: translateX(0); }
    to { transform: translateX(-50%); }
  }

  .ifa-wave-path-1 { fill: rgba(42, 157, 104, 0.16); }
  .ifa-wave-path-2 { fill: rgba(27, 61, 47, 0.12); }
  .ifa-wave-path-3 { fill: rgba(42, 157, 104, 0.20); }
  :global(.dark) .ifa-wave-path-1 { fill: rgba(34, 197, 94, 0.10); }
  :global(.dark) .ifa-wave-path-2 { fill: rgba(34, 197, 94, 0.06); }
  :global(.dark) .ifa-wave-path-3 { fill: rgba(34, 197, 94, 0.13); }

  /* Readability scrim over the waves (kept off Tailwind's gradient utilities
     so the global dark-mode gradient overrides in app.css can't clobber it) */
  .ifa-hero-scrim {
    background: linear-gradient(to right, rgba(248, 246, 240, 0.96) 0%, rgba(248, 246, 240, 0.88) 45%, rgba(248, 246, 240, 0.35) 75%, rgba(248, 246, 240, 0.05) 100%);
  }
  :global(.dark) .ifa-hero-scrim {
    background: linear-gradient(to right, rgba(10, 10, 10, 0.96) 0%, rgba(10, 10, 10, 0.88) 45%, rgba(10, 10, 10, 0.45) 75%, rgba(10, 10, 10, 0.08) 100%);
  }

  .ifa-hero-chip {
    background: rgba(27, 61, 47, 0.10);
    color: #1b3d2f;
  }
  :global(.dark) .ifa-hero-chip {
    background: rgba(34, 197, 94, 0.12);
    color: #4ade80;
  }
</style>
