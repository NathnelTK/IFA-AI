<script lang="ts">
  import { Mic, ArrowRight, Sparkles, Check, Edit2, Play, BookOpen, Clock, Youtube } from 'lucide-svelte';
  import type { PipelineModuleProposal } from '../types';

  export let onGoalSubmit = (goal: string) => {};

  let goalInput = '';
  let isListening = false;
  let conversationState: 'idle' | 'scoping' | 'pipeline_ready' | 'generating' = 'idle';

  // Scoping inputs
  let hoursPerWeek = '5';
  let preferredChannel = 'Nick Chapsas & FreeCodeCamp';

  // Pipeline proposal modules
  let pipelineModules: PipelineModuleProposal[] = [
    {
      id: 1,
      title: 'REST APIs with ASP.NET Core & C#',
      summary: 'Foundations of Minimal APIs, Controllers, routing, and HTTP verbs.',
      estimatedHours: 4,
      topics: ['Minimal APIs', 'Endpoints & DTOs', 'Dependency Injection']
    },
    {
      id: 2,
      title: 'Database Architecture with EF Core',
      summary: 'Data modeling, migrations, PostgreSQL integration, and repository patterns.',
      estimatedHours: 5,
      topics: ['Entity Framework Core', 'LINQ queries', 'Database Migrations']
    },
    {
      id: 3,
      title: 'Authentication & JWT Security',
      summary: 'Securing endpoints, bearer tokens, role claims, and policy middleware.',
      estimatedHours: 3,
      topics: ['JWT Auth', 'Password Hashing', 'Role-based authorization']
    },
    {
      id: 4,
      title: 'Architecture, Clean Code & Testing',
      summary: 'Clean/Onion architecture principles, unit testing, and Docker deployment.',
      estimatedHours: 4,
      topics: ['Clean Architecture', 'xUnit Tests', 'Docker Containerization']
    }
  ];

  function handleStartScoping(prompt?: string) {
    if (prompt) goalInput = prompt;
    if (!goalInput.trim()) return;
    conversationState = 'scoping';
  }

  function handleApprovePipeline() {
    conversationState = 'generating';
    setTimeout(() => {
      onGoalSubmit(goalInput);
      conversationState = 'idle';
    }, 2000);
  }

  function toggleVoice() {
    isListening = !isListening;
    if (isListening) {
      goalInput = 'I want to prepare for my Computer Science exit exam, focusing on C# backend APIs.';
      setTimeout(() => {
        isListening = false;
        handleStartScoping();
      }, 1500);
    }
  }
</script>

<div class="relative overflow-hidden rounded-3xl border border-ifa-border bg-ifa-card shadow-card">
  <!-- Scenic Background Wallpaper -->
  <div
    class="absolute inset-0 bg-cover bg-right opacity-85 pointer-events-none"
    style="background-image: url('https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=1200&auto=format&fit=crop&q=80');"
  ></div>

  <!-- Warm Editorial Gradient Overlay for High Readability -->
  <div class="absolute inset-0 bg-gradient-to-r from-[#F8F6F0] via-[#F8F6F0]/90 to-transparent w-full md:w-3/4"></div>

  <!-- Content Container -->
  <div class="relative z-10 p-8 md:p-10 max-w-2xl">
    <div class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-ifa-pine/10 text-ifa-pine text-[11px] font-mono tracking-wider font-semibold uppercase mb-3">
      // AI LEARNING ASSISTANT
    </div>

    <h2 class="text-3xl font-extrabold tracking-tight text-ifa-pine leading-tight mb-2">
      What do you want to learn today?
    </h2>
    <p class="text-xs text-ifa-text-secondary font-medium leading-relaxed mb-6">
      Tell IFA your goal, and we'll create a personalized learning path just for you.
    </p>

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
          on:keydown={(e) => e.key === 'Enter' && handleStartScoping()}
          placeholder="Type or speak your goal..."
          class="w-full pl-13 pr-14 py-3.5 text-xs rounded-full bg-ifa-card/95 backdrop-blur-sm border border-ifa-border text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-2 focus:ring-ifa-pine/30 shadow-soft"
        />

        <button
          type="button"
          on:click={() => handleStartScoping()}
          class="absolute right-2 w-8 h-8 rounded-full bg-ifa-pine text-white flex items-center justify-center hover:bg-ifa-pine-light transition shadow-sm"
        >
          <ArrowRight class="w-4 h-4" />
        </button>
      </div>

      <!-- Quick Action Suggestion Chips (from mockup) -->
      <div class="flex flex-wrap items-center gap-2">
        <button
          type="button"
          on:click={() => handleStartScoping('Prepare for my exit exam')}
          class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft"
        >
          Prepare for my exit exam
        </button>
        <button
          type="button"
          on:click={() => handleStartScoping('Learn C# from scratch')}
          class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft"
        >
          Learn C# from scratch
        </button>
        <button
          type="button"
          on:click={() => handleStartScoping('Improve my math skills')}
          class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft"
        >
          Improve my math skills
        </button>
        <button
          type="button"
          on:click={() => handleStartScoping('Explore public courses')}
          class="px-3.5 py-1.5 rounded-full text-xs font-medium bg-ifa-card/80 hover:bg-ifa-card text-ifa-text-primary border border-ifa-border transition shadow-soft"
        >
          Explore public courses
        </button>
      </div>

    <!-- State 2: Conversational Scoper Modal (User Requested Feature) -->
    {:else if conversationState === 'scoping'}
      <div class="bg-ifa-card/95 backdrop-blur-md rounded-2xl p-5 border border-ifa-border shadow-elevated space-y-4">
        <div class="flex items-center justify-between border-b border-ifa-border-light pb-2.5">
          <div class="flex items-center gap-2 text-xs font-bold text-ifa-pine">
            <Sparkles class="w-4 h-4 text-emerald-600" />
            <span>IFA Conversational Scoper</span>
          </div>
          <button on:click={() => (conversationState = 'idle')} class="text-[11px] text-ifa-text-muted hover:text-ifa-text-primary">
            Cancel
          </button>
        </div>

        <p class="text-xs text-ifa-text-primary">
          Goal: <strong class="text-ifa-pine">"{goalInput}"</strong>
        </p>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">
              <Clock class="w-3 h-3 inline mr-1 text-ifa-accent-amber" /> Weekly Time Commitment
            </label>
            <select
              bind:value={hoursPerWeek}
              class="w-full text-xs rounded-xl bg-ifa-card-muted border border-ifa-border p-2 focus:ring-1 focus:ring-ifa-pine"
            >
              <option value="3">3 hours / week (Paced)</option>
              <option value="5">5 hours / week (Standard)</option>
              <option value="10">10 hours / week (Intensive)</option>
            </select>
          </div>

          <div>
            <label class="block text-[11px] font-semibold text-ifa-text-secondary mb-1">
              <Youtube class="w-3 h-3 inline mr-1 text-red-600" /> Preferred Video Creators
            </label>
            <input
              type="text"
              bind:value={preferredChannel}
              placeholder="e.g. FreeCodeCamp, Traversy Media"
              class="w-full text-xs rounded-xl bg-ifa-card-muted border border-ifa-border p-2 focus:ring-1 focus:ring-ifa-pine"
            />
          </div>
        </div>

        <button
          type="button"
          on:click={() => (conversationState = 'pipeline_ready')}
          class="w-full py-2.5 rounded-xl bg-ifa-pine hover:bg-ifa-pine-light text-white text-xs font-bold transition flex items-center justify-center gap-2"
        >
          <span>Generate Learning Pipeline</span>
          <ArrowRight class="w-3.5 h-3.5" />
        </button>
      </div>

    <!-- State 3: Interactive Pipeline Card (User Requested JIT feature) -->
    {:else if conversationState === 'pipeline_ready'}
      <div class="bg-ifa-card/95 backdrop-blur-md rounded-2xl p-5 border border-ifa-border shadow-elevated space-y-3">
        <div class="flex items-center justify-between border-b border-ifa-border-light pb-2">
          <span class="text-xs font-bold text-ifa-pine flex items-center gap-1.5">
            <BookOpen class="w-3.5 h-3.5 text-emerald-600" /> Proposed Course Pipeline (JIT Generation)
          </span>
          <span class="text-[10px] px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 font-semibold">
            Module 1 Ready on Start
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
                <div class="flex gap-1.5 mt-1">
                  {#each mod.topics as topic}
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
            <span>Approve & Generate Module 1</span>
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

    <!-- State 4: Generating JIT Module -->
    {:else if conversationState === 'generating'}
      <div class="bg-ifa-card/95 backdrop-blur-md rounded-2xl p-6 border border-ifa-border shadow-elevated text-center space-y-3">
        <div class="w-10 h-10 mx-auto rounded-full bg-ifa-pine/10 flex items-center justify-center text-ifa-pine animate-spin">
          <Sparkles class="w-5 h-5" />
        </div>
        <p class="text-xs font-bold text-ifa-pine">Synthesizing Scholarxiv Research & Curating Videos...</p>
        <p class="text-[11px] text-ifa-text-secondary">Generating Module 1 via Fine-Tuned Course Model in &lt;3s</p>
      </div>
    {/if}
  </div>
</div>
