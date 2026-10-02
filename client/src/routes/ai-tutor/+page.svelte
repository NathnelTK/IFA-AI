<script lang="ts">
  import { Bot, Send, Sparkles, BookOpen, Clock, CheckCircle2 } from 'lucide-svelte';

  let message = '';
  let messages = [
    {
      role: 'assistant',
      content: 'Hello! I\'m your AI Tutor. I can help you with C# backend development, explain concepts, answer questions, and guide you through your learning journey. What would you like to learn today?'
    }
  ];

  function sendMessage() {
    if (!message.trim()) return;

    messages = [...messages, { role: 'user', content: message }];
    message = '';

    // Simulate AI response
    setTimeout(() => {
      messages = [...messages, {
        role: 'assistant',
        content: 'Great question! Based on your current progress in C# Backend Development, I can help you understand REST APIs, database design with EF Core, or authentication with JWT. Which topic interests you most?'
      }];
    }, 1000);
  }

  const quickTopics = [
    { icon: BookOpen, label: 'REST APIs', color: 'ifa-accent-blue' },
    { icon: Sparkles, label: 'LINQ Queries', color: 'ifa-accent-purple' },
    { icon: Clock, label: 'Async Programming', color: 'ifa-accent-green' },
    { icon: CheckCircle2, label: 'Unit Testing', color: 'ifa-accent-orange' }
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
        <p class="text-sm text-ifa-text-secondary">Your personal learning assistant</p>
      </div>
    </div>
    <div class="flex items-center gap-2">
      <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
      <span class="text-xs text-ifa-text-secondary font-medium">Online</span>
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
      <div class="flex gap-3">
        <input
          type="text"
          bind:value={message}
          placeholder="Ask me anything about your courses..."
          class="flex-1 px-4 py-3 rounded-xl border border-ifa-border bg-white text-ifa-text-primary placeholder:text-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/20 focus:border-ifa-pine transition-all"
          on:keydown={(e) => e.key === 'Enter' && sendMessage()}
        />
        <button
          on:click={sendMessage}
          class="px-5 py-3 rounded-xl bg-ifa-pine text-white font-medium hover:bg-ifa-pine-dark transition-colors flex items-center gap-2"
        >
          <Send class="w-4 h-4" />
          Send
        </button>
      </div>
    </div>
  </div>
</div>