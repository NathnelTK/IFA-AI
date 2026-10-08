<script lang="ts">
  import { browser } from '$app/environment';
  import { Bot, Send, Trash2, Loader2, Sparkles, Maximize2, Minimize2, History, Plus, ArrowLeft, X } from 'lucide-svelte';
  import { aiApi } from '$lib/api';
  import { renderMarkdown } from '$lib/utils/markdown';

  /**
   * In-course AI chatbot.
   *
   * A learner reading a generated course can ask questions about it and get
   * answers grounded in the course/module/lesson they are currently viewing —
   * it is NOT a course generator (that flow lives in the home hero / Voxide).
   *
   * The active transcript is persisted per course in localStorage
   * (`ifa.courseChat.<id>`), and past conversations can be archived and revisited
   * from the history panel. The backend tutor chat is stateless, so we keep the
   * transcript client-side and send a bounded slice back as context each turn.
   */

  export let courseId: string;
  export let courseTitle: string;
  export let moduleId: string | null = null;
  export let lessonId: string | null = null;
  export let lessonTitle: string | null = null;

  interface ChatTurn {
    role: 'user' | 'assistant';
    content: string;
    at: number;
  }

  interface ArchivedChat {
    id: string;
    title: string;
    at: number;
    messages: ChatTurn[];
  }

  const MAX_HISTORY = 40; // turns persisted per course
  const MAX_CONTEXT = 8; // turns sent to the model as context
  const MAX_ARCHIVED = 20;

  function storageKey(id: string): string {
    return `ifa.courseChat.${id}`;
  }
  function archiveKey(id: string): string {
    return `ifa.courseChat.archive.${id}`;
  }

  function readHistory(id: string): ChatTurn[] {
    if (!browser) return [];
    try {
      const raw = localStorage.getItem(storageKey(id));
      if (!raw) return [];
      const parsed = JSON.parse(raw) as ChatTurn[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  function writeHistory(id: string, turns: ChatTurn[]): void {
    if (!browser) return;
    try {
      localStorage.setItem(storageKey(id), JSON.stringify(turns.slice(-MAX_HISTORY)));
    } catch {
      /* storage may be unavailable (private mode) — keep it in memory only */
    }
  }

  function readArchive(id: string): ArchivedChat[] {
    if (!browser) return [];
    try {
      const raw = localStorage.getItem(archiveKey(id));
      if (!raw) return [];
      const parsed = JSON.parse(raw) as ArchivedChat[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  function writeArchive(id: string, list: ArchivedChat[]): void {
    if (!browser) return;
    try {
      localStorage.setItem(archiveKey(id), JSON.stringify(list.slice(0, MAX_ARCHIVED)));
    } catch {
      /* ignore */
    }
  }

  function greeting(): ChatTurn {
    return {
      role: 'assistant',
      content: `Hi! I'm your course assistant for **${courseTitle}**. Ask me to explain a concept, give an example, or quiz you on what you're reading.`,
      at: Date.now()
    };
  }

  let messages: ChatTurn[] = [];
  let draft = '';
  let sending = false;
  let error = '';
  let expanded = false;
  let showHistory = false;
  let archive: ArchivedChat[] = [];

  // (Re)load the transcript + archive whenever the course changes.
  let loadedFor = '';
  $: if (courseId && loadedFor !== courseId) {
    loadedFor = courseId;
    const saved = readHistory(courseId);
    messages = saved.length > 0 ? saved : [greeting()];
    archive = readArchive(courseId);
    error = '';
    expanded = false;
    showHistory = false;
  }

  // Context label shown under the header — reflects the open lesson/module.
  $: contextLabel = lessonTitle
    ? `On: ${lessonTitle}`
    : moduleId
      ? 'Using this module as context'
      : 'Using the course outline as context';

  async function scrollToBottom(): Promise<void> {
    if (!browser) return;
    await Promise.resolve();
    const el = document.getElementById('course-chat-scroll');
    if (el) el.scrollTop = el.scrollHeight;
  }

  /** Archive the current conversation (if it has real content) and start fresh. */
  function startNewChat() {
    const hasContent = messages.some((m) => m.role === 'user');
    if (hasContent) {
      const firstUser = messages.find((m) => m.role === 'user');
      archive = [
        {
          id: (browser && 'crypto' in window && 'randomUUID' in crypto) ? crypto.randomUUID() : `c-${Date.now()}`,
          title: (firstUser?.content || 'Conversation').slice(0, 60),
          at: Date.now(),
          messages
        },
        ...archive
      ].slice(0, MAX_ARCHIVED);
      writeArchive(courseId, archive);
    }
    messages = [greeting()];
    draft = '';
    error = '';
    showHistory = false;
    writeHistory(courseId, messages);
  }

  function openArchived(entry: ArchivedChat) {
    messages = entry.messages.length > 0 ? entry.messages : [greeting()];
    showHistory = false;
    error = '';
    writeHistory(courseId, messages);
    void scrollToBottom();
  }

  function deleteArchived(id: string) {
    archive = archive.filter((a) => a.id !== id);
    writeArchive(courseId, archive);
  }

  async function send() {
    const text = draft.trim();
    if (!text || sending) return;

    const history = messages
      .slice(-MAX_CONTEXT)
      .map((m) => ({ role: m.role, content: m.content }));

    messages = [...messages, { role: 'user', content: text, at: Date.now() }];
    draft = '';
    error = '';
    sending = true;
    writeHistory(courseId, messages);
    await scrollToBottom();

    try {
      const response = await aiApi.tutorChat({
        message: text,
        history,
        courseId,
        moduleId: moduleId ?? undefined,
        lessonId: lessonId ?? undefined
      });
      const reply = response.replyMarkdown?.trim() || 'I could not find an answer for that.';
      messages = [...messages, { role: 'assistant', content: reply, at: Date.now() }];
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The assistant is unavailable right now.';
      messages = [
        ...messages,
        {
          role: 'assistant',
          content: 'Sorry, I could not reach the tutor service just now. Please try again.',
          at: Date.now()
        }
      ];
    } finally {
      sending = false;
      writeHistory(courseId, messages);
      await scrollToBottom();
    }
  }

  function clearHistory() {
    messages = [greeting()];
    error = '';
    writeHistory(courseId, messages);
  }

  $: suggestedFollowUps = [
    'Explain this in simpler terms',
    'Give me a real-world example',
    'What should I remember for the quiz?'
  ];
</script>

<div
  class="bg-ifa-card border border-ifa-border shadow-card overflow-hidden flex flex-col {expanded
    ? 'fixed inset-0 z-50 h-screen rounded-none'
    : 'rounded-2xl h-[600px]'}"
>
  <!-- Header -->
  <div class="px-4 py-3 border-b border-ifa-border flex items-center justify-between gap-2 shrink-0">
    <div class="flex items-center gap-2.5 min-w-0">
      <div class="w-8 h-8 rounded-lg bg-ifa-pine flex items-center justify-center text-white shrink-0">
        <Bot class="w-4 h-4" />
      </div>
      <div class="min-w-0">
        <h3 class="text-sm font-bold text-ifa-text-primary leading-tight">Course Assistant</h3>
        <p class="text-[10px] text-ifa-text-muted truncate">{contextLabel}</p>
      </div>
    </div>
    <div class="flex items-center gap-1 shrink-0">
      <button
        type="button"
        on:click={() => (showHistory = true)}
        title="Conversation history"
        class="w-7 h-7 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-muted hover:text-ifa-pine transition"
      >
        <History class="w-3.5 h-3.5" />
      </button>
      <button
        type="button"
        on:click={startNewChat}
        title="New chat"
        class="w-7 h-7 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-muted hover:text-ifa-pine transition"
      >
        <Plus class="w-3.5 h-3.5" />
      </button>
      <button
        type="button"
        on:click={clearHistory}
        title="Clear conversation"
        class="w-7 h-7 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-muted hover:text-red-600 transition"
      >
        <Trash2 class="w-3.5 h-3.5" />
      </button>
      <button
        type="button"
        on:click={() => (expanded = !expanded)}
        title={expanded ? 'Exit full screen' : 'Expand to full screen'}
        class="w-7 h-7 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-muted hover:text-ifa-pine transition"
      >
        {#if expanded}
          <Minimize2 class="w-3.5 h-3.5" />
        {:else}
          <Maximize2 class="w-3.5 h-3.5" />
        {/if}
      </button>
    </div>
  </div>

  <!-- History panel -->
  {#if showHistory}
    <div class="absolute inset-0 z-10 bg-ifa-card flex flex-col">
      <div class="px-4 py-3 border-b border-ifa-border flex items-center justify-between gap-2 shrink-0">
        <button type="button" on:click={() => (showHistory = false)} class="flex items-center gap-2 text-sm font-bold text-ifa-text-primary hover:text-ifa-pine transition">
          <ArrowLeft class="w-4 h-4" />
          <span>Conversation history</span>
        </button>
        <button type="button" on:click={() => (showHistory = false)} class="w-7 h-7 rounded-full bg-ifa-card-muted border border-ifa-border flex items-center justify-center text-ifa-text-muted hover:text-ifa-text-primary transition">
          <X class="w-3.5 h-3.5" />
        </button>
      </div>
      <div class="flex-1 overflow-y-auto p-3 space-y-2">
        {#if archive.length === 0}
          <p class="text-xs text-ifa-text-muted text-center py-8">
            No previous conversations yet. Start a new chat and it will be saved here.
          </p>
        {:else}
          {#each archive as entry}
            <div class="group flex items-center gap-2 rounded-xl border border-ifa-border bg-ifa-card-muted/50 px-3 py-2.5">
              <button type="button" on:click={() => openArchived(entry)} class="flex-1 text-left min-w-0">
                <p class="text-xs font-semibold text-ifa-text-primary truncate">{entry.title}</p>
                <p class="text-[10px] text-ifa-text-muted">{new Date(entry.at).toLocaleString()} · {entry.messages.length} messages</p>
              </button>
              <button
                type="button"
                on:click={() => deleteArchived(entry.id)}
                title="Delete conversation"
                class="w-7 h-7 rounded-full flex items-center justify-center text-ifa-text-muted hover:text-red-600 transition shrink-0"
              >
                <Trash2 class="w-3.5 h-3.5" />
              </button>
            </div>
          {/each}
        {/if}
      </div>
    </div>
  {/if}

  <!-- Messages -->
  <div id="course-chat-scroll" class="flex-1 overflow-y-auto p-4 space-y-3 bg-ifa-bg/40 dark:bg-black/20">
    {#each messages as msg}
      <div class="flex {msg.role === 'user' ? 'justify-end' : 'justify-start'}">
        <div
          class="max-w-[88%] px-3.5 py-2.5 rounded-2xl text-xs shadow-soft {msg.role === 'user'
            ? 'bg-ifa-pine text-white rounded-tr-none'
            : 'bg-white border border-ifa-border text-ifa-text-primary rounded-tl-none'}"
        >
          {#if msg.role === 'assistant'}
            <div class="prose prose-sm max-w-none dark:prose-invert prose-p:my-1 prose-pre:my-1">
              {@html renderMarkdown(msg.content)}
            </div>
          {:else}
            <p class="leading-relaxed whitespace-pre-wrap">{msg.content}</p>
          {/if}
        </div>
      </div>
    {/each}

    {#if sending}
      <div class="flex justify-start">
        <div class="px-3.5 py-2.5 rounded-2xl bg-white border border-ifa-border text-ifa-text-muted flex items-center gap-2 shadow-soft">
          <Loader2 class="w-3.5 h-3.5 animate-spin text-ifa-pine" />
          <span class="text-[11px] font-mono">Thinking…</span>
        </div>
      </div>
    {/if}

    {#if error}
      <p role="alert" class="text-[11px] text-red-600 dark:text-red-400">{error}</p>
    {/if}
  </div>

  <!-- Suggested follow-ups -->
  {#if messages.length <= 1}
    <div class="px-4 pt-3 flex flex-wrap gap-1.5 border-t border-ifa-border bg-ifa-card">
      {#each suggestedFollowUps as s}
        <button
          type="button"
          on:click={() => (draft = s)}
          class="text-[10px] px-2 py-1 rounded-full bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary hover:border-ifa-pine/50 hover:text-ifa-pine transition flex items-center gap-1"
        >
          <Sparkles class="w-2.5 h-2.5" />{s}
        </button>
      {/each}
    </div>
  {/if}

  <!-- Input -->
  <div class="p-3 border-t border-ifa-border bg-ifa-card flex items-center gap-2 shrink-0">
    <input
      type="text"
      bind:value={draft}
      on:keydown={(e) => e.key === 'Enter' && send()}
      placeholder="Ask about this course…"
      disabled={sending}
      class="flex-1 text-xs px-3.5 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine disabled:opacity-60"
    />
    <button
      type="button"
      on:click={send}
      disabled={sending || !draft.trim()}
      class="w-9 h-9 rounded-xl bg-ifa-pine text-white flex items-center justify-center hover:bg-ifa-pine-light transition disabled:opacity-50 shrink-0"
    >
      <Send class="w-4 h-4" />
    </button>
  </div>
</div>
