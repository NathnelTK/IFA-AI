<script lang="ts">
  import { goto } from '$app/navigation';
  import { ArrowRight, Play, Check, Star, Code2, Database, BookOpen, Target, Shield, Monitor, Zap } from 'lucide-svelte';

  function handleStartLearning() {
    goto('/home');
  }

  function handleWatchDemo() {
    // In a real implementation, this would open a video or demo
    console.log('Watch demo clicked');
  }
</script>

<div class="min-h-screen bg-ifa-bg text-ifa-text-primary antialiased">
  <!-- Navigation -->
  <nav class="fixed top-0 left-0 right-0 z-50 bg-ifa-bg/80 backdrop-blur-md border-b border-ifa-border">
    <div class="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
      <!-- Logo -->
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-xl bg-ifa-pine flex items-center justify-center text-white">
          <Code2 class="w-5 h-5" />
        </div>
        <span class="text-xl font-bold text-ifa-pine">IFA</span>
      </div>

      <!-- Navigation Links -->
      <div class="hidden md:flex items-center gap-8">
        <a href="#features" class="text-sm font-medium text-ifa-text-secondary hover:text-ifa-pine transition">Features</a>
        <a href="#domains" class="text-sm font-medium text-ifa-text-secondary hover:text-ifa-pine transition">Domains</a>
        <a href="#about" class="text-sm font-medium text-ifa-text-secondary hover:text-ifa-pine transition">About</a>
        <a href="#contact" class="text-sm font-medium text-ifa-text-secondary hover:text-ifa-pine transition">Contact</a>
      </div>

      <!-- CTA Buttons -->
      <div class="flex items-center gap-3">
        <button
          type="button"
          on:click={handleStartLearning}
          class="text-sm font-semibold text-ifa-text-secondary hover:text-ifa-pine transition"
        >
          Sign In
        </button>
        <button
          type="button"
          on:click={handleStartLearning}
          class="px-5 py-2.5 rounded-xl bg-ifa-pine text-white text-sm font-semibold hover:bg-ifa-pine-light transition shadow-sm"
        >
          Get Started
        </button>
      </div>
    </div>
  </nav>

  <!-- Hero Section -->
  <section class="pt-32 pb-20 px-6">
    <div class="max-w-7xl mx-auto">
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
        <!-- Left Content -->
        <div class="space-y-8">
          <div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-ifa-pine/10 text-ifa-pine text-xs font-semibold">
            <Zap class="w-4 h-4" />
            <span>AI-Powered Learning</span>
          </div>

          <h1 class="text-4xl md:text-5xl lg:text-6xl font-extrabold tracking-tight text-ifa-text-primary leading-tight">
            Your Personalized Learning Journey,
            <span class="text-ifa-pine">Powered by AI</span>
          </h1>

          <p class="text-lg text-ifa-text-secondary max-w-xl leading-relaxed">
            IFA adapts to your learning style, pace, and goals. Get personalized course recommendations, 
            AI tutoring, and research-backed content tailored just for you.
          </p>

          <div class="flex flex-wrap items-center gap-4">
            <button
              type="button"
              on:click={handleStartLearning}
              class="px-6 py-3.5 rounded-xl bg-ifa-pine text-white text-sm font-semibold hover:bg-ifa-pine-light transition shadow-sm flex items-center gap-2"
            >
              Start Learning Free
              <ArrowRight class="w-4 h-4" />
            </button>
            <button
              type="button"
              on:click={handleWatchDemo}
              class="px-6 py-3.5 rounded-xl bg-ifa-card border border-ifa-border text-ifa-text-primary text-sm font-semibold hover:bg-ifa-card-muted transition flex items-center gap-2"
            >
              <Play class="w-4 h-4" />
              Watch Demo
            </button>
          </div>

          <!-- Social Proof -->
          <div class="flex items-center gap-6 pt-4">
            <div class="flex items-center gap-2">
              <div class="flex -space-x-2">
                <div class="w-8 h-8 rounded-full bg-ifa-accent-blue border-2 border-ifa-bg"></div>
                <div class="w-8 h-8 rounded-full bg-ifa-accent-purple border-2 border-ifa-bg"></div>
                <div class="w-8 h-8 rounded-full bg-ifa-accent-green border-2 border-ifa-bg"></div>
                <div class="w-8 h-8 rounded-full bg-ifa-accent-orange border-2 border-ifa-bg"></div>
              </div>
              <span class="text-sm text-ifa-text-secondary">Join 10,000+ learners worldwide</span>
            </div>
            <div class="flex items-center gap-1">
              <div class="flex">
                {#each [1, 2, 3, 4, 5] as _}
                  <Star class="w-4 h-4 fill-ifa-accent-amber text-ifa-accent-amber" />
                {/each}
              </div>
              <span class="text-sm font-semibold text-ifa-text-primary">4.8/5</span>
            </div>
          </div>
        </div>

        <!-- Right Content - Illustration -->
        <div class="relative">
          <div class="bg-gradient-to-br from-ifa-pine/20 to-ifa-accent-green/10 rounded-3xl p-8 border border-ifa-pine/30">
            <!-- Student Avatar with PC -->
            <div class="relative z-10">
              <div class="w-full h-64 rounded-2xl bg-ifa-card border border-ifa-border shadow-elevated mb-4 flex flex-col items-center justify-center">
                <!-- Student Icon -->
                <div class="relative mb-3">
                  <div class="w-20 h-20 rounded-full bg-gradient-to-br from-ifa-accent-blue to-ifa-accent-purple flex items-center justify-center">
                    <Monitor class="w-10 h-10 text-white" />
                  </div>
                  <!-- AI Sparkle -->
                  <div class="absolute -top-1 -right-1 w-6 h-6 rounded-full bg-ifa-pine flex items-center justify-center">
                    <Zap class="w-3 h-3 text-white" />
                  </div>
                </div>
                <p class="text-sm font-semibold text-ifa-text-primary">Your AI Learning Companion</p>
                <p class="text-xs text-ifa-text-muted">Study smarter, not harder</p>
              </div>

              <!-- Feature Cards -->
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-ifa-card rounded-xl p-3 border border-ifa-border shadow-soft">
                  <div class="flex items-center gap-2 mb-2">
                    <Database class="w-4 h-4 text-ifa-accent-purple" />
                    <span class="text-xs font-semibold text-ifa-text-primary">Research Powered</span>
                  </div>
                  <p class="text-[10px] text-ifa-text-secondary">Scholarxiv & academic sources</p>
                </div>

                <div class="bg-ifa-card rounded-xl p-3 border border-ifa-border shadow-soft">
                  <div class="flex items-center gap-2 mb-2">
                    <Target class="w-4 h-4 text-ifa-accent-green" />
                    <span class="text-xs font-semibold text-ifa-text-primary">Popular Domains</span>
                  </div>
                  <p class="text-[10px] text-ifa-text-secondary">Engineering, Medicine, CS</p>
                </div>

                <div class="bg-ifa-card rounded-xl p-3 border border-ifa-border shadow-soft">
                  <div class="flex items-center gap-2 mb-2">
                    <Code2 class="w-4 h-4 text-ifa-accent-blue" />
                    <span class="text-xs font-semibold text-ifa-text-primary">Build Real Skills</span>
                  </div>
                  <p class="text-[10px] text-ifa-text-secondary">Projects & certifications</p>
                </div>

                <div class="bg-ifa-card rounded-xl p-3 border border-ifa-border shadow-soft">
                  <div class="flex items-center gap-2 mb-2">
                    <BookOpen class="w-4 h-4 text-ifa-accent-orange" />
                    <span class="text-xs font-semibold text-ifa-text-primary">Track Progress</span>
                  </div>
                  <p class="text-[10px] text-ifa-text-secondary">Real-time analytics</p>
                </div>
              </div>
            </div>

            <!-- Decorative Elements -->
            <div class="absolute -top-4 -right-4 w-20 h-20 bg-ifa-accent-purple/20 rounded-full blur-xl"></div>
            <div class="absolute -bottom-4 -left-4 w-16 h-16 bg-ifa-accent-green/20 rounded-full blur-xl"></div>
          </div>
        </div>
      </div>
    </div>
  </section>

  <!-- Features Section -->
  <section id="features" class="py-20 px-6 bg-ifa-bg-warm">
    <div class="max-w-7xl mx-auto">
      <div class="text-center mb-12">
        <h2 class="text-3xl font-bold text-ifa-text-primary mb-4">Why Choose IFA?</h2>
        <p class="text-ifa-text-secondary max-w-2xl mx-auto">
          Experience the future of learning with our AI-powered platform designed for your success.
        </p>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <!-- Feature 1 -->
        <div class="bg-ifa-card rounded-2xl p-6 border border-ifa-border shadow-card hover:shadow-elevated transition">
          <div class="w-12 h-12 rounded-xl bg-ifa-pine/10 flex items-center justify-center text-ifa-pine mb-4">
            <Zap class="w-6 h-6" />
          </div>
          <h3 class="text-lg font-bold text-ifa-text-primary mb-2">AI-Powered Learning</h3>
          <p class="text-sm text-ifa-text-secondary">
            Personalized course recommendations and adaptive learning paths based on your goals and progress.
          </p>
        </div>

        <!-- Feature 2 -->
        <div class="bg-ifa-card rounded-2xl p-6 border border-ifa-border shadow-card hover:shadow-elevated transition">
          <div class="w-12 h-12 rounded-xl bg-ifa-accent-purple/10 flex items-center justify-center text-ifa-accent-purple mb-4">
            <Database class="w-6 h-6" />
          </div>
          <h3 class="text-lg font-bold text-ifa-text-primary mb-2">Research-Backed</h3>
          <p class="text-sm text-ifa-text-secondary">
            Content curated from Scholarxiv and academic sources for credible, up-to-date information.
          </p>
        </div>

        <!-- Feature 3 -->
        <div class="bg-ifa-card rounded-2xl p-6 border border-ifa-border shadow-card hover:shadow-elevated transition">
          <div class="w-12 h-12 rounded-xl bg-ifa-accent-blue/10 flex items-center justify-center text-ifa-accent-blue mb-4">
            <Target class="w-6 h-6" />
          </div>
          <h3 class="text-lg font-bold text-ifa-text-primary mb-2">Wide Domain Support</h3>
          <p class="text-sm text-ifa-text-secondary">
            Engineering, Medicine & Health, Computer Science, Science, Business and more.
          </p>
        </div>

        <!-- Feature 4 -->
        <div class="bg-ifa-card rounded-2xl p-6 border border-ifa-border shadow-card hover:shadow-elevated transition">
          <div class="w-12 h-12 rounded-xl bg-ifa-accent-green/10 flex items-center justify-center text-ifa-accent-green mb-4">
            <BookOpen class="w-6 h-6" />
          </div>
          <h3 class="text-lg font-bold text-ifa-text-primary mb-2">Track Your Progress</h3>
          <p class="text-sm text-ifa-text-secondary">
            Real-time analytics, skill tracking, and personalized progress reports.
          </p>
        </div>

        <!-- Feature 5 -->
        <div class="bg-ifa-card rounded-2xl p-6 border border-ifa-border shadow-card hover:shadow-elevated transition">
          <div class="w-12 h-12 rounded-xl bg-ifa-accent-orange/10 flex items-center justify-center text-ifa-accent-orange mb-4">
            <Shield class="w-6 h-6" />
          </div>
          <h3 class="text-lg font-bold text-ifa-text-primary mb-2">Trusted & Secure</h3>
          <p class="text-sm text-ifa-text-secondary">
            Your data is protected with enterprise-grade security and privacy controls.
          </p>
        </div>

        <!-- Feature 6 -->
        <div class="bg-ifa-card rounded-2xl p-6 border border-ifa-border shadow-card hover:shadow-elevated transition">
          <div class="w-12 h-12 rounded-xl bg-ifa-accent-pink/10 flex items-center justify-center text-ifa-accent-pink mb-4">
            <Monitor class="w-6 h-6" />
          </div>
          <h3 class="text-lg font-bold text-ifa-text-primary mb-2">Always With You</h3>
          <p class="text-sm text-ifa-text-secondary">
            Access your learning anywhere with our responsive mobile-friendly platform.
          </p>
        </div>
      </div>
    </div>
  </section>

  <!-- Popular Domains Section -->
  <section id="domains" class="py-20 px-6">
    <div class="max-w-7xl mx-auto">
      <div class="text-center mb-12">
        <h2 class="text-3xl font-bold text-ifa-text-primary mb-4">Popular Learning Domains</h2>
        <p class="text-ifa-text-secondary max-w-2xl mx-auto">
          Explore courses across multiple disciplines designed by experts and AI.
        </p>
      </div>

      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
        {#each [
          { name: 'Engineering', icon: Code2, color: 'ifa-accent-blue' },
          { name: 'Medicine & Health', icon: Database, color: 'ifa-accent-purple' },
          { name: 'Computer Science', icon: Target, color: 'ifa-accent-green' },
          { name: 'Science', icon: BookOpen, color: 'ifa-accent-orange' },
          { name: 'Business', icon: Shield, color: 'ifa-accent-pink' }
        ] as domain}
          <div class="bg-ifa-card rounded-xl p-6 border border-ifa-border shadow-soft hover:shadow-card transition text-center">
            <div class={`w-12 h-12 rounded-xl ${domain.color}/10 flex items-center justify-center ${domain.color} mx-auto mb-3`}>
              <svelte:component this={domain.icon} class="w-6 h-6" />
            </div>
            <h3 class="text-sm font-semibold text-ifa-text-primary">{domain.name}</h3>
          </div>
        {/each}
      </div>
    </div>
  </section>

  <!-- CTA Section -->
  <section class="py-20 px-6 bg-gradient-to-r from-ifa-pine to-ifa-pine-dark">
    <div class="max-w-4xl mx-auto text-center">
      <h2 class="text-3xl font-bold text-white mb-4">Ready to Start Your Learning Journey?</h2>
      <p class="text-lg text-emerald-100 mb-8">
        Join thousands of learners who are already transforming their careers with IFA.
      </p>
      <button
        type="button"
        on:click={handleStartLearning}
        class="px-8 py-4 rounded-xl bg-white text-ifa-pine text-base font-semibold hover:bg-emerald-50 transition shadow-lg flex items-center gap-2 mx-auto"
      >
        Start Learning Free
        <ArrowRight class="w-5 h-5" />
      </button>
    </div>
  </section>

  <!-- Footer -->
  <footer id="contact" class="py-12 px-6 bg-ifa-bg border-t border-ifa-border">
    <div class="max-w-7xl mx-auto">
      <div class="text-center">
        <div class="flex items-center justify-center gap-3 mb-4">
          <div class="w-8 h-8 rounded-lg bg-ifa-pine flex items-center justify-center text-white">
            <Code2 class="w-4 h-4" />
          </div>
          <span class="text-lg font-bold text-ifa-pine">IFA</span>
        </div>
        <p class="text-ifa-text-secondary mb-6">Trusted by learners, built for a better tomorrow</p>
        <div class="flex items-center justify-center gap-2 text-sm text-ifa-text-muted">
          <span>STUDY</span>
          <span>•</span>
          <span>GROW</span>
          <span>•</span>
          <span>ACHIEVE</span>
        </div>
      </div>
    </div>
  </footer>
</div>