<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { Bot, Send, Sparkles, BookOpen, Loader2 } from 'lucide-svelte';
  import { aiApi, coursesApi, type CourseSummaryDto } from '$lib/api';
  import { renderMarkdown } from '$lib/utils/markdown';

  let courseList: CourseSummaryDto[] = [];
  let selectedCourseId = '';
  let message = '';
  let sending = false;
  let error = '';
  let messages: Array<{ role: 'user' | 'assistant'; content: string }> = [];

  function greetingText(): string {
    const course = courseList.find((c) => c.id === selectedCourseId);
    if (course) {
      return `Hi! I'm your AI tutor for **${course.title}**. Ask me to explain a concept, walk through an example, or quiz you on this course.`;
    }
    return "Hi! I'm your AI tutor. Pick one of your courses to focus on, or ask a general question to get started.";
  }

  onMount(async () => {
    try {
      courseList = await coursesApi.listMine();
    } catch {
      /* keep empty — general mode still works */
    }
    messages = [{ role: 'assistant', content: greetingText() }];
    // Honour deep links like /ai-tutor?topic=Databases from My Skills / Progress.
    const topic = $page.url.searchParams.get('topic');
    if (topic) message = topic;
  });

  function onCourseChange() {
    messages = [{ role: 'assistant', content: greetingText() }];
    error = '';
  }

  async function sendMessage() {
    const question = message.trim();
    if (!question || sending) return;

    const history = [...messages];
    messages = [...messages, { role: 'user', content: question }];
    message = '';
    error = '';
    sending = true;

    try {
      const response = await aiApi.tutorChat({
        message: question,
        history,
        courseId: selectedCourseId || undefined
      });
      messages = [...messages, { role: 'assistant', content: response.replyMarkdown }];
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The tutor request failed.';
    } finally {
      sending = false;
    }
  }

  const quickTopics = [
    { icon: BookOpen, label: 'Explain the key concepts' },
    { icon: Sparkles, label: 'Give me a worked example' },
    { icon: Sparkles, label: 'Quiz me on this' }
  ];
</script>

<div class="p-4 md:p-8 space-y-6 max-w-[1400px] mx-auto h-[calc(100vh-4rem)] md:h-[calc(100vh-5rem)] flex flex-col">
  <!-- Header -->
  <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
    <div class="flex items-center gap-3 min-w-0">
      <div class="w-10 h-10 rounded-xl bg-ifa-pine flex items-center justify-center text-white shrink-0">
        <Bot class="w-5 h-5" />
      </div>
      <div class="min-w-0">
        <h1 class="text-xl md:text-2xl font-bold text-ifa-text-primary">AI Tutor</h1>
        <p class="text-sm text-ifa-text-secondary">Course-aware tutor — pick a course to change the subject</p>
      </div>
    </div>
    <div class="flex items-center gap-2 shrink-0">
      <span class="w-2 h-2 rounded-full {sending ? 'bg-amber-500 animate-pulse' : 'bg-emerald-500'}"></span>
      <span class="text-xs text-ifa-text-secondary font-medium">{sending ? 'Thinking…' : 'Ready'}</span>
    </div>
  </div>

  <!-- Course selector -->
  <div class="bg-ifa-card rounded-xl border border-ifa-border p-3 flex flex-col sm:flex-row sm:items-center gap-2">
    <label for="tutor-course" class="text-xs font-semibold text-ifa-text-secondary shrink-0">Focus course:</label>
    <select
      id="tutor-course"
      bind:value={selectedCourseId}
      on:change={onCourseChange}
      class="w-full sm:max-w-sm text-sm rounded-lg bg-ifa-card-muted border border-ifa-border px-3 py-2 text-ifa-text-primary focus:outline-none focus:ring-1 focus:ring-ifa-pine"
    >
      <option value="">General (no specific course)</option>
      {#each courseList as course}
        <option value={course.id}>{course.title}</option>
      {/each}
    </select>
  </div>

  <!-- Chat Area -->
  <div class="flex-1 bg-ifa-card rounded-xl border border-ifa-border shadow-card overflow-hidden flex flex-col min-h-0">
    <!-- Messages -->
    <div class="flex-1 overflow-y-auto p-4 md:p-6 space-y-4">
      {#each messages as msg}
        <div class="flex {msg.role === 'user' ? 'justify-end' : 'justify-start'}">
          <div class="max-w-[85%] rounded-2xl p-4 {msg.role === 'user'
            ? 'bg-ifa-pine text-white'
            : 'bg-ifa-bg-warm text-ifa-text-primary border border-ifa-border'}">
            {#if msg.role === 'assistant'}
              <div class="prose prose-sm max-w-none dark:prose-invert prose-p:my-1 prose-pre:my-1">
                {@html renderMarkdown(msg.content)}
              </div>
            {:else}
              <p class="text-sm leading-relaxed whitespace-pre-wrap">{msg.content}</p>
            {/if}
          </div>
        </div>
      {/each}
      {#if sending}
        <div class="flex items-center gap-2 text-sm text-ifa-text-muted">
          <Loader2 class="w-4 h-4 animate-spin" />
          <span>IFA is preparing an answer…</span>
        </div>
      {/if}
    </div>

    <!-- Quick Topics -->
    <div class="px-4 md:px-6 py-3 border-t border-ifa-border">
      <p class="text-xs text-ifa-text-secondary mb-2">Quick topics:</p>
      <div class="flex gap-2 flex-wrap">
        {#each quickTopics as topic}
          <button
            class="flex items-center gap-2 px-3 py-1.5 rounded-full bg-ifa-bg-warm border border-ifa-border text-xs text-ifa-text-secondary hover:bg-ifa-border/50 transition-colors"
            on:click={() => (message = topic.label)}
          >
            <svelte:component this={topic.icon} class="w-3 h-3" />
            {topic.label}
          </button>
        {/each}
      </div>
    </div>

    <!-- Input Area -->
    <div class="p-4 border-t border-ifa-border bg-ifa-bg-warm">
      {#if error}
        <p role="alert" class="text-sm text-red-700 mb-3">{error}</p>
      {/if}
      <div class="flex gap-3">
        <input
          type="text"
          bind:value={message}
          placeholder="Ask me anything about your courses..."
          class="flex-1 px-4 py-3 rounded-xl border border-ifa-border bg-white text-ifa-text-primary placeholder:text-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine transition-all"
          disabled={sending}
          on:keydown={(e) => e.key === 'Enter' && sendMessage()}
        />
        <button
          on:click={sendMessage}
          disabled={sending || !message.trim()}
          class="px-5 py-3 rounded-xl bg-ifa-pine text-white font-medium hover:bg-ifa-pine-dark transition-colors flex items-center gap-2 disabled:opacity-60"
        >
          <Send class="w-4 h-4" />
          Send
        </button>
      </div>
    </div>
  </div>
</div>
