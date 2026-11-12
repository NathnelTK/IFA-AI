<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import {
    ArrowRight,
    BadgeCheck,
    BookOpen,
    Check,
    ChevronRight,
    CirclePlay,
    FileStack,
    Globe2,
    GraduationCap,
    Layers3,
    Menu,
    MousePointer2,
    PenTool,
    ShieldCheck,
    Sparkles,
    Users,
    X
  } from 'lucide-svelte';
  import { parallax, reveal, withGsap } from '$lib/motion/premiumMotion';

  let root: HTMLElement;
  let mobileNavOpen = false;

  const story = [
    {
      number: '01',
      label: 'Shape the source',
      title: 'Start with expertise, not a blank prompt.',
      body: 'Write directly, begin from a bounded template, or attach verified links and clean files. Every path creates the same saved draft, ready for review.',
      metric: 'One draft model',
      detail: 'Manual · template · assisted'
    },
    {
      number: '02',
      label: 'Release with intent',
      title: 'Publish a version learners can trust.',
      body: 'Review the outline, refine every lesson, set access and enrollment rules, then publish an immutable release. Future edits never rewrite someone’s learning history.',
      metric: 'Version-bound',
      detail: 'Content · cohort · offer'
    },
    {
      number: '03',
      label: 'Prove the outcome',
      title: 'Turn activity into credible progress.',
      body: 'Lessons, quizzes and assignments contribute to release-level progress. Completion produces a verifiable record and, when enabled, a downloadable certificate.',
      metric: 'Evidence-led',
      detail: 'Progress · completion · proof'
    }
  ];

  const audiences = [
    {
      id: 'learners',
      eyebrow: 'For learners',
      title: 'A clear path through the work.',
      body: 'Know what to do next, why it matters, and what counts toward completion. Your enrollment remains tied to the exact release you started.',
      items: ['Structured course player', 'Course-aware study assistant', 'Assignments and scored assessments', 'Release-bound certificates'],
      color: 'signal'
    },
    {
      id: 'educators',
      eyebrow: 'For educators',
      title: 'Your expertise, with operational rigor.',
      body: 'Move from an idea or source pack to a reviewable course without losing ownership of the curriculum, audience, objectives or publishing decision.',
      items: ['Courses-first Studio', 'Durable background generation', 'Team roles and instructor assignments', 'Offers, cohorts and analytics'],
      color: 'coral'
    },
    {
      id: 'organizations',
      eyebrow: 'For organizations',
      title: 'A learning presence that feels like yours.',
      body: 'Give instructors a shared workspace, publish a bounded microsite, connect a verified domain and keep enrollment inside the secure Witforge experience.',
      items: ['Organization workspaces', 'Eight purposeful site pages', 'Verified custom domains', 'Public course discovery'],
      color: 'cobalt'
    }
  ];

  const expectations = [
    { icon: ShieldCheck, title: 'Deliberate access', body: 'Workspace roles, scoped sessions and exact-host policies protect authoring and learner data.' },
    { icon: Layers3, title: 'Durable operations', body: 'Long-running work is leased, retried and visible instead of disappearing inside an API request.' },
    { icon: BadgeCheck, title: 'Stable learning history', body: 'Releases, payments, progress and certificates preserve the context in which they were created.' }
  ];

  onMount(() => {
    let cleanup = () => {};
    void withGsap((gsap, ScrollTrigger) => {
      const context = gsap.context(() => {
        const intro = gsap.timeline({ defaults: { ease: 'power3.out' } });
        intro
          .from('[data-hero-nav]', { y: -18, autoAlpha: 0, duration: 0.65 })
          .from('[data-hero-kicker]', { y: 18, autoAlpha: 0, duration: 0.55 }, '-=0.25')
          .from('[data-hero-line]', { yPercent: 115, duration: 0.9, stagger: 0.1 }, '-=0.2')
          .from('[data-hero-copy], [data-hero-actions]', { y: 24, autoAlpha: 0, duration: 0.7, stagger: 0.1 }, '-=0.45')
          .from('[data-hero-stage]', { x: 70, rotate: 2.5, autoAlpha: 0, duration: 1 }, '-=0.85')
          .from('[data-float-card]', { y: 30, autoAlpha: 0, duration: 0.7, stagger: 0.12 }, '-=0.55');

        const media = gsap.matchMedia();
        media.add('(min-width: 1024px)', () => {
          gsap.to('[data-stage-back]', {
            y: -90,
            rotate: -4,
            ease: 'none',
            scrollTrigger: { trigger: '[data-hero]', start: 'top top', end: 'bottom top', scrub: 1.1 }
          });
          gsap.to('[data-stage-front]', {
            y: 45,
            rotate: 2,
            ease: 'none',
            scrollTrigger: { trigger: '[data-hero]', start: 'top top', end: 'bottom top', scrub: 1.1 }
          });

          ScrollTrigger.create({
            trigger: '[data-story]',
            start: 'top 12%',
            end: 'bottom bottom',
            pin: '[data-story-sticky]',
            pinSpacing: false
          });
          gsap.utils.toArray<HTMLElement>('[data-story-card]').forEach((card) => {
            const index = Number(card.dataset.storyCard ?? 0);
            ScrollTrigger.create({
              trigger: card,
              start: 'top 58%',
              end: 'bottom 42%',
              onToggle: ({ isActive }) => {
                if (!isActive) return;
                document.querySelectorAll('[data-story-index]').forEach((item) => {
                  item.toggleAttribute('data-active', item.getAttribute('data-story-index') === String(index));
                });
                gsap.to('[data-story-orbit]', { rotate: index * 120, duration: 0.85, ease: 'power3.inOut' });
              }
            });
          });

          const panels = gsap.utils.toArray<HTMLElement>('[data-audience-panel]');
          if (panels.length > 1) {
            gsap.to(panels, {
              xPercent: -100 * (panels.length - 1),
              ease: 'none',
              scrollTrigger: {
                trigger: '[data-audience-section]',
                start: 'top top',
                end: () => `+=${window.innerWidth * (panels.length - 1)}`,
                pin: true,
                scrub: 1,
                anticipatePin: 1,
                invalidateOnRefresh: true
              }
            });
          }
        });
        cleanup = () => {
          media.revert();
          context.revert();
        };
      }, root);
      return context;
    });
    return () => cleanup();
  });

  function scrollTo(id: string) {
    mobileNavOpen = false;
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }
</script>

<svelte:head>
  <title>Witforge · Build, teach and learn with proof</title>
  <meta name="description" content="Witforge gives learners a clear course path, educators a rigorous authoring Studio, and organizations a trusted public learning presence." />
</svelte:head>

<div bind:this={root} class="landing-root">
  <section data-hero class="hero-shell">
    <div class="hero-grid" aria-hidden="true"></div>
    <nav data-hero-nav class="landing-nav" aria-label="Main navigation">
      <a href="/" class="brand-lockup" aria-label="Witforge home">
        <span class="brand-mark"><span></span><span></span><span></span></span>
        <span>Witforge</span>
      </a>
      <div class="nav-links">
        <button on:click={() => scrollTo('product')}>Product</button>
        <button on:click={() => scrollTo('audiences')}>Who it’s for</button>
        <button on:click={() => scrollTo('trust')}>Trust</button>
        <a href="/marketplace">Browse courses</a>
      </div>
      <div class="nav-actions">
        <a href="/login" class="text-link">Sign in</a>
        <a href="/register" class="nav-cta">Start building <ArrowRight /></a>
      </div>
      <button class="mobile-menu-button" on:click={() => (mobileNavOpen = !mobileNavOpen)} aria-expanded={mobileNavOpen} aria-controls="mobile-nav" aria-label="Toggle navigation">
        {#if mobileNavOpen}<X />{:else}<Menu />{/if}
      </button>
    </nav>

    {#if mobileNavOpen}
      <div id="mobile-nav" class="mobile-nav">
        <button on:click={() => scrollTo('product')}>Product</button>
        <button on:click={() => scrollTo('audiences')}>Who it’s for</button>
        <button on:click={() => scrollTo('trust')}>Trust</button>
        <a href="/marketplace">Browse courses</a>
        <a href="/login">Sign in</a>
        <a href="/register" class="wf-signal">Create an account</a>
      </div>
    {/if}

    <div class="hero-layout">
      <div class="hero-copy">
        <p data-hero-kicker class="wf-kicker">Learning infrastructure for people with something to build</p>
        <h1 class="hero-title wf-display" aria-label="Turn what you know into what people can become">
          <span class="title-mask"><span data-hero-line>Turn what you know</span></span>
          <span class="title-mask"><span data-hero-line>into what people</span></span>
          <span class="title-mask accent-line"><span data-hero-line>can become.</span></span>
        </h1>
        <p data-hero-copy class="hero-intro">
          A serious course platform for learning, teaching and running education at scale. Build reviewable courses, publish stable releases, and give every learner a path they can finish.
        </p>
        <div data-hero-actions class="hero-actions">
          <button on:click={() => goto('/register')} class="primary-action">Create your account <ArrowRight /></button>
          <button on:click={() => scrollTo('product')} class="secondary-action"><CirclePlay /> See how it works</button>
        </div>
        <div class="hero-proof" data-hero-copy>
          <span><Check /> Manual-first authoring</span>
          <span><Check /> Optional reviewed assistance</span>
          <span><Check /> Release-bound progress</span>
        </div>
      </div>

      <div data-hero-stage class="hero-stage" aria-label="A preview of the Witforge course workflow">
        <div data-stage-back class="stage-back">
          <div class="stage-label">Source pack</div>
          <div class="source-row"><FileStack /><span>Field notes.pdf</span><BadgeCheck /></div>
          <div class="source-row"><Globe2 /><span>Verified reference</span><BadgeCheck /></div>
          <div class="source-row"><PenTool /><span>Original lesson draft</span><BadgeCheck /></div>
        </div>
        <div data-stage-front class="stage-front">
          <div class="stage-topline"><span>Course release · 03</span><span class="status-dot">Ready</span></div>
          <div class="course-mini-cover">
            <span class="mini-code">CS / 204</span>
            <h2 class="wf-display">Systems that keep their promises</h2>
            <p>Four modules · twelve lessons · one practical assessment</p>
          </div>
          <div class="module-list">
            {#each ['Model the boundary', 'Design the contract', 'Handle the failure', 'Ship with evidence'] as item, index}
              <div class:complete={index < 2} class="module-row"><span>{String(index + 1).padStart(2, '0')}</span><strong>{item}</strong>{#if index < 2}<Check />{:else}<ChevronRight />{/if}</div>
            {/each}
          </div>
        </div>
        <div data-float-card use:parallax={{ y: -46 }} class="float-card float-progress">
          <span class="ring-progress">68<small>%</small></span>
          <span><strong>Progress with context</strong><small>Bound to release 03</small></span>
        </div>
        <div data-float-card use:parallax={{ y: 38 }} class="float-card float-certificate">
          <GraduationCap /><span><strong>Completion record</strong><small>Verifiable by serial</small></span>
        </div>
      </div>
    </div>

    <div class="hero-ticker" aria-label="Witforge capabilities">
      <div>
        <span>Author with control</span><i></i><span>Publish immutable releases</span><i></i><span>Teach with a team</span><i></i><span>Learn with evidence</span><i></i><span>Build a public presence</span>
      </div>
    </div>
  </section>

  <section id="product" data-story class="story-section">
    <div data-story-sticky class="story-sticky">
      <div class="story-heading" use:reveal>
        <p class="wf-kicker">One connected lifecycle</p>
        <h2 class="wf-display">From lived knowledge to trusted outcome.</h2>
        <p>Witforge keeps authoring, delivery and proof in one product without collapsing them into one screen.</p>
      </div>
      <div class="story-orbit-wrap" aria-hidden="true">
        <div data-story-orbit class="story-orbit">
          <span><PenTool /></span><span><BookOpen /></span><span><BadgeCheck /></span>
        </div>
        <div class="orbit-core">WF</div>
      </div>
      <ol class="story-index" aria-label="Product lifecycle steps">
        {#each story as item, index}
          <li data-story-index={index} data-active={index === 0 ? '' : undefined}><span>{item.number}</span>{item.label}</li>
        {/each}
      </ol>
    </div>
    <div class="story-cards">
      {#each story as item, index}
        <article data-story-card={index} class="story-card" use:reveal={{ y: 54 }}>
          <div class="story-card-meta"><span>{item.number}</span><span>{item.label}</span></div>
          <h3 class="wf-display">{item.title}</h3>
          <p>{item.body}</p>
          <div class="story-card-proof"><strong>{item.metric}</strong><span>{item.detail}</span></div>
        </article>
      {/each}
    </div>
  </section>

  <section id="audiences" data-audience-section class="audience-section">
    <div class="audience-track">
      {#each audiences as audience, index}
        <article data-audience-panel class="audience-panel audience-{audience.color}">
          <div class="audience-number">0{index + 1}</div>
          <div class="audience-copy">
            <p class="wf-kicker">{audience.eyebrow}</p>
            <h2 class="wf-display">{audience.title}</h2>
            <p>{audience.body}</p>
            <ul>
              {#each audience.items as item}<li><Check />{item}</li>{/each}
            </ul>
          </div>
          <div class="audience-art" aria-hidden="true">
            {#if audience.id === 'learners'}
              <div class="learner-path"><span class="path-head">Current release</span>{#each ['Orientation', 'Core practice', 'Assessment', 'Completion'] as item, i}<div class:done={i < 2}><b>{i + 1}</b><span>{item}</span><em>{i < 2 ? 'Done' : i === 2 ? 'Next' : 'Locked'}</em></div>{/each}</div>
            {:else if audience.id === 'educators'}
              <div class="studio-board"><div class="studio-bar"><span></span><span></span><span></span></div><div class="studio-canvas"><aside>{#each ['Overview', 'Curriculum', 'Assignments', 'Release'] as item}<span>{item}</span>{/each}</aside><main><small>Module 03</small><strong>Handle the failure</strong><div class="editor-lines"></div><div class="editor-lines short"></div><button>Review draft</button></main></div></div>
            {:else}
              <div class="site-board"><div class="site-nav"><strong>Northline</strong><span>Courses&nbsp;&nbsp; Instructors&nbsp;&nbsp; About</span></div><div class="site-hero"><small>LEARNING STUDIO</small><strong class="wf-display">Build capability that lasts.</strong><span></span></div><div class="domain-chip"><Globe2 /> learn.northline.org <BadgeCheck /></div></div>
            {/if}
          </div>
        </article>
      {/each}
    </div>
  </section>

  <section id="trust" class="trust-section">
    <div class="trust-intro" use:reveal>
      <p class="wf-kicker">Designed for the long run</p>
      <h2 class="wf-display">Professional does not mean complicated. It means dependable.</h2>
      <p>Every product decision is shaped around clarity for the person learning and control for the person responsible.</p>
    </div>
    <div class="expectation-grid">
      {#each expectations as expectation, index}
        <article use:reveal={{ delay: index * 0.08 }}>
          <span><svelte:component this={expectation.icon} /></span>
          <h3>{expectation.title}</h3>
          <p>{expectation.body}</p>
        </article>
      {/each}
    </div>
    <div class="trust-rail" use:reveal>
      <p>What you will not find here</p>
      <div><span>Surprise publishing</span><span>Fabricated progress</span><span>Mutable learner history</span><span>Hidden payment state</span></div>
    </div>
  </section>

  <section class="course-window-section">
    <div class="course-window-copy" use:reveal>
      <p class="wf-kicker">The learner experience</p>
      <h2 class="wf-display">Less dashboard theatre. More useful next steps.</h2>
      <p>A focused home surfaces the active course, today’s work and the evidence that is moving completion forward.</p>
      <a href="/register">See your learning workspace <ArrowRight /></a>
    </div>
    <div class="course-window" use:reveal={{ y: 60 }}>
      <div class="window-nav"><span class="brand-mark mini"><span></span><span></span><span></span></span><b>My learning</b><span>Search&nbsp;&nbsp; Notifications&nbsp;&nbsp; Profile</span></div>
      <div class="window-body">
        <aside><small>IN PROGRESS</small><strong>Systems that keep their promises</strong><div class="window-progress"><i></i></div><span>68% complete</span></aside>
        <main><small>TODAY’S FOCUS</small><h3 class="wf-display">Handle the failure before it handles you.</h3><p>Lesson 3 of 4 · 14 min</p><button>Continue lesson <ArrowRight /></button></main>
        <div class="evidence-card"><BadgeCheck /><span><strong>2 skills strengthened</strong><small>From quiz and assignment evidence</small></span></div>
      </div>
    </div>
  </section>

  <section class="final-cta">
    <div class="cta-rule"></div>
    <div use:reveal>
      <p class="wf-kicker">Start where you are</p>
      <h2 class="wf-display">Bring the knowledge.<br />Witforge gives it a path.</h2>
      <p>Learn something deeply, teach it deliberately, or build a home for your organization’s courses.</p>
      <div><a href="/register" class="wf-signal">Create an account <ArrowRight /></a><a href="/marketplace">Browse public courses <MousePointer2 /></a></div>
    </div>
  </section>

  <footer class="landing-footer">
    <a href="/" class="brand-lockup"><span class="brand-mark"><span></span><span></span><span></span></span><span>Witforge</span></a>
    <p>Learning, authoring and proof in one dependable system.</p>
    <div><a href="/marketplace">Courses</a><a href="/login">Sign in</a><a href="/register">Create account</a></div>
    <small>© {new Date().getFullYear()} Witforge</small>
  </footer>
</div>

<style>
  :global(html) { scroll-behavior: smooth; }
  .landing-root { overflow: clip; color: var(--wf-text); background: var(--wf-surface-canvas); }
  .hero-shell { position: relative; min-height: 100svh; padding: 0 4vw 3rem; background: #f2efe7; }
  :global(.dark) .hero-shell { background: #101511; }
  .hero-grid { position: absolute; inset: 0; opacity: .34; pointer-events: none; background-image: linear-gradient(rgba(23,34,28,.1) 1px, transparent 1px), linear-gradient(90deg, rgba(23,34,28,.1) 1px, transparent 1px); background-size: clamp(42px,5vw,76px) clamp(42px,5vw,76px); mask-image: linear-gradient(to bottom,#000 0%,transparent 90%); }
  .landing-nav { position: relative; z-index: 20; display: grid; grid-template-columns: 1fr auto 1fr; align-items: center; height: 5.5rem; border-bottom: 1px solid rgba(23,34,28,.18); }
  .brand-lockup { display: inline-flex; align-items: center; gap: .75rem; width: max-content; color: inherit; font-size: 1rem; font-weight: 800; letter-spacing: -.03em; }
  .brand-mark { display: inline-flex; align-items: end; gap: 2px; width: 1.55rem; height: 1.55rem; padding: 5px; border: 1px solid currentColor; border-radius: 50%; }
  .brand-mark span { display: block; width: 3px; background: currentColor; border-radius: 2px; }.brand-mark span:nth-child(1){height:38%}.brand-mark span:nth-child(2){height:80%}.brand-mark span:nth-child(3){height:58%}.brand-mark.mini{width:1.3rem;height:1.3rem;padding:4px}
  .nav-links { display: flex; gap: 1.7rem; font-size: .78rem; font-weight: 700; }.nav-links button,.nav-links a,.text-link{transition:opacity .2s}.nav-links button:hover,.nav-links a:hover,.text-link:hover{opacity:.55}
  .nav-actions { display: flex; justify-content: flex-end; align-items: center; gap: 1.35rem; font-size: .8rem; font-weight: 750; }
  .nav-cta { display: inline-flex; align-items: center; gap: .55rem; padding: .7rem 1rem; border-radius: 999px; color: #f6f3eb; background: #17221c; }.nav-cta :global(svg){width:1rem}.mobile-menu-button,.mobile-nav{display:none}
  .hero-layout { position: relative; z-index: 2; display: grid; grid-template-columns: minmax(0,1.04fr) minmax(31rem,.96fr); gap: clamp(3rem,7vw,8rem); align-items: center; width: min(92rem,100%); min-height: calc(100svh - 9rem); margin: 0 auto; padding: 5.5rem 0 7rem; }
  .hero-copy .wf-kicker { color: #465249; }.hero-title { margin: 2rem 0 1.6rem; font-size: clamp(4rem,7.4vw,8.5rem); font-weight: 420; line-height: .82; letter-spacing: -.065em; }.title-mask{display:block;overflow:hidden;padding-bottom:.08em}.title-mask>span{display:block}.accent-line{color:#176b4a;font-style:italic}
  .hero-intro { max-width: 42rem; color: #4c574f; font-size: clamp(1rem,1.28vw,1.22rem); line-height: 1.75; }
  .hero-actions { display:flex;flex-wrap:wrap;gap:.75rem;margin-top:2.25rem}.primary-action,.secondary-action{display:inline-flex;align-items:center;justify-content:center;gap:.75rem;min-height:3.3rem;padding:0 1.4rem;border-radius:999px;font-size:.82rem;font-weight:800}.primary-action{color:#17221c;background:#c7f464}.secondary-action{border:1px solid rgba(23,34,28,.26);background:rgba(251,250,246,.55)}.primary-action :global(svg),.secondary-action :global(svg){width:1.05rem}
  .hero-proof{display:flex;flex-wrap:wrap;gap:1rem 1.4rem;margin-top:2rem;color:#536057;font-size:.7rem;font-weight:700}.hero-proof span{display:inline-flex;align-items:center;gap:.4rem}.hero-proof :global(svg){width:.85rem;color:#176b4a}
  .hero-stage { position: relative; min-height: 43rem; perspective: 1200px; }.stage-back,.stage-front{position:absolute;border:1px solid rgba(23,34,28,.22);background:#fbfaf6;box-shadow:0 45px 90px -55px rgba(23,34,28,.55)}.stage-back{top:3.5rem;left:0;width:72%;padding:1.4rem;transform:rotate(-5deg);background:#eae5d9}.stage-front{right:0;top:8rem;width:78%;overflow:hidden;transform:rotate(1.8deg)}
  .stage-label,.stage-topline{display:flex;justify-content:space-between;color:#5d685f;font-size:.65rem;font-weight:850;letter-spacing:.12em;text-transform:uppercase}.source-row{display:grid;grid-template-columns:1.2rem 1fr 1rem;gap:.65rem;align-items:center;margin-top:.75rem;padding:.85rem;border-top:1px solid rgba(23,34,28,.15);font-size:.72rem}.source-row :global(svg){width:1rem;color:#176b4a}.stage-topline{padding:1rem 1.2rem;border-bottom:1px solid #cfc8b9}.status-dot{display:inline-flex;align-items:center;gap:.35rem}.status-dot:before{width:.45rem;height:.45rem;border-radius:50%;background:#176b4a;content:''}
  .course-mini-cover{min-height:12.5rem;padding:2.2rem 2rem;color:#f3f0e8;background:#17221c}.mini-code{font-size:.64rem;font-weight:800;letter-spacing:.15em}.course-mini-cover h2{max-width:18rem;margin:.9rem 0;font-size:2.35rem;font-weight:450;line-height:.95}.course-mini-cover p{color:#b9c2bb;font-size:.7rem}.module-list{padding:.7rem 1.2rem 1.2rem}.module-row{display:grid;grid-template-columns:1.5rem 1fr 1rem;gap:.6rem;align-items:center;padding:.8rem 0;border-bottom:1px solid #ded8cb;color:#657068;font-size:.69rem}.module-row strong{color:#17221c;font-size:.75rem}.module-row.complete>span{color:#176b4a}.module-row :global(svg){width:.9rem}.module-row.complete :global(svg){color:#176b4a}
  .float-card{position:absolute;z-index:4;display:flex;align-items:center;gap:.75rem;padding:.75rem 1rem;border:1px solid rgba(23,34,28,.22);background:rgba(251,250,246,.92);box-shadow:0 20px 50px -30px rgba(23,34,28,.7);backdrop-filter:blur(12px)}.float-card strong,.float-card small{display:block}.float-card strong{font-size:.68rem}.float-card small{margin-top:.12rem;color:#657068;font-size:.56rem}.float-progress{left:-7%;bottom:6rem}.float-certificate{right:-5%;top:2.5rem}.float-certificate :global(svg){width:1.4rem;color:#176b4a}.ring-progress{display:grid;place-items:center;width:2.8rem;height:2.8rem;border:4px solid #c7f464;border-left-color:#ded8cb;border-radius:50%;font-size:.72rem;font-weight:850}.ring-progress small{display:inline;font-size:.46rem}
  .hero-ticker{position:absolute;right:0;bottom:0;left:0;overflow:hidden;border-top:1px solid rgba(23,34,28,.18);border-bottom:1px solid rgba(23,34,28,.18);background:rgba(242,239,231,.75)}.hero-ticker>div{display:flex;align-items:center;justify-content:center;gap:1.4rem;min-width:max-content;padding:.75rem 2rem;color:#4c574f;font-size:.65rem;font-weight:800;letter-spacing:.12em;text-transform:uppercase}.hero-ticker i{width:.32rem;height:.32rem;border-radius:50%;background:#ff735d}
  .story-section{display:grid;grid-template-columns:minmax(22rem,.85fr) minmax(0,1.15fr);gap:8vw;padding:10rem 5vw;background:#17221c;color:#f3f0e8}.story-sticky{align-self:start;min-height:75vh}.story-heading h2{max-width:37rem;margin:1.5rem 0;font-size:clamp(3.1rem,5vw,6rem);font-weight:400;line-height:.92;letter-spacing:-.05em}.story-heading>p:last-child{max-width:32rem;color:#aeb8b1;line-height:1.75}.story-heading .wf-kicker{color:#c7f464}
  .story-orbit-wrap{position:relative;display:grid;place-items:center;width:15rem;height:15rem;margin:4rem 0 3rem}.story-orbit{position:absolute;inset:0;border:1px solid #4a574e;border-radius:50%}.story-orbit:before,.story-orbit:after{position:absolute;inset:2rem;border:1px dashed #3c4840;border-radius:50%;content:''}.story-orbit span{position:absolute;display:grid;place-items:center;width:3rem;height:3rem;border-radius:50%;color:#17221c;background:#c7f464}.story-orbit span:nth-child(1){top:-1.5rem;left:6rem}.story-orbit span:nth-child(2){right:-.8rem;bottom:2rem}.story-orbit span:nth-child(3){bottom:1rem;left:-.5rem}.story-orbit :global(svg){width:1.2rem}.orbit-core{display:grid;place-items:center;width:5rem;height:5rem;border-radius:50%;border:1px solid #657168;font-family:'Newsreader Variable';font-size:1.4rem}
  .story-index{display:flex;gap:1.4rem;list-style:none}.story-index li{display:flex;align-items:center;gap:.45rem;color:#7f8c83;font-size:.65rem;font-weight:750;transition:color .35s}.story-index li span{font-variant-numeric:tabular-nums}.story-index li[data-active]{color:#c7f464}
  .story-cards{display:flex;flex-direction:column;gap:24vh;padding-top:8vh;padding-bottom:12vh}.story-card{min-height:60vh;padding:clamp(2rem,5vw,5rem);border:1px solid #465249;background:#202a23}.story-card:nth-child(2){background:#29231f}.story-card:nth-child(3){background:#192529}.story-card-meta{display:flex;justify-content:space-between;color:#c7f464;font-size:.65rem;font-weight:800;letter-spacing:.12em;text-transform:uppercase}.story-card h3{max-width:42rem;margin:clamp(4rem,12vh,8rem) 0 1.5rem;font-size:clamp(3rem,5.4vw,6.4rem);font-weight:400;line-height:.92;letter-spacing:-.05em}.story-card>p{max-width:39rem;color:#b8c0ba;font-size:1rem;line-height:1.8}.story-card-proof{display:flex;justify-content:space-between;gap:1rem;margin-top:4rem;padding-top:1.25rem;border-top:1px solid #465249}.story-card-proof strong{color:#f3f0e8;font-size:.8rem}.story-card-proof span{color:#8d998f;font-size:.7rem}
  .audience-section{height:100svh;overflow:hidden;background:#f2efe7}.audience-track{display:flex;width:300%;height:100%}.audience-panel{position:relative;display:grid;grid-template-columns:.18fr .82fr 1fr;align-items:center;width:100vw;height:100%;padding:7rem 6vw;overflow:hidden}.audience-number{align-self:start;font-family:'Newsreader Variable';font-size:clamp(5rem,11vw,12rem);line-height:.8;opacity:.17}.audience-copy{max-width:39rem}.audience-copy h2{margin:1.5rem 0;font-size:clamp(3.5rem,6vw,7rem);font-weight:420;line-height:.88;letter-spacing:-.055em}.audience-copy>p{max-width:36rem;color:#4c574f;line-height:1.8}.audience-copy ul{display:grid;grid-template-columns:1fr 1fr;gap:.85rem;margin-top:2rem}.audience-copy li{display:flex;align-items:center;gap:.55rem;font-size:.74rem;font-weight:750}.audience-copy li :global(svg){width:.9rem}.audience-art{justify-self:end;width:min(38rem,92%);min-height:31rem;padding:1.5rem;border:1px solid rgba(23,34,28,.22);background:rgba(251,250,246,.62);box-shadow:0 40px 100px -65px rgba(23,34,28,.65)}.audience-signal{background:#c7f464}.audience-coral{background:#ff735d}.audience-cobalt{color:#f7f5ee;background:#3d63dd}.audience-cobalt .audience-copy>p{color:#dbe2ff}.audience-cobalt .audience-art{color:#17221c}
  .learner-path{padding:1rem;background:#fbfaf6}.path-head{display:block;padding:.5rem;color:#657068;font-size:.65rem;font-weight:800;letter-spacing:.12em;text-transform:uppercase}.learner-path>div{display:grid;grid-template-columns:2.3rem 1fr auto;align-items:center;gap:.7rem;padding:1.1rem .7rem;border-top:1px solid #ded8cb}.learner-path b{display:grid;place-items:center;width:1.8rem;height:1.8rem;border:1px solid #cfc8b9;border-radius:50%;font-size:.65rem}.learner-path span{font-size:.8rem;font-weight:750}.learner-path em{color:#657068;font-size:.62rem;font-style:normal}.learner-path div.done b{color:#17221c;border-color:#c7f464;background:#c7f464}
  .studio-board{border:1px solid #cfc8b9;background:#fbfaf6}.studio-bar{display:flex;gap:.3rem;padding:.7rem;border-bottom:1px solid #ded8cb}.studio-bar span{width:.45rem;height:.45rem;border-radius:50%;background:#cfc8b9}.studio-canvas{display:grid;grid-template-columns:8rem 1fr;min-height:27rem}.studio-canvas aside{display:flex;flex-direction:column;gap:.2rem;padding:1rem;border-right:1px solid #ded8cb;background:#eae5d9}.studio-canvas aside span{padding:.6rem;color:#5d685f;font-size:.62rem;font-weight:700}.studio-canvas main{padding:3rem 2.5rem}.studio-canvas small{color:#657068;font-size:.62rem;font-weight:800;letter-spacing:.12em}.studio-canvas strong{display:block;margin:.75rem 0 2rem;font-family:'Newsreader Variable';font-size:2rem}.editor-lines{height:.6rem;margin-top:.7rem;background:#ded8cb}.editor-lines.short{width:64%}.studio-canvas button{margin-top:4rem;padding:.65rem 1rem;border-radius:999px;color:#fff;background:#17221c;font-size:.65rem;font-weight:800}
  .site-board{position:relative;min-height:28rem;border:1px solid #cfc8b9;background:#fbfaf6}.site-nav{display:flex;justify-content:space-between;padding:1rem 1.4rem;border-bottom:1px solid #ded8cb;font-size:.65rem}.site-hero{padding:5rem 2.5rem}.site-hero small{font-size:.6rem;font-weight:800;letter-spacing:.16em}.site-hero strong{display:block;max-width:26rem;margin-top:1rem;font-size:3.6rem;line-height:.9}.site-hero>span{display:block;width:8rem;height:.65rem;margin-top:2rem;background:#3d63dd}.domain-chip{position:absolute;right:1.4rem;bottom:1.4rem;display:flex;align-items:center;gap:.55rem;padding:.7rem 1rem;border:1px solid #cfc8b9;background:#fff;font-size:.64rem;font-weight:750}.domain-chip :global(svg){width:.9rem;color:#176b4a}
  .trust-section{padding:10rem 5vw;background:#eae5d9}.trust-intro{display:grid;grid-template-columns:.5fr 1.3fr 1fr;gap:4vw;align-items:start}.trust-intro h2{font-size:clamp(3.3rem,5.8vw,7rem);font-weight:400;line-height:.9;letter-spacing:-.055em}.trust-intro>p:last-child{color:#4c574f;line-height:1.8}.expectation-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:1px;margin-top:7rem;border:1px solid #cfc8b9;background:#cfc8b9}.expectation-grid article{padding:2.2rem;background:#f2efe7}.expectation-grid article>span{display:grid;place-items:center;width:3rem;height:3rem;border-radius:50%;background:#c7f464}.expectation-grid :global(svg){width:1.2rem}.expectation-grid h3{margin:3rem 0 .8rem;font-size:1rem}.expectation-grid p{color:#5d685f;font-size:.78rem;line-height:1.7}.trust-rail{display:grid;grid-template-columns:.5fr 2.3fr;gap:4vw;margin-top:5rem;padding-top:1.5rem;border-top:1px solid #bdb5a5}.trust-rail p{font-size:.68rem;font-weight:850;text-transform:uppercase}.trust-rail div{display:flex;flex-wrap:wrap;gap:.7rem}.trust-rail span{padding:.55rem .8rem;border:1px solid #bdb5a5;border-radius:999px;font-size:.65rem;font-weight:700}
  .course-window-section{display:grid;grid-template-columns:.58fr 1.42fr;gap:6vw;padding:10rem 5vw;background:#f2efe7}.course-window-copy{align-self:center}.course-window-copy h2{margin:1.5rem 0;font-size:clamp(3.2rem,5vw,6rem);font-weight:400;line-height:.92;letter-spacing:-.05em}.course-window-copy>p{color:#4c574f;line-height:1.8}.course-window-copy>a{display:inline-flex;align-items:center;gap:.65rem;margin-top:2rem;font-size:.78rem;font-weight:850}.course-window-copy>a :global(svg){width:1rem}.course-window{overflow:hidden;border:1px solid #cfc8b9;background:#fbfaf6;box-shadow:0 50px 110px -75px #17221c}.window-nav{display:flex;align-items:center;gap:.7rem;padding:.85rem 1rem;border-bottom:1px solid #ded8cb;font-size:.65rem}.window-nav>span:last-child{margin-left:auto;color:#657068}.window-body{position:relative;display:grid;grid-template-columns:15rem 1fr;min-height:31rem}.window-body>aside{padding:2rem 1.4rem;border-right:1px solid #ded8cb;background:#eae5d9}.window-body small{display:block;color:#657068;font-size:.57rem;font-weight:800;letter-spacing:.12em}.window-body aside strong{display:block;margin:1.2rem 0;font-family:'Newsreader Variable';font-size:1.45rem;line-height:1.05}.window-progress{height:.45rem;margin:2rem 0 .5rem;background:#cfc8b9}.window-progress i{display:block;width:68%;height:100%;background:#176b4a}.window-body aside>span{font-size:.62rem}.window-body>main{padding:4rem 3rem}.window-body main h3{max-width:32rem;margin:1rem 0;font-size:3.2rem;font-weight:430;line-height:.95}.window-body main p{color:#657068;font-size:.7rem}.window-body main button{display:inline-flex;align-items:center;gap:.6rem;margin-top:5rem;padding:.8rem 1rem;border-radius:999px;color:#17221c;background:#c7f464;font-size:.68rem;font-weight:850}.window-body main :global(svg){width:.9rem}.evidence-card{position:absolute;right:1.2rem;bottom:1.2rem;display:flex;align-items:center;gap:.7rem;padding:.75rem 1rem;border:1px solid #cfc8b9;background:#fff}.evidence-card :global(svg){width:1.1rem;color:#176b4a}.evidence-card strong,.evidence-card small{display:block}.evidence-card strong{font-size:.65rem}.evidence-card small{margin-top:.15rem;color:#657068;font-size:.55rem}
  .final-cta{position:relative;display:grid;place-items:center;min-height:88svh;padding:8rem 5vw;color:#f3f0e8;text-align:center;background:#17221c}.cta-rule{position:absolute;inset:2rem;border:1px solid #465249;pointer-events:none}.final-cta>div:last-child{position:relative}.final-cta .wf-kicker{color:#c7f464}.final-cta h2{margin:1.6rem 0;font-size:clamp(4rem,8vw,9rem);font-weight:400;line-height:.84;letter-spacing:-.06em}.final-cta p{max-width:42rem;margin:0 auto;color:#b8c0ba;line-height:1.8}.final-cta>div>div{display:flex;justify-content:center;gap:.8rem;margin-top:2.2rem}.final-cta a{display:inline-flex;align-items:center;gap:.65rem;padding:.85rem 1.2rem;border:1px solid #536057;border-radius:999px;font-size:.72rem;font-weight:850}.final-cta a :global(svg){width:.9rem}.final-cta a.wf-signal{border-color:#c7f464}
  .landing-footer{display:grid;grid-template-columns:1fr 1.4fr 1fr auto;gap:2rem;align-items:center;padding:2.3rem 4vw;border-top:1px solid #cfc8b9;background:#eae5d9}.landing-footer p{color:#5d685f;font-size:.7rem}.landing-footer>div{display:flex;gap:1rem;font-size:.68rem;font-weight:750}.landing-footer small{color:#657068;font-size:.6rem}
  :global(.dark) .hero-shell,:global(.dark) .audience-section,:global(.dark) .course-window-section{background:#101511}:global(.dark) .hero-intro{color:#b8c0ba}
  @media(max-width:1100px){.nav-links{display:none}.landing-nav{grid-template-columns:1fr 1fr}.hero-layout{grid-template-columns:1fr;padding-top:4rem}.hero-copy{max-width:55rem}.hero-stage{width:min(44rem,100%);margin:0 auto}.story-section{grid-template-columns:1fr}.story-sticky{min-height:auto}.story-orbit-wrap{display:none}.story-cards{gap:2rem;padding:3rem 0}.story-card{min-height:auto}.audience-section{height:auto;overflow:visible}.audience-track{display:block;width:100%}.audience-panel{grid-template-columns:.18fr 1fr;height:auto;min-height:48rem}.audience-art{grid-column:2;margin-top:3rem;justify-self:start}.trust-intro{grid-template-columns:1fr}.course-window-section{grid-template-columns:1fr}.landing-footer{grid-template-columns:1fr 1fr}.mobile-menu-button{display:none}}
  @media(max-width:760px){.hero-shell{padding:0 1rem 2rem}.landing-nav{height:4.5rem}.nav-actions,.nav-links{display:none}.mobile-menu-button{display:grid;place-items:center;justify-self:end;width:2.6rem;height:2.6rem;border:1px solid rgba(23,34,28,.22);border-radius:50%}.mobile-menu-button :global(svg){width:1.1rem}.mobile-nav{position:absolute;z-index:30;top:4.6rem;right:1rem;left:1rem;display:flex;flex-direction:column;padding:1rem;border:1px solid #cfc8b9;background:#fbfaf6;box-shadow:0 30px 60px -35px #17221c}.mobile-nav>*{padding:.75rem;text-align:left;font-size:.78rem;font-weight:750}.mobile-nav .wf-signal{margin-top:.4rem;text-align:center}.hero-layout{display:block;min-height:auto;padding:5rem 0 7rem}.hero-title{font-size:clamp(3.6rem,18vw,5.4rem)}.hero-stage{min-height:33rem;margin-top:4rem}.stage-back{display:none}.stage-front{top:1rem;width:94%}.course-mini-cover{min-height:10rem}.course-mini-cover h2{font-size:1.8rem}.float-progress{left:0;bottom:2rem}.float-certificate{right:0;top:-1rem}.hero-proof{display:grid}.hero-ticker>div{justify-content:flex-start}.story-section{padding:7rem 1rem}.story-heading h2{font-size:3.5rem}.story-index{flex-direction:column}.story-card{padding:2rem 1.3rem}.story-card h3{margin-top:5rem;font-size:3.3rem}.audience-panel{display:block;min-height:auto;padding:6rem 1rem}.audience-number{font-size:5rem}.audience-copy h2{font-size:3.6rem}.audience-copy ul{grid-template-columns:1fr}.audience-art{width:100%;min-height:0;margin-top:2.5rem}.studio-canvas{grid-template-columns:5.5rem 1fr}.studio-canvas main{padding:2rem 1rem}.site-hero{padding:3rem 1rem}.site-hero strong{font-size:2.5rem}.trust-section,.course-window-section{padding:7rem 1rem}.trust-intro h2,.course-window-copy h2{font-size:3.6rem}.expectation-grid{grid-template-columns:1fr;margin-top:4rem}.trust-rail{grid-template-columns:1fr}.window-body{display:block}.window-body>aside{border-right:0;border-bottom:1px solid #ded8cb}.window-body>main{padding:2.5rem 1.25rem 7rem}.window-body main h3{font-size:2.6rem}.window-nav>span:last-child{display:none}.final-cta{padding:7rem 1rem}.final-cta h2{font-size:4.2rem}.final-cta>div>div{flex-direction:column}.landing-footer{grid-template-columns:1fr}.landing-footer>div{flex-wrap:wrap}.cta-rule{inset:.7rem}}
  @media(prefers-reduced-motion:reduce){.story-card,.hero-stage,.float-card{transform:none!important}}
</style>
