<script lang="ts">
  import { goto } from '$app/navigation';
  import { onMount } from 'svelte';
  import {
    ArrowRight,
    Play,
    Check,
    Sparkles,
    Bot,
    Layers,
    BookOpen,
    Mic,
    Search,
    Users,
    BarChart3,
    GraduationCap,
    Code2,
    Database,
    Target,
    Shield,
    Monitor,
    Sun,
    Moon,
    Cpu,
    Server,
    Terminal,
    MessageSquare,
    FileText,
    Youtube,
    TrendingUp,
    Award,
    ListChecks,
    ChevronRight,
    Compass,
    Globe,
    Rocket,
    Waves,
    AudioLines,
    FlaskConical,
    CircuitBoard,
    Route,
    Zap
  } from 'lucide-svelte';
  import { theme, toggleTheme } from '$lib/stores/uiStore';
  import { prefersReducedMotion } from '$lib/stores/preferencesStore';
  import { voxideEnabled } from '$lib/voxide';

  function handleSignIn() {
    goto('/login');
  }

  function handleGetStarted() {
    goto('/register');
  }

  function handleWatchDemo() {
    // The sign-in page exposes a one-click demo learner so visitors can explore
    // the real app (courses, skills, progress) without registering.
    goto('/login');
  }

  function scrollTo(id: string) {
    if (typeof document === 'undefined') return;
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  /* ------------------------------------------------------------------ */
  /* Scroll-reveal action                                                */
  /* ------------------------------------------------------------------ */

  /**
   * Adds a staggered fade-up once an element enters the viewport. The hidden
   * initial state is gated behind `html.lp-motion-ready`, which is only set on
   * mount — so with JS disabled (or before hydration) all content stays
   * visible instead of being stuck at opacity 0.
   */
  function reveal(node: HTMLElement, delay = 0) {
    node.classList.add('lp-reveal');
    node.style.setProperty('--lp-delay', `${delay}ms`);
    if (typeof IntersectionObserver === 'undefined') {
      node.classList.add('is-visible');
      return {};
    }
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            observer.unobserve(entry.target);
          }
        }
      },
      { rootMargin: '0px 0px -8% 0px', threshold: 0.12 }
    );
    observer.observe(node);
    return {
      destroy() {
        observer.disconnect();
      }
    };
  }

  /* ------------------------------------------------------------------ */
  /* Hero pipeline animation                                             */
  /* ------------------------------------------------------------------ */

  const pipelineStages = [
    { label: 'Intake' },
    { label: 'Blueprint' },
    { label: 'Research' },
    { label: 'Module 1' }
  ];

  let stage = 0;
  let autoPlay = true;

  const blueprintRows = [
    { n: '01', title: 'Foundations & mental model', meta: '4 topics · 5h' },
    { n: '02', title: 'APIs, routing & persistence', meta: '5 topics · 7h' },
    { n: '03', title: 'Testing & deployment', meta: '4 topics · 6h' }
  ];

  const researchRows = [
    { icon: FlaskConical, label: 'ScholarXiv · academic papers', meta: 'peer-reviewed sources', tone: 'purple' },
    { icon: Globe, label: 'Web resources', meta: 'official docs & references', tone: 'blue' },
    { icon: Youtube, label: 'YouTube walkthroughs', meta: 'matched to your creator', tone: 'pink' }
  ];

  function selectStage(index: number) {
    stage = index;
    autoPlay = false;
  }

  onMount(() => {
    if (typeof document !== 'undefined') {
      document.documentElement.classList.add('lp-motion-ready');
    }

    const reduced =
      prefersReducedMotion() ||
      (typeof document !== 'undefined' && document.documentElement.dataset.reducedMotion === 'true');

    if (reduced) {
      autoPlay = false;
      return () => {
        if (typeof document !== 'undefined') document.documentElement.classList.remove('lp-motion-ready');
      };
    }

    const timer = window.setInterval(() => {
      if (autoPlay) stage = (stage + 1) % pipelineStages.length;
    }, 3600);

    return () => {
      window.clearInterval(timer);
      if (typeof document !== 'undefined') document.documentElement.classList.remove('lp-motion-ready');
    };
  });

  /* ------------------------------------------------------------------ */
  /* Content                                                             */
  /* ------------------------------------------------------------------ */

  const partners = [
    { icon: FlaskConical, name: 'ScholarXiv', role: 'Academic research' },
    { icon: AudioLines, name: 'Voxide', role: 'Voice navigation' },
    { icon: Sparkles, name: 'Gemini', role: 'Conversational AI' },
    { icon: Zap, name: 'Groq', role: 'Fast inference' },
    { icon: Youtube, name: 'YouTube', role: 'Video sources' },
    { icon: Database, name: 'PostgreSQL', role: 'Learner state' }
  ];

  const pipeline = [
    {
      step: '01',
      model: 'Model 1',
      name: 'Conversational advisor',
      icon: MessageSquare,
      accent: 'green',
      summary:
        'You chat. IFA asks the questions that actually matter and turns your answers into a learner profile instead of guessing.',
      points: [
        'Understands your goal in plain language — no course codes',
        'Captures hours per week, preferred creators, existing materials',
        'Only proposes a course once your profile is complete'
      ]
    },
    {
      step: '02',
      model: 'Model 2',
      name: 'Course architect',
      icon: Route,
      accent: 'blue',
      summary:
        'A reviewable blueprint lands before a single lesson is written — modules, key topics and hour estimates you can edit.',
      points: [
        'Module-by-module plan with topics and estimated hours',
        'Paste links, docs or repos the course must consider',
        'Nothing is generated until you approve the blueprint'
      ]
    },
    {
      step: '03',
      model: 'Model 3',
      name: 'Course builder',
      icon: CircuitBoard,
      accent: 'purple',
      summary:
        'Lessons and exam-style quizzes are written one module at a time, grounded in research and shaped by your assessment results.',
      points: [
        'ScholarXiv papers, web and YouTube gathered per module',
        'Lesson + quiz generated on demand, saved to the course',
        'Adaptive: weak areas return with more depth and practice'
      ]
    }
  ];

  const features = [
    {
      icon: Layers,
      accent: 'green',
      title: 'Three-model pipeline',
      body: 'Intake, architecture and building are separate models with separate jobs — not one prompt asked to do everything.'
    },
    {
      icon: FlaskConical,
      accent: 'purple',
      title: 'ScholarXiv research',
      body: 'Academic search through the ScholarXiv MCP, plus web and YouTube, saved as a reusable research package per course.'
    },
    {
      icon: Mic,
      accent: 'pink',
      title: 'Voxide voice control',
      body: 'Talk to the app: jump to a section, start a course, or open the tutor. Voice maps to real capabilities, not a mock transcript.'
    },
    {
      icon: TrendingUp,
      accent: 'amber',
      title: 'Adaptive engine',
      body: 'Assessment outcomes feed back into the plan, so the next module leans into the skills you have not locked in yet.'
    },
    {
      icon: Zap,
      accent: 'orange',
      title: 'Just-in-time generation',
      body: 'Modules materialize when you reach them. Course one opens with a finished lesson and quiz, not a placeholder.'
    },
    {
      icon: BarChart3,
      accent: 'blue',
      title: 'Skills & progress',
      body: 'Skill breakdowns and progress analytics computed from your real assessment data, with weak areas called out.'
    },
    {
      icon: Users,
      accent: 'green',
      title: 'Social learning',
      body: 'Share a course with an enrollment code, publish it to the marketplace, and benchmark your skills against peers.'
    },
    {
      icon: Search,
      accent: 'purple',
      title: 'Keyboard-first search',
      body: 'A ⌘K command palette jumps across courses, skills, research and pages without touching the mouse.'
    }
  ];

  const voiceCapabilities = [
    {
      icon: Compass,
      trigger: '“Take me to my progress”',
      title: 'Navigate',
      body: 'Moves the app to any section: home, courses, marketplace, recommendations, skills, progress, research, settings or tutor.'
    },
    {
      icon: Rocket,
      trigger: '“I want to learn GraphQL”',
      title: 'Create a course',
      body: 'Runs the real course creation flow with your topic and weekly hours, then opens the generated course.'
    },
    {
      icon: GraduationCap,
      trigger: '“Explain pointers”',
      title: 'Open the tutor',
      body: 'Opens the AI tutor focused on the topic you named, ready to explain or drill you.'
    }
  ];

  const stack = [
    {
      icon: Monitor,
      title: 'Frontend',
      items: ['SvelteKit 2 · Svelte 5', 'TypeScript', 'Tailwind CSS design system', 'Lucide icon set']
    },
    {
      icon: Server,
      title: 'Backend',
      items: ['ASP.NET Core (.NET 10)', 'Clean Architecture layers', 'Entity Framework Core', 'PostgreSQL']
    },
    {
      icon: Cpu,
      title: 'AI & integrations',
      items: ['Gemini — conversational AI', 'Groq — fast inference', 'ScholarXiv MCP — research', 'Voxide — voice interface']
    }
  ];

  const domains = [
    { name: 'Engineering', icon: Code2 },
    { name: 'Medicine & Health', icon: Target },
    { name: 'Computer Science', icon: Terminal },
    { name: 'Science', icon: FlaskConical },
    { name: 'Business', icon: Award }
  ];

  const checkpoints = [
    { icon: MessageSquare, label: 'Chat your goal' },
    { icon: Route, label: 'Review the blueprint' },
    { icon: FlaskConical, label: 'Sources are researched' },
    { icon: ListChecks, label: 'Lessons & quizzes built' }
  ];

  const stats = [
    { value: '3', label: 'AI models in the pipeline' },
    { value: '9', label: 'learning surfaces in-app' },
    { value: '5', label: 'domains supported' },
    { value: 'JIT', label: 'module-by-module generation' }
  ];
</script>

<svelte:head>
  <title>IFA — Your AI Learning Companion</title>
  <meta
    name="description"
    content="IFA turns a conversation into a personalized course: a conversational advisor learns your goal, a course architect drafts a blueprint you approve, and a builder writes research-backed lessons — grounded in ScholarXiv research with hands-free Voxide voice navigation."
  />
</svelte:head>

<div class="lp-root min-h-screen bg-ifa-bg text-ifa-text-primary antialiased">
  <!-- ================= Navigation ================= -->
  <nav class="fixed top-0 left-0 right-0 z-50 border-b border-ifa-border lp-nav">
    <div class="max-w-7xl mx-auto px-6 py-3.5 flex items-center justify-between gap-4">
      <button type="button" class="flex items-center gap-3 group" on:click={() => scrollTo('top')}>
        <span class="w-10 h-10 rounded-xl bg-ifa-pine text-white flex items-center justify-center shadow-sm lp-logo">
          <GraduationCap class="w-5 h-5" />
        </span>
        <span class="text-left leading-tight">
          <span class="block text-lg font-extrabold tracking-tight text-ifa-text-primary">IFA</span>
          <span class="block text-[10px] font-semibold uppercase tracking-wider text-ifa-text-muted">
            AI Learning Companion
          </span>
        </span>
      </button>

      <div class="hidden lg:flex items-center gap-1">
        {#each [
          { id: 'how', label: 'How it works' },
          { id: 'features', label: 'Features' },
          { id: 'voice', label: 'Voice' },
          { id: 'research', label: 'Research' },
          { id: 'stack', label: 'Stack' }
        ] as link}
          <button
            type="button"
            on:click={() => scrollTo(link.id)}
            class="px-3 py-2 rounded-lg text-sm font-medium text-ifa-text-secondary hover:text-ifa-text-primary hover:bg-ifa-card-muted transition"
          >
            {link.label}
          </button>
        {/each}
      </div>

      <div class="flex items-center gap-2">
        <button
          type="button"
          on:click={toggleTheme}
          title="Toggle {$theme === 'dark' ? 'light' : 'dark'} mode"
          aria-label="Toggle theme"
          class="w-9 h-9 rounded-xl border border-ifa-border bg-ifa-card text-ifa-text-secondary hover:text-ifa-text-primary transition flex items-center justify-center"
        >
          {#if $theme === 'dark'}
            <Sun class="w-4 h-4" />
          {:else}
            <Moon class="w-4 h-4" />
          {/if}
        </button>
        <button
          type="button"
          on:click={handleSignIn}
          class="hidden sm:inline-flex px-3.5 py-2 rounded-xl text-sm font-semibold text-ifa-text-secondary hover:text-ifa-text-primary transition"
        >
          Sign in
        </button>
        <button
          type="button"
          on:click={handleGetStarted}
          class="lp-btn-primary px-4 py-2.5 rounded-xl bg-ifa-pine text-white text-sm font-semibold transition shadow-sm flex items-center gap-1.5"
        >
          <span class="relative z-10">Get started</span>
          <ArrowRight class="relative z-10 w-4 h-4" />
        </button>
      </div>
    </div>
  </nav>

  <!-- ================= Hero ================= -->
  <section id="top" class="relative overflow-hidden pt-32 pb-20 px-6">
    <div class="absolute inset-0 lp-hero-bg pointer-events-none" aria-hidden="true">
      <div class="absolute inset-0 lp-grid"></div>
      <div class="absolute lp-blob lp-blob-a"></div>
      <div class="absolute lp-blob lp-blob-b"></div>
      <div class="absolute lp-blob lp-blob-c"></div>
      <svg class="lp-wave lp-wave-slow" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
        <path class="lp-wave-path-1" d="M0 160 C 360 260 720 60 1080 160 C 1440 260 1800 60 2160 160 C 2520 260 2700 110 2880 160 L 2880 320 L 0 320 Z" />
      </svg>
      <svg class="lp-wave lp-wave-mid" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
        <path class="lp-wave-path-2" d="M0 200 C 480 100 960 300 1440 200 C 1920 100 2400 300 2880 200 L 2880 320 L 0 320 Z" />
      </svg>
      <svg class="lp-wave lp-wave-fast" viewBox="0 0 2880 320" preserveAspectRatio="none" fill="none">
        <path class="lp-wave-path-3" d="M0 240 C 720 160 1440 320 2160 240 C 2520 200 2700 260 2880 240 L 2880 320 L 0 320 Z" />
      </svg>
    </div>

    <div class="relative max-w-7xl mx-auto">
      <div class="grid grid-cols-1 lg:grid-cols-[1.05fr_0.95fr] gap-14 items-center">
        <!-- Copy -->
        <div class="space-y-7">
          <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full border border-ifa-pine/30 bg-ifa-card/80 backdrop-blur">
            <span class="lp-live-dot"></span>
            <span class="text-[11px] font-bold uppercase tracking-wider text-ifa-pine">
              STARK Hackathon 2026 · Team XOR
            </span>
          </div>

          <h1
            class="text-4xl md:text-5xl lg:text-[3.35rem] font-extrabold tracking-tight leading-[1.06] text-ifa-text-primary"
          >
            Tell IFA your goal.<br />
            Get a course <span class="lp-underline">built around it</span>.
          </h1>

          <p class="text-lg text-ifa-text-secondary max-w-xl leading-relaxed">
            IFA runs a three-model pipeline: a conversational advisor learns what you are trying to achieve, a course
            architect drafts a blueprint you approve, and a builder writes the lessons — grounded in
            <strong class="text-ifa-text-primary">ScholarXiv</strong> academic research and YouTube, generated one module at a
            time. Then run the whole app hands-free with <strong class="text-ifa-text-primary">Voxide</strong> voice navigation.
          </p>

          <div class="flex flex-wrap items-center gap-3">
            <button
              type="button"
              on:click={handleGetStarted}
              class="lp-btn-primary px-6 py-3.5 rounded-xl bg-ifa-pine text-white text-sm font-bold transition shadow-elevated flex items-center gap-2"
            >
              <span class="relative z-10">Start learning free</span>
              <ArrowRight class="relative z-10 w-4 h-4" />
            </button>
            <button
              type="button"
              on:click={handleWatchDemo}
              class="px-6 py-3.5 rounded-xl border border-ifa-border bg-ifa-card/90 backdrop-blur text-ifa-text-primary text-sm font-bold hover:bg-ifa-card-muted transition flex items-center gap-2"
            >
              <Play class="w-4 h-4" />
              Explore the live demo
            </button>
          </div>

          <p class="text-xs text-ifa-text-muted">
            One-click demo learner on the sign-in page — no setup, no credit card, real data.
          </p>

          <div class="flex flex-wrap gap-2 pt-1">
            {#each [
              { icon: Layers, label: '3-model pipeline' },
              { icon: FlaskConical, label: 'ScholarXiv research' },
              { icon: Mic, label: 'Voxide voice' },
              { icon: Zap, label: 'JIT modules' }
            ] as chip}
              <span
                class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full border border-ifa-border bg-ifa-card/90 backdrop-blur text-[11px] font-semibold text-ifa-text-secondary"
              >
                <svelte:component this={chip.icon} class="w-3.5 h-3.5 text-ifa-pine" />
                {chip.label}
              </span>
            {/each}
          </div>
        </div>

        <!-- Animated pipeline mock -->
        <div class="relative lp-float-in">
          <div class="lp-stage-ring" aria-hidden="true"></div>

          <div class="relative rounded-3xl border border-ifa-border bg-ifa-card shadow-elevated overflow-hidden">
            <div class="flex items-center gap-2 px-4 py-3 border-b border-ifa-border bg-ifa-card-muted/60">
              <span class="w-2.5 h-2.5 rounded-full bg-ifa-accent-pink"></span>
              <span class="w-2.5 h-2.5 rounded-full bg-ifa-accent-amber"></span>
              <span class="w-2.5 h-2.5 rounded-full bg-ifa-accent-green"></span>
              <span class="ml-2 text-[11px] font-semibold text-ifa-text-muted">IFA Studio · live pipeline</span>
              <span class="ml-auto flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wide text-ifa-pine">
                <span class="lp-live-dot"></span> live
              </span>
            </div>

            <div class="grid grid-cols-4 gap-1.5 p-3">
              {#each pipelineStages as s, i}
                <button
                  type="button"
                  on:click={() => selectStage(i)}
                  class="lp-step {i === stage ? 'is-active' : ''} {i < stage ? 'is-done' : ''}"
                  aria-label={`Show ${s.label} stage`}
                >
                  {s.label}
                </button>
              {/each}
            </div>

            <div class="px-4 pb-4 h-[292px]">
              {#key stage}
                <div class="lp-stage-body h-full flex flex-col">
                  {#if stage === 0}
                    <div class="space-y-2.5 flex-1">
                      <div class="lp-bubble lp-bubble-user">I want to prepare for my exit exam — C# backend APIs.</div>
                      <div class="lp-bubble lp-bubble-ai">
                        <Bot class="w-3.5 h-3.5 text-ifa-pine shrink-0 mt-0.5" />
                        <span>Got it. How many hours a week can you realistically study?</span>
                      </div>
                      <div class="lp-bubble lp-bubble-user">About 6 hours.</div>
                      <div class="lp-bubble lp-bubble-ai">
                        <Bot class="w-3.5 h-3.5 text-ifa-pine shrink-0 mt-0.5" />
                        <span>Perfect — any material I should build from?</span>
                      </div>
                    </div>
                    <div class="flex items-center gap-2 text-[11px] text-ifa-text-muted pt-3">
                      <span class="lp-typing"><i></i><i></i><i></i></span>
                      Model 1 is assembling your learner profile
                    </div>
                  {:else if stage === 1}
                    <div class="space-y-2 flex-1">
                      <p class="text-[11px] font-bold uppercase tracking-wider text-ifa-text-muted">
                        Blueprint · waiting for your approval
                      </p>
                      {#each blueprintRows as row}
                        <div class="lp-row">
                          <span class="lp-row-num">{row.n}</span>
                          <span class="min-w-0">
                            <span class="block text-xs font-semibold text-ifa-text-primary truncate">{row.title}</span>
                            <span class="block text-[10px] text-ifa-text-muted">{row.meta}</span>
                          </span>
                          <Check class="w-3.5 h-3.5 text-ifa-pine ml-auto shrink-0" />
                        </div>
                      {/each}
                    </div>
                    <div class="flex items-center gap-2 pt-3">
                      <span class="lp-pill">Approve &amp; generate</span>
                      <span class="text-[11px] text-ifa-text-muted">nothing is written yet</span>
                    </div>
                  {:else if stage === 2}
                    <div class="space-y-2 flex-1">
                      <p class="text-[11px] font-bold uppercase tracking-wider text-ifa-text-muted">
                        Research package · saved with the course
                      </p>
                      {#each researchRows as row}
                        <div class="lp-row">
                          <span class="lp-row-icon lp-tone-{row.tone}">
                            <svelte:component this={row.icon} class="w-3.5 h-3.5" />
                          </span>
                          <span class="min-w-0">
                            <span class="block text-xs font-semibold text-ifa-text-primary truncate">{row.label}</span>
                            <span class="block text-[10px] text-ifa-text-muted">{row.meta}</span>
                          </span>
                          <Check class="w-3.5 h-3.5 text-ifa-pine ml-auto shrink-0" />
                        </div>
                      {/each}
                    </div>
                    <p class="pt-3 text-[11px] text-ifa-text-muted">
                      Cited in every lesson under <span class="font-semibold text-ifa-text-secondary">Course Sources</span>.
                    </p>
                  {:else}
                    <div class="space-y-3 flex-1">
                      <div class="flex items-center gap-2">
                        <span class="text-xs font-bold text-ifa-text-primary">Module 1 · Foundations</span>
                        <span class="lp-badge-ready">Ready</span>
                        <span class="ml-auto text-[11px] text-ifa-text-muted">learn now</span>
                      </div>
                      <div class="lp-progress-track"><span class="lp-progress-fill"></span></div>
                      {#each [
                        { label: '4 lessons written', icon: BookOpen },
                        { label: '3 exam-style quizzes', icon: ListChecks },
                        { label: 'Sources attached', icon: FileText },
                        { label: 'You are enrolled', icon: Users }
                      ] as item}
                        <div class="flex items-center gap-2 text-[11px] text-ifa-text-secondary">
                          <svelte:component this={item.icon} class="w-3.5 h-3.5 text-ifa-pine" />
                          {item.label}
                        </div>
                      {/each}
                    </div>
                    <p class="pt-3 text-[11px] text-ifa-text-muted">
                      Later modules generate on demand, reusing this research.
                    </p>
                  {/if}
                </div>
              {/key}
            </div>
          </div>

          <!-- Floating integration chips -->
          <div class="hidden xl:flex absolute -left-8 top-24 lp-chip-float">
            <FlaskConical class="w-3.5 h-3.5 text-ifa-accent-purple" />
            <span>ScholarXiv</span>
          </div>
          <div class="hidden xl:flex absolute -right-7 top-40 lp-chip-float lp-chip-delay">
            <AudioLines class="w-3.5 h-3.5 text-ifa-accent-pink" />
            <span>Voxide voice</span>
          </div>
          <div class="hidden xl:flex absolute -left-6 bottom-16 lp-chip-float lp-chip-delay-2">
            <Zap class="w-3.5 h-3.5 text-ifa-accent-amber" />
            <span>JIT modules</span>
          </div>
        </div>
      </div>
    </div>
  </section>

  <!-- ================= Partners marquee ================= -->
  <section class="py-8 border-y border-ifa-border bg-ifa-bg-warm overflow-hidden">
    <p class="text-center text-[11px] font-bold uppercase tracking-[0.2em] text-ifa-text-muted mb-6">
      Real integrations — not mock data
    </p>
    <div class="lp-marquee-mask">
      <div class="lp-marquee">
        {#each [...partners, ...partners] as partner, i}
          <div class="lp-partner" aria-hidden={i >= partners.length}>
            <svelte:component this={partner.icon} class="w-5 h-5 text-ifa-pine" />
            <span class="text-sm font-bold text-ifa-text-primary">{partner.name}</span>
            <span class="text-xs text-ifa-text-muted">{partner.role}</span>
          </div>
        {/each}
      </div>
    </div>
  </section>

  <!-- ================= How it works ================= -->
  <section id="how" class="py-24 px-6">
    <div class="max-w-7xl mx-auto">
      <div class="max-w-3xl mb-14" use:reveal>
        <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-ifa-pine mb-3">How it works</p>
        <h2 class="text-3xl md:text-4xl font-extrabold tracking-tight text-ifa-text-primary mb-4">
          Three models, three jobs, one course that fits you
        </h2>
        <p class="text-ifa-text-secondary leading-relaxed">
          Instead of asking a single prompt to invent a curriculum, IFA splits the work. Each stage hands a clean,
          inspectable artifact to the next one — and you sit in the middle of it.
        </p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-[1fr_auto_1fr_auto_1fr] gap-6 items-stretch">
        {#each pipeline as step, i}
          {#if i > 0}
            <div class="hidden lg:flex items-center justify-center lp-connector" aria-hidden="true">
              <ChevronRight class="w-5 h-5 text-ifa-text-muted" />
            </div>
          {/if}
          <div class="lp-card rounded-2xl border border-ifa-border bg-ifa-card p-6 shadow-card" use:reveal={i * 90}>
            <div class="flex items-center justify-between mb-5">
              <span class="lp-step-num">{step.step}</span>
              <span class="lp-model-chip lp-tone-{step.accent}">
                <svelte:component this={step.icon} class="w-3.5 h-3.5" />
                {step.model}
              </span>
            </div>
            <h3 class="text-lg font-bold text-ifa-text-primary mb-1">{step.name}</h3>
            <p class="text-sm text-ifa-text-secondary leading-relaxed mb-4">{step.summary}</p>
            <ul class="space-y-2">
              {#each step.points as point}
                <li class="flex items-start gap-2 text-xs text-ifa-text-secondary leading-relaxed">
                  <Check class="w-3.5 h-3.5 text-ifa-pine shrink-0 mt-0.5" />
                  <span>{point}</span>
                </li>
              {/each}
            </ul>
          </div>
        {/each}
      </div>

      <div class="mt-10 flex flex-wrap items-center gap-3" use:reveal={120}>
        {#each checkpoints as cp}
          <span class="lp-checkpoint">
            <svelte:component this={cp.icon} class="w-3.5 h-3.5 text-ifa-pine" />
            {cp.label}
          </span>
        {/each}
      </div>
    </div>
  </section>

  <!-- ================= Features ================= -->
  <section id="features" class="py-24 px-6 bg-ifa-bg-warm">
    <div class="max-w-7xl mx-auto">
      <div class="max-w-3xl mb-14" use:reveal>
        <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-ifa-pine mb-3">Platform</p>
        <h2 class="text-3xl md:text-4xl font-extrabold tracking-tight text-ifa-text-primary mb-4">
          Everything you need after the course exists
        </h2>
        <p class="text-ifa-text-secondary leading-relaxed">
          IFA is a learning companion, not a content dump: courses you can actually enrol in, skills you can measure,
          research you can revisit, and peers you can compare against.
        </p>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        {#each features as feature, i}
          <div class="lp-card rounded-2xl border border-ifa-border bg-ifa-card p-5 shadow-soft" use:reveal={i * 60}>
            <span class="lp-feature-icon lp-tone-{feature.accent}">
              <svelte:component this={feature.icon} class="w-5 h-5" />
            </span>
            <h3 class="text-sm font-bold text-ifa-text-primary mt-4 mb-1.5">{feature.title}</h3>
            <p class="text-xs text-ifa-text-secondary leading-relaxed">{feature.body}</p>
          </div>
        {/each}
      </div>
    </div>
  </section>

  <!-- ================= Voice (Voxide) ================= -->
  <section id="voice" class="py-24 px-6">
    <div class="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-14 items-center">
      <div use:reveal>
        <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-ifa-pine mb-3">Voice navigation</p>
        <h2 class="text-3xl md:text-4xl font-extrabold tracking-tight text-ifa-text-primary mb-4">
          Hands-free, powered by Voxide
        </h2>
        <p class="text-ifa-text-secondary leading-relaxed mb-6">
          Voxide maps your speech onto capabilities the app registers — so a spoken request runs the same code path as a
          click. No fake transcripts, no dead ends: if IFA cannot do it, it tells you.
        </p>

        <div class="space-y-3">
          {#each voiceCapabilities as cap}
            <div class="lp-card rounded-2xl border border-ifa-border bg-ifa-card p-4 shadow-soft flex items-start gap-3">
              <span class="lp-feature-icon lp-tone-green">
                <svelte:component this={cap.icon} class="w-5 h-5" />
              </span>
              <div class="min-w-0">
                <div class="flex flex-wrap items-center gap-2">
                  <h3 class="text-sm font-bold text-ifa-text-primary">{cap.title}</h3>
                  <span class="lp-quote">{cap.trigger}</span>
                </div>
                <p class="text-xs text-ifa-text-secondary leading-relaxed mt-1">{cap.body}</p>
              </div>
            </div>
          {/each}
        </div>

        <div class="mt-5 flex items-start gap-2 text-xs">
          {#if voxideEnabled}
            <span class="lp-badge-ready shrink-0">Key detected</span>
            <span class="text-ifa-text-secondary">
              A Voxide publishable key is configured, so the mic is armed after sign-in.
            </span>
          {:else}
            <span class="lp-badge-warn shrink-0">Key missing</span>
            <span class="text-ifa-text-secondary">
              Add <code class="font-mono text-ifa-text-primary">VITE_VOXIDE_KEY</code> (a
              <code class="font-mono text-ifa-text-primary">vox_pub_…</code> publishable key) to
              <code class="font-mono text-ifa-text-primary">client/.env</code> to arm the mic.
            </span>
          {/if}
        </div>
      </div>

      <div class="lp-voice-panel rounded-3xl border border-ifa-border p-8" use:reveal={100}>
        <div class="flex items-center justify-between mb-8">
          <span class="flex items-center gap-2">
            <AudioLines class="w-5 h-5 text-ifa-pine" />
            <span class="text-sm font-bold text-ifa-text-primary">IFA voice console</span>
          </span>
          <span class="lp-live-pill"><span class="lp-live-dot"></span> listening</span>
        </div>

        <div class="lp-wavebars" aria-hidden="true">
          {#each Array(36) as _, i}
            <span
              class="lp-bar"
              style={`--h: ${28 + ((i * 37) % 62)}%; --d: ${(i % 9) * 90}ms`}
            ></span>
          {/each}
        </div>

        <div class="mt-8 space-y-2.5">
          <div class="lp-bubble lp-bubble-user ml-auto">Create a course on data structures</div>
          <div class="lp-bubble lp-bubble-ai">
            <Bot class="w-3.5 h-3.5 text-ifa-pine shrink-0 mt-0.5" />
            <span>On it — 5 hours a week. Opening your new course…</span>
          </div>
          <div class="lp-bubble lp-bubble-user ml-auto">Take me to my progress</div>
          <div class="lp-bubble lp-bubble-ai">
            <Bot class="w-3.5 h-3.5 text-ifa-pine shrink-0 mt-0.5" />
            <span>Done. Here is your progress dashboard.</span>
          </div>
        </div>

        <p class="mt-7 text-[11px] text-ifa-text-muted leading-relaxed">
          The browser SDK only ever uses the publishable <code class="font-mono">vox_pub_…</code> key; the secret
          <code class="font-mono">vox_sk_…</code> key never leaves the server. Whitelist your origin in the Voxide
          dashboard.
        </p>
      </div>
    </div>
  </section>

  <!-- ================= Research (ScholarXiv) ================= -->
  <section id="research" class="py-24 px-6 bg-ifa-bg-warm">
    <div class="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-14 items-center">
      <div class="lp-research-card rounded-3xl border border-ifa-border bg-ifa-card p-6 shadow-elevated" use:reveal>
        <div class="flex items-center justify-between mb-5">
          <span class="flex items-center gap-2 text-sm font-bold text-ifa-text-primary">
            <FlaskConical class="w-5 h-5 text-ifa-accent-purple" />
            Research package
          </span>
          <span class="lp-model-chip lp-tone-purple">ScholarXiv MCP</span>
        </div>

        <div class="space-y-3">
          {#each [
            { icon: FlaskConical, title: 'Academic papers', meta: 'Search run through the ScholarXiv MCP server', count: 'ranked by relevance' },
            { icon: Globe, title: 'Web resources', meta: 'Official documentation and reference pages', count: 'fetched per topic' },
            { icon: Youtube, title: 'YouTube walkthroughs', meta: 'Matched to the creator you asked for', count: 'transcript-ready' }
          ] as source}
            <div class="lp-row">
              <span class="lp-row-icon lp-tone-purple">
                <svelte:component this={source.icon} class="w-3.5 h-3.5" />
              </span>
              <span class="min-w-0">
                <span class="block text-xs font-semibold text-ifa-text-primary">{source.title}</span>
                <span class="block text-[10px] text-ifa-text-muted">{source.meta}</span>
              </span>
              <span class="ml-auto text-[10px] font-semibold text-ifa-text-muted shrink-0">{source.count}</span>
            </div>
          {/each}
        </div>

        <div class="mt-5 rounded-2xl border border-ifa-border bg-ifa-card-muted/60 p-4">
          <p class="text-[11px] font-bold uppercase tracking-wider text-ifa-text-muted mb-2">In the lesson</p>
          <p class="text-xs text-ifa-text-secondary leading-relaxed">
            Sources are folded into the prompt and appended to the lesson under
            <span class="font-semibold text-ifa-text-primary">Course Sources</span> — and your own links go in there
            too, so nothing you supplied gets dropped.
          </p>
        </div>
      </div>

      <div use:reveal={100}>
        <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-ifa-pine mb-3">Research</p>
        <h2 class="text-3xl md:text-4xl font-extrabold tracking-tight text-ifa-text-primary mb-4">
          Backed by ScholarXiv, not vibes
        </h2>
        <p class="text-ifa-text-secondary leading-relaxed mb-6">
          Research runs once per course and is stored as a reusable package, so every module you generate later is
          grounded in the same evidence — without paying to search again.
        </p>
        <ul class="space-y-3">
          {#each [
            { icon: FlaskConical, text: 'ScholarXiv academic search for peer-reviewed starting points.' },
            { icon: Globe, text: 'Web references pulled for the specific topics in each module.' },
            { icon: Youtube, text: 'Video resources matched to the creator you prefer.' },
            { icon: FileText, text: 'Your pasted links and cover image are treated as first-class sources.' }
          ] as item}
            <li class="flex items-start gap-3">
              <span class="lp-feature-icon lp-tone-purple shrink-0">
                <svelte:component this={item.icon} class="w-4 h-4" />
              </span>
              <span class="text-sm text-ifa-text-secondary leading-relaxed pt-1.5">{item.text}</span>
            </li>
          {/each}
        </ul>
      </div>
    </div>
  </section>

  <!-- ================= Stats ================= -->
  <section class="py-16 px-6">
    <div class="max-w-7xl mx-auto grid grid-cols-2 lg:grid-cols-4 gap-5">
      {#each stats as stat, i}
        <div class="lp-stat rounded-2xl border border-ifa-border bg-ifa-card p-6 text-center" use:reveal={i * 70}>
          <p class="text-3xl font-extrabold tracking-tight text-ifa-pine">{stat.value}</p>
          <p class="text-xs text-ifa-text-secondary mt-1.5">{stat.label}</p>
        </div>
      {/each}
    </div>
  </section>

  <!-- ================= Domains ================= -->
  <section id="domains" class="py-16 px-6 pb-24">
    <div class="max-w-7xl mx-auto">
      <div class="text-center max-w-2xl mx-auto mb-10" use:reveal>
        <h2 class="text-2xl md:text-3xl font-extrabold tracking-tight text-ifa-text-primary mb-3">
          Built for exam prep across five domains
        </h2>
        <p class="text-sm text-ifa-text-secondary">
          Computer science, engineering, medicine and health, science and business — the pipeline adapts to the subject,
          not the other way around.
        </p>
      </div>
      <div class="flex flex-wrap justify-center gap-3" use:reveal={80}>
        {#each domains as domain}
          <span class="lp-domain">
            <svelte:component this={domain.icon} class="w-4 h-4 text-ifa-pine" />
            {domain.name}
          </span>
        {/each}
      </div>
    </div>
  </section>

  <!-- ================= Stack ================= -->
  <section id="stack" class="py-24 px-6 bg-ifa-bg-warm">
    <div class="max-w-7xl mx-auto">
      <div class="max-w-3xl mb-14" use:reveal>
        <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-ifa-pine mb-3">Under the hood</p>
        <h2 class="text-3xl md:text-4xl font-extrabold tracking-tight text-ifa-text-primary mb-4">
          Engineered, not assembled
        </h2>
        <p class="text-ifa-text-secondary leading-relaxed">
          A typed SvelteKit client on top of a clean-architecture .NET API, with AI providers and research wired in as
          first-class services.
        </p>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-5">
        {#each stack as group, i}
          <div class="lp-card rounded-2xl border border-ifa-border bg-ifa-card p-6 shadow-card" use:reveal={i * 80}>
            <span class="lp-feature-icon lp-tone-blue">
              <svelte:component this={group.icon} class="w-5 h-5" />
            </span>
            <h3 class="text-sm font-bold text-ifa-text-primary mt-4 mb-3">{group.title}</h3>
            <ul class="space-y-2">
              {#each group.items as item}
                <li class="flex items-start gap-2 text-xs text-ifa-text-secondary">
                  <Terminal class="w-3.5 h-3.5 text-ifa-text-muted shrink-0 mt-0.5" />
                  <span>{item}</span>
                </li>
              {/each}
            </ul>
          </div>
        {/each}
      </div>

      <div class="mt-6 flex flex-wrap items-center gap-3 text-xs text-ifa-text-muted" use:reveal={160}>
        <span class="inline-flex items-center gap-1.5"><Shield class="w-3.5 h-3.5" /> JWT auth &amp; learner-scoped data</span>
        <span class="lp-dot">•</span>
        <span class="inline-flex items-center gap-1.5"><Waves class="w-3.5 h-3.5" /> Responsive, theme-aware UI</span>
        <span class="lp-dot">•</span>
        <span class="inline-flex items-center gap-1.5"><BookOpen class="w-3.5 h-3.5" /> Docker &amp; Docker Compose</span>
      </div>
    </div>
  </section>

  <!-- ================= Final CTA ================= -->
  <section class="px-6 py-20">
    <div class="max-w-6xl mx-auto lp-cta rounded-3xl overflow-hidden border border-ifa-border">
      <div class="relative px-8 py-16 text-center">
        <div class="absolute inset-0 lp-cta-bg pointer-events-none" aria-hidden="true">
          <div class="absolute lp-blob lp-blob-cta"></div>
        </div>
        <div class="relative">
          <span class="lp-live-pill mb-5"><Sparkles class="w-3.5 h-3.5" /> Free to try</span>
          <h2 class="text-3xl md:text-4xl font-extrabold tracking-tight text-ifa-text-primary mb-4">
            Your next course is one conversation away
          </h2>
          <p class="text-ifa-text-secondary max-w-2xl mx-auto mb-8 leading-relaxed">
            Describe the goal, approve the blueprint, and start learning from a research-backed module today. Or sign in
            with the demo learner and let Voxide drive.
          </p>
          <div class="flex flex-wrap items-center justify-center gap-3">
            <button
              type="button"
              on:click={handleGetStarted}
              class="lp-btn-primary px-7 py-3.5 rounded-xl bg-ifa-pine text-white text-sm font-bold shadow-elevated transition flex items-center gap-2"
            >
              <span class="relative z-10">Create your free account</span>
              <ArrowRight class="relative z-10 w-4 h-4" />
            </button>
            <button
              type="button"
              on:click={handleWatchDemo}
              class="px-7 py-3.5 rounded-xl border border-ifa-border bg-ifa-card text-ifa-text-primary text-sm font-bold hover:bg-ifa-card-muted transition flex items-center gap-2"
            >
              <Play class="w-4 h-4" />
              Try the demo learner
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>

  <!-- ================= Footer ================= -->
  <footer id="contact" class="border-t border-ifa-border bg-ifa-bg px-6 py-14">
    <div class="max-w-7xl mx-auto grid grid-cols-1 md:grid-cols-[1.4fr_1fr_1fr_1.2fr] gap-10">
      <div>
        <div class="flex items-center gap-3 mb-4">
          <span class="w-9 h-9 rounded-xl bg-ifa-pine text-white flex items-center justify-center">
            <GraduationCap class="w-5 h-5" />
          </span>
          <span class="text-lg font-extrabold tracking-tight text-ifa-text-primary">IFA</span>
        </div>
        <p class="text-sm text-ifa-text-secondary leading-relaxed max-w-xs">
          Your AI learning companion. Personalized courses, research-backed lessons and hands-free navigation.
        </p>
        <div class="flex items-center gap-2 mt-5 text-[11px] font-bold uppercase tracking-wider text-ifa-text-muted">
          <span>Study</span><span class="lp-dot">•</span><span>Grow</span><span class="lp-dot">•</span><span>Achieve</span>
        </div>
      </div>

      <div>
        <h3 class="text-xs font-bold uppercase tracking-wider text-ifa-text-primary mb-4">Explore</h3>
        <ul class="space-y-2.5">
          {#each [
            { id: 'how', label: 'How it works' },
            { id: 'features', label: 'Features' },
            { id: 'voice', label: 'Voice (Voxide)' },
            { id: 'research', label: 'Research (ScholarXiv)' },
            { id: 'stack', label: 'Tech stack' }
          ] as link}
            <li>
              <button
                type="button"
                on:click={() => scrollTo(link.id)}
                class="text-sm text-ifa-text-secondary hover:text-ifa-pine transition"
              >
                {link.label}
              </button>
            </li>
          {/each}
        </ul>
      </div>

      <div>
        <h3 class="text-xs font-bold uppercase tracking-wider text-ifa-text-primary mb-4">Get started</h3>
        <ul class="space-y-2.5">
          <li>
            <button type="button" on:click={handleGetStarted} class="text-sm text-ifa-text-secondary hover:text-ifa-pine transition">
              Create an account
            </button>
          </li>
          <li>
            <button type="button" on:click={handleSignIn} class="text-sm text-ifa-text-secondary hover:text-ifa-pine transition">
              Sign in
            </button>
          </li>
          <li>
            <button type="button" on:click={handleWatchDemo} class="text-sm text-ifa-text-secondary hover:text-ifa-pine transition">
              Demo learner
            </button>
          </li>
        </ul>
      </div>

      <div>
        <h3 class="text-xs font-bold uppercase tracking-wider text-ifa-text-primary mb-4">Built by Team XOR</h3>
        <ul class="space-y-2.5 text-sm text-ifa-text-secondary">
          <li>Nathnel Teklemariam</li>
          <li>Ermiyas Eshetu</li>
          <li>Negede Tekleyes</li>
        </ul>
        <p class="text-xs text-ifa-text-muted mt-4">STARK Official Hackathon 2026</p>
      </div>
    </div>

    <div
      class="max-w-7xl mx-auto mt-12 pt-6 border-t border-ifa-border flex flex-col sm:flex-row items-center justify-between gap-3"
    >
      <p class="text-xs text-ifa-text-muted">© 2026 IFA. All rights reserved.</p>
      <p class="text-xs text-ifa-text-muted flex items-center gap-2">
        <Compass class="w-3.5 h-3.5" /> Research by ScholarXiv · Voice by Voxide
      </p>
    </div>
  </footer>
</div>

<style>
  /* ------------------------------------------------------------------ */
  /* Shared primitives                                                   */
  /* ------------------------------------------------------------------ */

  .lp-nav {
    background: rgba(248, 246, 240, 0.82);
    backdrop-filter: blur(14px);
  }
  :global(.dark) .lp-nav {
    background: rgba(10, 10, 10, 0.82);
  }

  .lp-btn-primary {
    position: relative;
    overflow: hidden;
  }
  .lp-btn-primary::after {
    content: '';
    position: absolute;
    inset: 0;
    background: linear-gradient(105deg, transparent 32%, rgba(255, 255, 255, 0.4) 50%, transparent 68%);
    transform: translateX(-130%);
    transition: transform 0.75s ease;
  }
  .lp-btn-primary:hover::after {
    transform: translateX(130%);
  }

  /* Highlight bar under the headline — an inline background, so it can never
     slip behind the hero layers the way a negative z-index would. */
  .lp-underline {
    background-image: linear-gradient(rgba(42, 157, 104, 0.32), rgba(42, 157, 104, 0.32));
    background-repeat: no-repeat;
    background-size: 100% 0.34em;
    background-position: 0 88%;
  }
  :global(.dark) .lp-underline {
    background-image: linear-gradient(rgba(34, 197, 94, 0.3), rgba(34, 197, 94, 0.3));
  }

  .lp-live-dot {
    width: 7px;
    height: 7px;
    border-radius: 9999px;
    background: #2a9d68;
    box-shadow: 0 0 0 0 rgba(42, 157, 104, 0.55);
    animation: lp-pulse 2.1s ease-out infinite;
  }
  :global(.dark) .lp-live-dot {
    background: #22c55e;
  }
  @keyframes lp-pulse {
    0% { box-shadow: 0 0 0 0 rgba(42, 157, 104, 0.5); }
    70% { box-shadow: 0 0 0 8px rgba(42, 157, 104, 0); }
    100% { box-shadow: 0 0 0 0 rgba(42, 157, 104, 0); }
  }

  .lp-dot {
    color: #b8b0a0;
  }
  :global(.dark) .lp-dot {
    color: #4a4a4a;
  }

  /* ------------------------------------------------------------------ */
  /* Hero background                                                     */
  /* ------------------------------------------------------------------ */

  .lp-root {
    position: relative;
  }

  .lp-hero-bg {
    background: linear-gradient(165deg, #eef3ea 0%, #f8f6f0 52%, #e8f1e8 100%);
  }
  :global(.dark) .lp-hero-bg {
    background: linear-gradient(165deg, #0c1a12 0%, #0a0a0a 52%, #0f1c14 100%);
  }

  .lp-grid {
    background-image:
      linear-gradient(to right, rgba(27, 61, 47, 0.07) 1px, transparent 1px),
      linear-gradient(to bottom, rgba(27, 61, 47, 0.07) 1px, transparent 1px);
    background-size: 46px 46px;
    mask-image: radial-gradient(120% 90% at 50% 0%, #000 0%, transparent 78%);
    -webkit-mask-image: radial-gradient(120% 90% at 50% 0%, #000 0%, transparent 78%);
  }
  :global(.dark) .lp-grid {
    background-image:
      linear-gradient(to right, rgba(34, 197, 94, 0.09) 1px, transparent 1px),
      linear-gradient(to bottom, rgba(34, 197, 94, 0.09) 1px, transparent 1px);
  }

  .lp-blob {
    border-radius: 9999px;
    filter: blur(70px);
    opacity: 0.75;
  }
  .lp-blob-a {
    width: 32rem;
    height: 32rem;
    top: -12rem;
    right: -8rem;
    background: rgba(42, 157, 104, 0.3);
    animation: lp-float 16s ease-in-out infinite alternate;
  }
  .lp-blob-b {
    width: 24rem;
    height: 24rem;
    bottom: -10rem;
    left: -6rem;
    background: rgba(124, 92, 252, 0.16);
    animation: lp-float 20s ease-in-out infinite alternate-reverse;
  }
  .lp-blob-c {
    width: 18rem;
    height: 18rem;
    top: 30%;
    left: 40%;
    background: rgba(37, 99, 235, 0.12);
    animation: lp-float 24s ease-in-out infinite alternate;
  }
  :global(.dark) .lp-blob-a { background: rgba(34, 197, 94, 0.2); }
  :global(.dark) .lp-blob-b { background: rgba(168, 85, 247, 0.12); }
  :global(.dark) .lp-blob-c { background: rgba(59, 130, 246, 0.1); }

  @keyframes lp-float {
    from { transform: translate3d(0, 0, 0) scale(1); }
    to { transform: translate3d(-3rem, 2.5rem, 0) scale(1.12); }
  }

  .lp-wave {
    position: absolute;
    left: 0;
    bottom: -2px;
    width: 200%;
    will-change: transform;
  }
  .lp-wave-slow { height: 60%; animation: lp-wave-drift 30s linear infinite; }
  .lp-wave-mid { height: 44%; opacity: 0.7; animation: lp-wave-drift 20s linear infinite reverse; }
  .lp-wave-fast { height: 30%; opacity: 0.5; animation: lp-wave-drift 13s linear infinite; }

  @keyframes lp-wave-drift {
    from { transform: translateX(0); }
    to { transform: translateX(-50%); }
  }

  .lp-wave-path-1 { fill: rgba(42, 157, 104, 0.16); }
  .lp-wave-path-2 { fill: rgba(27, 61, 47, 0.1); }
  .lp-wave-path-3 { fill: rgba(42, 157, 104, 0.2); }
  :global(.dark) .lp-wave-path-1 { fill: rgba(34, 197, 94, 0.1); }
  :global(.dark) .lp-wave-path-2 { fill: rgba(34, 197, 94, 0.06); }
  :global(.dark) .lp-wave-path-3 { fill: rgba(34, 197, 94, 0.13); }

  /* ------------------------------------------------------------------ */
  /* Hero pipeline mock                                                  */
  /* ------------------------------------------------------------------ */

  .lp-float-in {
    animation: lp-rise 0.9s cubic-bezier(0.22, 1, 0.36, 1) both;
  }
  @keyframes lp-rise {
    from { opacity: 0; transform: translateY(26px) scale(0.98); }
    to { opacity: 1; transform: none; }
  }

  .lp-stage-ring {
    position: absolute;
    inset: -3.5rem -2rem;
    border-radius: 3rem;
    border: 1px dashed rgba(42, 157, 104, 0.28);
    animation: lp-spin 46s linear infinite;
    pointer-events: none;
  }
  :global(.dark) .lp-stage-ring {
    border-color: rgba(34, 197, 94, 0.22);
  }
  @keyframes lp-spin {
    to { transform: rotate(360deg); }
  }

  .lp-step {
    padding: 0.4rem 0.25rem;
    border-radius: 0.6rem;
    border: 1px solid var(--color-ifa-border, #e8e3da);
    background: rgba(247, 245, 240, 0.7);
    color: #8b978f;
    font-size: 10px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    transition: all 0.3s ease;
  }
  :global(.dark) .lp-step {
    border-color: #333333;
    background: rgba(38, 38, 38, 0.5);
    color: #737373;
  }
  .lp-step.is-active {
    border-color: rgba(42, 157, 104, 0.6);
    background: rgba(42, 157, 104, 0.12);
    color: #1b3d2f;
  }
  :global(.dark) .lp-step.is-active {
    border-color: rgba(34, 197, 94, 0.6);
    background: rgba(34, 197, 94, 0.16);
    color: #4ade80;
  }
  .lp-step.is-done {
    color: #2a9d68;
  }

  .lp-stage-body {
    animation: lp-stage-in 0.45s cubic-bezier(0.22, 1, 0.36, 1) both;
  }
  @keyframes lp-stage-in {
    from { opacity: 0; transform: translateY(10px); }
    to { opacity: 1; transform: none; }
  }

  .lp-bubble {
    display: flex;
    gap: 0.5rem;
    padding: 0.6rem 0.75rem;
    border-radius: 0.9rem;
    font-size: 11.5px;
    line-height: 1.5;
    max-width: 92%;
  }
  .lp-bubble-user {
    margin-left: auto;
    background: #1b3d2f;
    color: #ffffff;
  }
  :global(.dark) .lp-bubble-user {
    background: #22c55e;
    color: #06210f;
  }
  .lp-bubble-ai {
    background: rgba(247, 245, 240, 0.95);
    border: 1px solid #e8e3da;
    color: #606c64;
  }
  :global(.dark) .lp-bubble-ai {
    background: rgba(38, 38, 38, 0.85);
    border-color: #333333;
    color: #a3a3a3;
  }

  .lp-typing {
    display: inline-flex;
    gap: 3px;
  }
  .lp-typing i {
    width: 4px;
    height: 4px;
    border-radius: 9999px;
    background: currentColor;
    animation: lp-typing 1.2s ease-in-out infinite;
  }
  .lp-typing i:nth-child(2) { animation-delay: 0.15s; }
  .lp-typing i:nth-child(3) { animation-delay: 0.3s; }
  @keyframes lp-typing {
    0%, 60%, 100% { opacity: 0.25; transform: translateY(0); }
    30% { opacity: 1; transform: translateY(-2px); }
  }

  .lp-row {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.55rem 0.65rem;
    border-radius: 0.8rem;
    border: 1px solid #e8e3da;
    background: rgba(255, 255, 255, 0.75);
  }
  :global(.dark) .lp-row {
    border-color: #333333;
    background: rgba(26, 26, 26, 0.85);
  }

  .lp-row-num {
    width: 1.6rem;
    height: 1.6rem;
    border-radius: 0.5rem;
    background: rgba(42, 157, 104, 0.14);
    color: #1b3d2f;
    font-size: 10px;
    font-weight: 800;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }
  :global(.dark) .lp-row-num {
    background: rgba(34, 197, 94, 0.18);
    color: #4ade80;
  }

  .lp-row-icon {
    width: 1.6rem;
    height: 1.6rem;
    border-radius: 0.5rem;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .lp-pill {
    padding: 0.4rem 0.7rem;
    border-radius: 0.7rem;
    background: #1b3d2f;
    color: #ffffff;
    font-size: 10.5px;
    font-weight: 700;
  }
  :global(.dark) .lp-pill {
    background: #22c55e;
    color: #06210f;
  }

  .lp-badge-ready {
    padding: 0.2rem 0.5rem;
    border-radius: 9999px;
    background: rgba(42, 157, 104, 0.16);
    color: #1b3d2f;
    font-size: 10px;
    font-weight: 700;
  }
  :global(.dark) .lp-badge-ready {
    background: rgba(34, 197, 94, 0.2);
    color: #4ade80;
  }

  .lp-badge-warn {
    padding: 0.2rem 0.5rem;
    border-radius: 9999px;
    background: rgba(217, 119, 6, 0.16);
    color: #9a3412;
    font-size: 10px;
    font-weight: 700;
  }
  :global(.dark) .lp-badge-warn {
    background: rgba(234, 179, 8, 0.2);
    color: #eab308;
  }

  .lp-progress-track {
    height: 6px;
    border-radius: 9999px;
    background: rgba(232, 227, 218, 0.9);
    overflow: hidden;
  }
  :global(.dark) .lp-progress-track {
    background: #333333;
  }
  .lp-progress-fill {
    display: block;
    height: 100%;
    width: 100%;
    border-radius: 9999px;
    background: linear-gradient(90deg, #2a9d68, #1b3d2f);
    transform-origin: left center;
    animation: lp-fill 1.1s cubic-bezier(0.22, 1, 0.36, 1) both;
  }
  :global(.dark) .lp-progress-fill {
    background: linear-gradient(90deg, #22c55e, #16a34a);
  }
  @keyframes lp-fill {
    from { transform: scaleX(0); }
    to { transform: scaleX(1); }
  }

  .lp-chip-float {
    align-items: center;
    gap: 0.4rem;
    padding: 0.45rem 0.75rem;
    border-radius: 9999px;
    border: 1px solid #e8e3da;
    background: rgba(255, 255, 255, 0.92);
    backdrop-filter: blur(8px);
    font-size: 11px;
    font-weight: 700;
    color: #1f2923;
    box-shadow: 0 10px 30px -8px rgba(27, 61, 47, 0.18);
    animation: lp-bob 6s ease-in-out infinite;
  }
  :global(.dark) .lp-chip-float {
    border-color: #333333;
    background: rgba(26, 26, 26, 0.92);
    color: #e5e5e5;
  }
  .lp-chip-delay { animation-delay: 1.6s; }
  .lp-chip-delay-2 { animation-delay: 3.1s; }
  @keyframes lp-bob {
    0%, 100% { transform: translateY(0); }
    50% { transform: translateY(-10px); }
  }

  /* ------------------------------------------------------------------ */
  /* Marquee                                                             */
  /* ------------------------------------------------------------------ */

  .lp-marquee-mask {
    position: relative;
    overflow: hidden;
    mask-image: linear-gradient(to right, transparent, #000 12%, #000 88%, transparent);
    -webkit-mask-image: linear-gradient(to right, transparent, #000 12%, #000 88%, transparent);
  }
  .lp-marquee {
    display: flex;
    align-items: center;
    gap: 3.5rem;
    width: max-content;
    animation: lp-marquee 34s linear infinite;
  }
  .lp-marquee:hover {
    animation-play-state: paused;
  }
  @keyframes lp-marquee {
    from { transform: translateX(0); }
    to { transform: translateX(-50%); }
  }
  .lp-partner {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    white-space: nowrap;
  }

  /* ------------------------------------------------------------------ */
  /* Cards, tones, sections                                              */
  /* ------------------------------------------------------------------ */

  .lp-card {
    transition: transform 0.32s ease, box-shadow 0.32s ease, border-color 0.32s ease;
  }
  .lp-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 18px 40px -14px rgba(27, 61, 47, 0.22);
    border-color: rgba(42, 157, 104, 0.45);
  }

  .lp-feature-icon {
    width: 2.4rem;
    height: 2.4rem;
    border-radius: 0.8rem;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: transform 0.32s ease;
  }
  .lp-card:hover .lp-feature-icon {
    transform: scale(1.08) rotate(-4deg);
  }

  .lp-tone-green { background: rgba(42, 157, 104, 0.14); color: #2a9d68; }
  .lp-tone-blue { background: rgba(37, 99, 235, 0.12); color: #2563eb; }
  .lp-tone-purple { background: rgba(124, 92, 252, 0.14); color: #7c5cfc; }
  .lp-tone-orange { background: rgba(224, 122, 95, 0.16); color: #e07a5f; }
  .lp-tone-amber { background: rgba(217, 119, 6, 0.14); color: #d97706; }
  .lp-tone-pink { background: rgba(225, 29, 72, 0.12); color: #e11d48; }

  :global(.dark) .lp-tone-green { background: rgba(34, 197, 94, 0.16); color: #4ade80; }
  :global(.dark) .lp-tone-blue { background: rgba(59, 130, 246, 0.16); color: #3b82f6; }
  :global(.dark) .lp-tone-purple { background: rgba(168, 85, 247, 0.16); color: #a855f7; }
  :global(.dark) .lp-tone-orange { background: rgba(249, 115, 22, 0.16); color: #f97316; }
  :global(.dark) .lp-tone-amber { background: rgba(234, 179, 8, 0.16); color: #eab308; }
  :global(.dark) .lp-tone-pink { background: rgba(236, 72, 153, 0.16); color: #ec4899; }

  .lp-step-num {
    font-size: 1.5rem;
    font-weight: 800;
    letter-spacing: -0.03em;
    color: rgba(27, 61, 47, 0.18);
  }
  :global(.dark) .lp-step-num {
    color: rgba(34, 197, 94, 0.25);
  }

  .lp-model-chip {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.28rem 0.6rem;
    border-radius: 9999px;
    font-size: 10px;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.06em;
  }

  .lp-connector {
    padding-top: 3rem;
    animation: lp-nudge 2.4s ease-in-out infinite;
  }
  @keyframes lp-nudge {
    0%, 100% { transform: translateX(0); opacity: 0.6; }
    50% { transform: translateX(4px); opacity: 1; }
  }

  .lp-checkpoint {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.5rem 0.85rem;
    border-radius: 9999px;
    border: 1px solid #e8e3da;
    background: rgba(255, 255, 255, 0.7);
    font-size: 11.5px;
    font-weight: 600;
    color: #606c64;
  }
  :global(.dark) .lp-checkpoint {
    border-color: #333333;
    background: rgba(38, 38, 38, 0.6);
    color: #a3a3a3;
  }

  .lp-quote {
    padding: 0.2rem 0.55rem;
    border-radius: 9999px;
    border: 1px dashed rgba(42, 157, 104, 0.45);
    font-size: 10.5px;
    font-weight: 600;
    color: #2a9d68;
  }
  :global(.dark) .lp-quote {
    border-color: rgba(34, 197, 94, 0.45);
    color: #4ade80;
  }

  /* ------------------------------------------------------------------ */
  /* Voice panel                                                         */
  /* ------------------------------------------------------------------ */

  .lp-voice-panel {
    background: linear-gradient(160deg, #eef3ea 0%, #f8f6f0 60%, #e6f0e7 100%);
    box-shadow: 0 24px 60px -30px rgba(27, 61, 47, 0.35);
  }
  :global(.dark) .lp-voice-panel {
    background: linear-gradient(160deg, #0d1a12 0%, #101010 60%, #0f1c14 100%);
  }

  .lp-live-pill {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.35rem 0.7rem;
    border-radius: 9999px;
    border: 1px solid rgba(42, 157, 104, 0.4);
    background: rgba(42, 157, 104, 0.1);
    font-size: 10.5px;
    font-weight: 800;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: #1b3d2f;
  }
  :global(.dark) .lp-live-pill {
    border-color: rgba(34, 197, 94, 0.4);
    background: rgba(34, 197, 94, 0.14);
    color: #4ade80;
  }

  .lp-wavebars {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 4px;
    height: 7rem;
  }
  .lp-bar {
    width: 4px;
    border-radius: 9999px;
    background: linear-gradient(180deg, #2a9d68, rgba(42, 157, 104, 0.35));
    height: var(--h, 40%);
    animation: lp-bars 1.5s ease-in-out infinite;
    animation-delay: var(--d, 0ms);
  }
  :global(.dark) .lp-bar {
    background: linear-gradient(180deg, #4ade80, rgba(34, 197, 94, 0.3));
  }
  @keyframes lp-bars {
    0%, 100% { transform: scaleY(0.55); }
    50% { transform: scaleY(1.15); }
  }

  /* ------------------------------------------------------------------ */
  /* Research card, stats, domains, CTA                                  */
  /* ------------------------------------------------------------------ */

  .lp-research-card {
    position: relative;
  }
  .lp-research-card::before {
    content: '';
    position: absolute;
    inset: -1px;
    border-radius: 1.6rem;
    padding: 1px;
    background: linear-gradient(140deg, rgba(124, 92, 252, 0.5), transparent 45%, rgba(42, 157, 104, 0.5));
    -webkit-mask: linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0);
    -webkit-mask-composite: xor;
    mask: linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0);
    mask-composite: exclude;
    pointer-events: none;
  }

  .lp-stat {
    transition: transform 0.3s ease, border-color 0.3s ease;
  }
  .lp-stat:hover {
    transform: translateY(-3px);
    border-color: rgba(42, 157, 104, 0.5);
  }

  .lp-domain {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.7rem 1.15rem;
    border-radius: 9999px;
    border: 1px solid #e8e3da;
    background: rgba(255, 255, 255, 0.8);
    font-size: 13px;
    font-weight: 600;
    color: #1f2923;
    transition: transform 0.28s ease, border-color 0.28s ease, box-shadow 0.28s ease;
  }
  .lp-domain:hover {
    transform: translateY(-3px);
    border-color: rgba(42, 157, 104, 0.5);
    box-shadow: 0 12px 28px -14px rgba(27, 61, 47, 0.3);
  }
  :global(.dark) .lp-domain {
    border-color: #333333;
    background: rgba(26, 26, 26, 0.85);
    color: #e5e5e5;
  }

  .lp-cta {
    background: #ffffff;
  }
  :global(.dark) .lp-cta {
    background: #121212;
  }
  .lp-cta-bg {
    overflow: hidden;
  }
  .lp-blob-cta {
    width: 34rem;
    height: 34rem;
    top: -18rem;
    left: 50%;
    margin-left: -17rem;
    background: rgba(42, 157, 104, 0.24);
    animation: lp-float 18s ease-in-out infinite alternate;
  }
  :global(.dark) .lp-blob-cta {
    background: rgba(34, 197, 94, 0.16);
  }

  /* ------------------------------------------------------------------ */
  /* Scroll reveal                                                       */
  /* ------------------------------------------------------------------ */

  :global(html.lp-motion-ready) .lp-reveal {
    opacity: 0;
    transform: translateY(18px);
    transition: opacity 0.6s cubic-bezier(0.22, 1, 0.36, 1) var(--lp-delay, 0ms),
      transform 0.6s cubic-bezier(0.22, 1, 0.36, 1) var(--lp-delay, 0ms);
  }
  :global(html.lp-motion-ready) .lp-reveal.is-visible {
    opacity: 1;
    transform: none;
  }

  @media (max-width: 640px) {
    .lp-wavebars { gap: 3px; height: 5rem; }
    .lp-bar { width: 3px; }
    .lp-stage-ring { display: none; }
  }
</style>
