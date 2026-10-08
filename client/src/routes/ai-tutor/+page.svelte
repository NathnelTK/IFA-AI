<script lang="ts">
  import { Bot, Send, Sparkles, BookOpen, Clock, CheckCircle2 } from 'lucide-svelte';
  import { aiApi } from '$lib/api';

  let message = '';
  let sending = false;
  let error = '';
  let messages: Array<{ role: 'user' | 'assistant'; content: string }> = [
    {
      role: 'assistant',
      content: 'Hello! I can help you prepare for the Ethiopian Grade 12 Natural Science entrance exam. Which subject or question should we work on?'
    }
  ];

  async function sendMessage() {
    const question = message.trim();
    if (!question || sending) return;

    const history = [...messages];
    messages = [...messages, { role: 'user', content: question }];
    message = '';
    error = '';
    sending = true;

    try {
      const response = await aiApi.tutorChat({ message: question, history });
      messages = [...messages, { role: 'assistant', content: response.replyMarkdown }];
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The tutor request failed.';
    } finally {
      sending = false;
    }
  }

  const quickTopics = [
    { icon: BookOpen, label: 'Mathematics', color: 'ifa-accent-blue' },
    { icon: Sparkles, label: 'Physics', color: 'ifa-accent-purple' },
    { icon: Clock, label: 'Chemistry', color: 'ifa-accent-green' },
    { icon: CheckCircle2, label: 'Biology', color: 'ifa-accent-orange' }
  ];
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1400px] mx-auto h-[calc(100vh-4rem)] flex flex-col">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div class="flex items-center gap-3">
      <div class="w-10 h-10 rounded-xl bg-ifa-pine flex items-center justify-center text-white">
        <Bot class="w-5 h-5" />
      </div>
      <div>
        <h1 class="text-2xl font-bold text-ifa-text-primary">AI Tutor</h1>
        <p class="text-sm text-ifa-text-secondary">Live exam-prep tutor powered by the backend AI service</p>
      </div>
    </div>
    <div class="flex items-center gap-2">
      <span class="w-2 h-2 rounded-full {sending ? 'bg-amber-500 animate-pulse' : 'bg-emerald-500'}"></span>
      <span class="text-xs text-ifa-text-secondary font-medium">{sending ? 'Thinking…' : 'Ready'}</span>
    </div>
  </div>

  <!-- Chat Area -->
  <div class="flex-1 bg-ifa-card rounded-xl border border-ifa-border shadow-card overflow-hidden flex flex-col">
    <!-- Messages -->
    <div class="flex-1 overflow-y-auto p-6 space-y-4">
      {#each messages as msg}
        <div class="flex {msg.role === 'user' ? 'justify-end' : 'justify-start'}">
          <div class="max-w-[80%] rounded-2xl p-4 {msg.role === 'user'
            ? 'bg-ifa-pine text-white'
            : 'bg-ifa-bg-warm text-ifa-text-primary border border-ifa-border'}">
            <p class="text-sm leading-relaxed">{msg.content}</p>
          </div>
        </div>
      {/each}
      {#if sending}
        <p class="text-sm text-ifa-text-muted">IFA is preparing an answer…</p>
      {/if}
    </div>

    <!-- Quick Topics -->
    <div class="px-6 py-3 border-t border-ifa-border">
      <p class="text-xs text-ifa-text-secondary mb-2">Quick topics:</p>
      <div class="flex gap-2 flex-wrap">
        {#each quickTopics as topic}
          <button
            class="flex items-center gap-2 px-3 py-1.5 rounded-full bg-ifa-bg-warm border border-ifa-border text-xs text-ifa-text-secondary hover:bg-ifa-border/50 transition-colors"
            on:click={() => message = topic.label}
          >
            <svelte:component this={topic.icon} class="w-3 h-3 text-{topic.color}" />
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
          class="px-5 py-3 rounded-xl bg-ifa-pine text-white font-medium hover:bg-ifa-pine-dark transition-colors flex items-center gap-2"
        >
          <Send class="w-4 h-4" />
          Send
        </button>
      </div>
    </div>
  </div>
</div>