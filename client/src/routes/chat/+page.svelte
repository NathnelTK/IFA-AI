<script lang="ts">
  import { afterUpdate, onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { ArrowLeft, Sparkles, Check, Edit2, BookOpen, Clock, Youtube, Loader2, Link2, ImageIcon, Send, LogIn } from 'lucide-svelte';
  import { aiApi, coursesApi } from '$lib/api';
  import { loadCourseDetail } from '$lib/stores/coursesStore';
  import { initSession, session } from '$lib/stores/sessionStore';
  import { renderMarkdown } from '$lib/utils/markdown';
  import type { PipelineModuleProposal } from '$lib/types';
  import type { IntakeHistoryMessage, IntakeProfileDto } from '$lib/api/types';

  type ConversationState = 'idle' | 'chatting' | 'scoping' | 'pipeline_ready' | 'generating' | 'saved';

  let conversationState: ConversationState = 'idle';
  let generatingPhase: 'blueprint' | 'module1' = 'blueprint';
  let error = '';

  // Chat state (Model 1 intake conversation)
  interface ChatMessage { role: 'user' | 'assistant'; content: string }
  let messages: ChatMessage[] = [];
  let chatBusy = false;
  let chatListEl: HTMLElement | null = null;
  let composerInput = '';

  // Scoping inputs (pre-filled from the intake profile when available)
  let hoursPerWeek = '5';
  let preferredChannel = 'freeCodeCamp';

  // Blueprint-relevant extras
  let materialsText = '';      // one link per line
  let coverImageUrl = '';
  let autoGenerateCover = false;

  // Pipeline proposal modules (filled by the Course Architect AI)
  let pipelineModules: PipelineModuleProposal[] = [];
  // Set once the blueprint has been saved to My Courses.
  let savedCourseId = '';

  // Auth gate: the page is publicly routed so it can show its own sign-in
  // prompt (which redirects back here) instead of bouncing to /login.
  let sessionReady = false;
  onMount(async () => {
    await initSession().catch(() => {});
    sessionReady = true;
    if ($session.authenticated && conversationState === 'idle') {
      openChat();
    }
  });

  afterUpdate(() => {
    chatListEl?.scrollTo({ top: chatListEl.scrollHeight });
  });

  function openChat(seed?: string) {
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
    const msg = (text ?? composerInput).trim();
    if (!msg || chatBusy) return;
    composerInput = '';
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
    if (profile.learningGoal) composerInput = profile.learningGoal;
    if (profile.weeklyStudyHours > 0) hoursPerWeek = String(profile.weeklyStudyHours);
    const channel = profile.preferredYouTubeChannels?.map((c) => (c ?? '').trim()).find((c) => c.length > 0);
    if (channel) preferredChannel = channel;
  }

  function handleStartScoping(prompt?: string) {
    if (prompt) composerInput = prompt;
    if (!composerInput.trim()) return;
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
    if (autoGenerateCover && composerInput.trim()) return buildGeneratedCover(composerInput.trim());
    const url = coverImageUrl.trim();
    if (!url) return undefined;
    return url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:image/') ? url : undefined;
  }

  async function proposePipeline() {
    if (!composerInput.trim()) return;
    if (!isValidGoal(composerInput)) {
      error = 'That goal is too short to build a course from. Tell me a bit more — e.g. "Learn SQL from scratch for data analysis" — or chat with me first.';
      return;
    }
    generatingPhase = 'blueprint';
    conversationState = 'generating';
    error = '';
    try {
      const proposal = await aiApi.proposeCourse({
        goal: composerInput.trim(),
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

  async function handleSaveCourse() {
    generatingPhase = 'module1';
    conversationState = 'generating';
    error = '';
    try {
      // Persist the blueprint only — content is generated later, per module,
      // from the course page. This keeps the save fast (no long synchronous
      // generation) and lets the learner start from My Courses.
      const course = await coursesApi.create({
        goal: composerInput.trim(),
        hoursPerWeek: Number(hoursPerWeek) || 5,
        preferredCreator: preferredChannel,
        materials: parseMaterials(),
        coverImageUrl: resolvedCoverUrl(),
        generateFirstModule: false
      });
      // Merge the fresh course into the store so it shows in My Courses
      // immediately (the /courses page reads this store).
      await loadCourseDetail(course.id).catch(() => {});
      savedCourseId = course.id;
      conversationState = 'saved';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not save the course.';
      conversationState = 'pipeline_ready';
    }
  }
</script>

<svelte:head>
  <title>Chat with IFA — AI Advisor</title>
</svelte:head>

<div class="h-screen flex flex-col bg-ifa-bg text-ifa-text-primary antialiased">
  <!-- Minimal header -->
  <header class="h-14 shrink-0 px-4 flex items-center justify-between border-b border-ifa-border bg-ifa-bg/90 backdrop-blur">
    <button
      type="button"
      on:click={() => goto('/home')}
      class="flex items-center gap-2 text-sm font-semibold text-ifa-text-secondary hover:text-ifa-text-primary transition"
    >
      <ArrowLeft class="w-4 h-4" />
      <span>Back to home</span>
    </button>
    <div class="flex items-center gap-2 text-sm font-bold text-ifa-pine">
      <Sparkles class="w-4 h-4 text-emerald-600" />
      <span>IFA Advisor</span>
    </div>
  </header>

  {#if !sessionReady}
    <div class="flex-1 flex items-center justify-center">
      <Loader2 class="w-6 h-6 text-ifa-pine animate-spin" />
    </div>
  {:else if !$session.authenticated}
    <!-- Sign-in prompt that redirects back to /chat after login -->
    <div class="flex-1 flex items-center justify-center p-6">
      <div class="w-full max-w-md bg-ifa-card rounded-2xl border border-ifa-border shadow-elevated p-8 text-center space-y-4">
        <div class="w-12 h-12 mx-auto rounded-full bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
          <LogIn class="w-5 h-5" />
        </div>
        <h1 class="text-lg font-bold text-ifa-text-primary">Sign in to chat with IFA</h1>
        <p class="text-sm text-ifa-text-secondary leading-relaxed">
          The advisor conversation designs a course around your goal — sign in (or create a free account) so your blueprint can be saved to My Courses.
        </p>
        <div class="flex flex-col sm:flex-row gap-2 pt-1">
          <a
            href="/login?redirect=/chat"
            class="flex-1 py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-sm font-bold transition flex items-center justify-center gap-2"
          >
            <LogIn class="w-4 h-4" />
            <span>Sign in</span>
          </a>
          <a
            href="/register?redirect=/chat"
            class="flex-1 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-primary text-sm font-semibold hover:bg-ifa-border/40 transition text-center"
          >
            Create account
          </a>
        </div>
      </div>
    </div>
  {:else}
    <!-- Scrollable message list -->
    <div bind:this={chatListEl} class="flex-1 overflow-y-auto">
      <div class="max-w-3xl mx-auto px-4 py-6 space-y-4">
        {#each messages as m, i (i)}
          {#if m.role === 'user'}
            <div class="flex justify-end">
              <div class="max-w-[85%] px-4 py-2.5 rounded-2xl bg-ifa-pine text-white text-sm leading-relaxed whitespace-pre-wrap rounded-tr-sm">
                {m.content}
              </div>
            </div>
          {:else}
            <div class="flex items-start gap-2.5">
              <div class="w-7 h-7 mt-0.5 rounded-full shrink-0 flex items-center justify-center bg-emerald-100 text-emerald-700">
                <Sparkles class="w-3.5 h-3.5" />
              </div>
              <!-- Assistant replies render as sanitized markdown so headings, bold, lists and code show properly -->
              <div class="max-w-[85%] px-4 py-2.5 rounded-2xl bg-ifa-card-muted border border-ifa-border-light rounded-tl-sm prose prose-sm max-w-none dark:prose-invert prose-p:my-1 prose-pre:my-1">
                {@html renderMarkdown(m.content)}
              </div>
            </div>
          {/if}
        {/each}

        {#if chatBusy}
          <div class="flex items-start gap-2.5">
            <div class="w-7 h-7 rounded-full shrink-0 flex items-center justify-center bg-emerald-100 text-emerald-700">
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
    </div>

    <!-- Sticky composer / state panels -->
    <div class="shrink-0 border-t border-ifa-border bg-ifa-bg/95 backdrop-blur">
      <div class="max-w-3xl mx-auto px-4 py-3 space-y-3">
        {#if error}
          <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700">{error}</p>
        {/if}

        <!-- Course setup form (pre-filled from the chat profile) -->
        {#if conversationState === 'scoping'}
          <div class="bg-ifa-card rounded-2xl p-4 border border-ifa-border shadow-card space-y-3">
            <div class="flex items-center justify-between border-b border-ifa-border-light pb-2">
              <div class="flex items-center gap-2 text-xs font-bold text-ifa-pine">
                <Sparkles class="w-4 h-4 text-emerald-600" />
                <span>Course Setup</span>
              </div>
              <button on:click={() => (conversationState = 'chatting')} class="text-[11px] text-ifa-text-muted hover:text-ifa-text-primary">
                Back to chat
              </button>
            </div>

            <div>
              <label for="chat-goal" class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">Learning Goal</label>
              <textarea
                id="chat-goal"
                bind:value={composerInput}
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
              <span aria-hidden="true">→</span>
            </button>
          </div>
        {:else if conversationState === 'pipeline_ready'}
          <!-- Proposed blueprint -->
          <div class="bg-ifa-card rounded-2xl p-4 border border-ifa-border shadow-card space-y-3">
            <div class="flex items-center justify-between border-b border-ifa-border-light pb-2">
              <span class="text-xs font-bold text-ifa-pine flex items-center gap-1.5">
                <BookOpen class="w-3.5 h-3.5 text-emerald-600" /> Proposed Course Blueprint (JIT Generation)
              </span>
              <span class="text-[10px] px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 font-semibold">
                Generate content on demand
              </span>
            </div>

            <div class="space-y-2 max-h-56 overflow-y-auto pr-1">
              {#each pipelineModules as mod (mod.id)}
                <div class="p-2.5 rounded-xl border border-ifa-border bg-ifa-card-muted/50 flex items-start justify-between gap-2">
                  <div>
                    <p class="text-xs font-bold text-ifa-text-primary">Module {mod.id}: {mod.title}</p>
                    <p class="text-[11px] text-ifa-text-secondary line-clamp-1">{mod.summary}</p>
                    <div class="flex gap-1.5 mt-1 flex-wrap">
                      {#each mod.topics.slice(0, 3) as topic (topic)}
                        <span class="text-[9px] px-1.5 py-0.5 bg-ifa-card rounded border border-ifa-border text-ifa-text-muted">{topic}</span>
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
                on:click={handleSaveCourse}
                class="flex-1 py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center justify-center gap-1.5"
              >
                <Check class="w-3.5 h-3.5" />
                <span>Save to My Courses</span>
              </button>
              <button
                type="button"
                on:click={() => (conversationState = 'scoping')}
                title="Edit setup"
                class="px-3 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary hover:text-ifa-text-primary text-xs font-semibold"
              >
                <Edit2 class="w-3.5 h-3.5" />
              </button>
            </div>
          </div>
        {:else if conversationState === 'generating'}
          <div class="bg-ifa-card rounded-2xl p-4 border border-ifa-border shadow-card flex items-center gap-3">
            <Loader2 class="w-4 h-4 text-ifa-pine animate-spin" />
            <div>
              <p class="text-xs font-bold text-ifa-pine">
                {generatingPhase === 'blueprint' ? 'Designing your course blueprint…' : 'Saving your course…'}
              </p>
              <p class="text-[11px] text-ifa-text-secondary">
                {generatingPhase === 'blueprint' ? 'The Course Architect is structuring your modules.' : 'Persisting your blueprint to My Courses.'}
              </p>
            </div>
          </div>
        {:else if conversationState === 'saved'}
          <div class="bg-ifa-card rounded-2xl p-4 border border-ifa-border shadow-card flex items-center gap-3">
            <div class="w-9 h-9 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-700 shrink-0">
              <Check class="w-4 h-4" />
            </div>
            <div class="flex-1">
              <p class="text-xs font-bold text-ifa-pine">Saved to My Courses</p>
              <p class="text-[11px] text-ifa-text-secondary">Open it and generate each module whenever you're ready.</p>
            </div>
            <button
              type="button"
              on:click={() => goto(`/courses/${savedCourseId}`)}
              class="px-3 py-2 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center gap-1.5"
            >
              <BookOpen class="w-3.5 h-3.5" />
              <span>Open course</span>
            </button>
            <button
              type="button"
              on:click={() => goto('/courses')}
              class="px-3 py-2 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary hover:text-ifa-text-primary text-xs font-semibold"
            >
              My Courses
            </button>
          </div>
        {/if}

        <!-- Chat composer (sticky bottom, hidden while the setup form is up) -->
        {#if conversationState !== 'saved'}
          <div class="relative flex items-center">
            <input
              type="text"
              bind:value={composerInput}
              on:keydown={(e) => e.key === 'Enter' && conversationState !== 'scoping' && conversationState !== 'generating' && sendChat()}
              placeholder={conversationState === 'scoping' ? 'Adjust your goal in the form above, or keep chatting…' : 'Answer IFA, or describe your goal…'}
              disabled={chatBusy || conversationState === 'generating'}
              class="w-full py-3 pl-4 pr-12 text-sm rounded-full bg-ifa-card border border-ifa-border text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/30 shadow-soft"
            />
            <button
              type="button"
              on:click={() => sendChat()}
              disabled={chatBusy || !composerInput.trim() || conversationState === 'generating'}
              class="absolute right-2 w-9 h-9 rounded-full bg-ifa-pine text-white flex items-center justify-center hover:bg-ifa-pine-light transition disabled:opacity-40"
              aria-label="Send message"
            >
              {#if chatBusy}
                <Loader2 class="w-4 h-4 animate-spin" />
              {:else}
                <Send class="w-4 h-4" />
              {/if}
            </button>
          </div>
          {#if conversationState === 'chatting'}
            <div class="flex items-center justify-between text-[11px]">
              <button on:click={() => handleStartScoping()} class="font-semibold text-ifa-pine hover:underline">
                Set up course now →
              </button>
              <span class="text-ifa-text-muted">IFA designs a course once your profile is complete</span>
            </div>
          {:else if conversationState === 'idle'}
            <div class="flex flex-wrap items-center gap-2">
              <button
                type="button"
                on:click={() => openChat('Prepare for my exit exam')}
                class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card hover:bg-ifa-card-muted text-ifa-text-primary border border-ifa-border transition shadow-soft"
              >
                Prepare for my exit exam
              </button>
              <button
                type="button"
                on:click={() => openChat('Learn C# from scratch')}
                class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card hover:bg-ifa-card-muted text-ifa-text-primary border border-ifa-border transition shadow-soft"
              >
                Learn C# from scratch
              </button>
              <button
                type="button"
                on:click={() => openChat('Improve my math skills')}
                class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card hover:bg-ifa-card-muted text-ifa-text-primary border border-ifa-border transition shadow-soft"
              >
                Improve my math skills
              </button>
            </div>
          {/if}
        {/if}
      </div>
    </div>
  {/if}
</div>

<style>
  /* Keep the composer visually pinned even on short viewports */
  @media (prefers-reduced-motion: no-preference) {
    .animate-bounce {
      animation: bounce 1s infinite;
    }
    @keyframes bounce {
      0%, 100% { transform: translateY(-25%); }
      50% { transform: translateY(25%); }
    }
  }
</style>
