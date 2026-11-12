<script lang="ts">
  import { goto } from '$app/navigation';
  import {
    ArrowRight,
    BookOpen,
    Check,
    Clock3,
    Compass,
    Flame,
    Lightbulb,
    MoreHorizontal,
    Share2,
    Sparkles,
    Target,
    Trophy
  } from 'lucide-svelte';
  import {
    activeCourse,
    overallProgress,
    skillsList,
    recommendations,
    recentActivities,
    publicCourses,
    userProfile
  } from '$lib/stores/dashboardStore';
  import { courses, courseProgress, moduleInfo, nextLesson } from '$lib/stores/coursesStore';
  import CourseShareDialog from '$lib/components/CourseShareDialog.svelte';
  import { reveal } from '$lib/motion/premiumMotion';

  let shareOpen = false;
  let notice = '';
  $: activeProgress = $activeCourse ? courseProgress($activeCourse) : 0;
  $: activeNext = $activeCourse ? nextLesson($activeCourse) : null;
  $: completedCount = $courses.filter((course) => course.status === 'completed').length;
  $: strongestSkill = [...$skillsList].sort((a, b) => b.percentage - a.percentage)[0] ?? null;
  $: today = new Intl.DateTimeFormat(undefined, { weekday: 'long', month: 'long', day: 'numeric' }).format(new Date());

  function continueLearning() {
    if ($activeCourse) void goto(`/app/courses/${$activeCourse.id}/learn`);
    else void goto('/marketplace');
  }

  async function shareCourse(course: { id: string; title: string }) {
    const url = `${window.location.origin}/courses/${course.id}`;
    try {
      if (navigator.share) await navigator.share({ title: course.title, url });
      else await navigator.clipboard.writeText(url);
      notice = 'Course link ready to share.';
    } catch (cause) {
      if (!(cause instanceof DOMException && cause.name === 'AbortError')) notice = 'The link could not be shared.';
    }
  }
</script>

<svelte:head><title>Your learning · Witforge</title></svelte:head>

<div class="dashboard-shell">
  {#if notice}<div role="status" class="dashboard-toast">{notice}</div>{/if}

  <header class="dashboard-heading" use:reveal>
    <div>
      <p class="wf-kicker">{today}</p>
      <h1 class="wf-display">Make the next hour <em>count.</em></h1>
      <p>Welcome back, {$userProfile.name}. Your workspace is organized around the next useful action, not the loudest metric.</p>
    </div>
    <a href="/marketplace" class="discover-link"><Compass /> Discover a course</a>
  </header>

  <section class="dashboard-grid" aria-label="Learning overview">
    <article class="active-course-card" use:reveal={{ y: 44 }}>
      {#if $activeCourse}
        <div class="active-course-top">
          <span class="wf-kicker">Continue learning</span>
          <button aria-label="Share course" on:click={() => (shareOpen = true)}><Share2 /></button>
        </div>
        <div class="active-course-body">
          <div class="course-code"><span>{$activeCourse.thumbnail || $activeCourse.category.slice(0, 2).toUpperCase()}</span></div>
          <div>
            <p>{$activeCourse.provider}</p>
            <h2 class="wf-display">{$activeCourse.title}</h2>
            <span>{moduleInfo($activeCourse)}{#if activeNext} · {activeNext.lesson.duration}{/if}</span>
          </div>
        </div>
        <div class="active-course-foot">
          <div class="progress-block"><span><strong>{activeProgress}%</strong> release progress</span><div><i style={`width:${activeProgress}%`}></i></div></div>
          <button on:click={continueLearning}>Continue <ArrowRight /></button>
        </div>
      {:else}
        <div class="course-empty">
          <span class="course-code"><BookOpen /></span>
          <p class="wf-kicker">Your first course</p>
          <h2 class="wf-display">Choose a path worth finishing.</h2>
          <p>Browse published courses and enroll when the release, workload and outcome are right for you.</p>
          <button on:click={continueLearning}>Browse courses <ArrowRight /></button>
        </div>
      {/if}
    </article>

    <article class="metric-card signal" use:reveal={{ delay: .08 }}>
      <span class="metric-icon"><Target /></span>
      <p>Across active courses</p>
      <strong class="wf-display">{$overallProgress}<small>%</small></strong>
      <span>Overall progress</span>
    </article>

    <article class="metric-card paper" use:reveal={{ delay: .14 }}>
      <span class="metric-icon"><Trophy /></span>
      <p>Completed</p>
      <strong class="wf-display">{completedCount}</strong>
      <span>{completedCount === 1 ? 'course' : 'courses'}</span>
    </article>

    <article class="next-card" use:reveal={{ delay: .12 }}>
      <div class="section-top"><span class="wf-kicker">Next useful actions</span><a href="/recommendations">View all</a></div>
      {#if $recommendations.length}
        <div class="action-list">
          {#each $recommendations.slice(0, 3) as item, index}
            <button on:click={() => goto(item.actionUrl)}>
              <span class="action-number">0{index + 1}</span>
              <span><strong>{item.title}</strong><small>{item.subtitle}</small></span>
              <ArrowRight />
            </button>
          {/each}
        </div>
      {:else}
        <div class="calm-empty"><Lightbulb /><span><strong>No recommendation noise.</strong><small>Actions will appear when your real course evidence supports them.</small></span></div>
      {/if}
    </article>

    <article class="skills-card" use:reveal={{ delay: .18 }}>
      <div class="section-top"><span class="wf-kicker">Skill evidence</span><a href="/my-skills">Details</a></div>
      {#if $skillsList.length}
        <div class="skill-focus">
          <div class="skill-ring" style={`--value:${strongestSkill?.percentage ?? 0}`}><span>{strongestSkill?.percentage ?? 0}<small>%</small></span></div>
          <div><small>Strongest signal</small><strong>{strongestSkill?.name}</strong><p>Based on completed assessments and learning activity.</p></div>
        </div>
        <div class="skill-bars">
          {#each $skillsList.slice(0, 4) as skill}<div><span>{skill.name}</span><i><b style={`width:${skill.percentage}%`}></b></i><em>{skill.percentage}%</em></div>{/each}
        </div>
      {:else}
        <div class="calm-empty"><Sparkles /><span><strong>Skills begin with evidence.</strong><small>Complete an assessment to create your first signal.</small></span></div>
      {/if}
    </article>

    <article class="activity-card" use:reveal={{ delay: .2 }}>
      <div class="section-top"><span class="wf-kicker">Recent activity</span><button aria-label="Activity options"><MoreHorizontal /></button></div>
      {#if $recentActivities.length}
        <ol class="activity-list">
          {#each $recentActivities.slice(0, 5) as activity}
            <li><i></i><span><strong>{activity.title}</strong><small>{activity.detail}</small></span><time>{activity.timeAgo}</time></li>
          {/each}
        </ol>
      {:else}
        <div class="calm-empty"><Clock3 /><span><strong>Your timeline is clear.</strong><small>Completed lessons, quizzes and research activity will appear here.</small></span></div>
      {/if}
    </article>
  </section>

  <section class="discovery-section" use:reveal>
    <div class="discovery-heading"><div><p class="wf-kicker">Public course releases</p><h2 class="wf-display">Explore what others are teaching.</h2></div><a href="/marketplace">Open marketplace <ArrowRight /></a></div>
    {#if $publicCourses.length}
      <div class="course-row">
        {#each $publicCourses.slice(0, 4) as course, index}
          <article>
            <a href={`/courses/${course.id}`} class="course-art art-{index % 4}"><span>{course.title.slice(0, 2).toUpperCase()}</span><small>{course.duration}</small></a>
            <div><p>{course.provider}</p><h3>{course.title}</h3><footer><span>{course.rating ? `${course.rating} rating` : 'New release'}</span><button on:click={() => shareCourse(course)} aria-label={`Share ${course.title}`}><Share2 /></button></footer></div>
          </article>
        {/each}
      </div>
    {:else}
      <div class="discovery-empty">Published courses will appear here as the marketplace grows.</div>
    {/if}
  </section>

  {#if shareOpen}
    <CourseShareDialog isOpen={shareOpen} on:close={() => (shareOpen = false)} courseId={$activeCourse?.id || ''} courseTitle={$activeCourse?.title || 'Course'} />
  {/if}
</div>

<style>
  .dashboard-shell{width:min(100%,94rem);margin:0 auto;padding:clamp(1.5rem,3vw,3rem)}
  .dashboard-toast{position:fixed;z-index:60;top:5.3rem;right:1.5rem;padding:.75rem 1rem;border:1px solid #465249;color:#f3f0e8;background:#17221c;font-size:.68rem;font-weight:750;box-shadow:0 20px 50px -30px #17221c}
  .dashboard-heading{display:flex;align-items:end;justify-content:space-between;gap:3rem;padding:1.2rem 0 2.7rem;border-bottom:1px solid var(--wf-border)}.dashboard-heading .wf-kicker{color:var(--wf-text-muted)}.dashboard-heading h1{max-width:55rem;margin:.8rem 0 .6rem;font-size:clamp(3.2rem,5.5vw,6.5rem);font-weight:430;line-height:.88;letter-spacing:-.055em}.dashboard-heading h1 em{color:#176b4a;font-weight:430}.dashboard-heading p:last-child{max-width:47rem;color:var(--wf-text-muted);font-size:.78rem;line-height:1.7}.discover-link{display:inline-flex;align-items:center;gap:.55rem;flex-shrink:0;padding:.75rem 1rem;border:1px solid var(--wf-border);border-radius:999px;background:var(--wf-surface-raised);font-size:.68rem;font-weight:800}.discover-link :global(svg){width:1rem}
  .dashboard-grid{display:grid;grid-template-columns:1.15fr .85fr .52fr .52fr;gap:1rem;margin-top:1rem}.dashboard-grid>article{border:1px solid var(--wf-border);background:var(--wf-surface-raised)}
  .active-course-card{grid-column:span 2;grid-row:span 2;min-height:29rem;padding:1.6rem;color:#f3f0e8;background:#17221c!important}.active-course-top,.section-top{display:flex;align-items:center;justify-content:space-between}.active-course-top .wf-kicker{color:#c7f464}.active-course-top button{display:grid;width:2.3rem;height:2.3rem;place-items:center;border:1px solid #465249;border-radius:50%;color:#b8c0ba}.active-course-top :global(svg){width:1rem}.active-course-body{display:grid;grid-template-columns:9rem 1fr;gap:1.5rem;align-items:center;margin-top:4.5rem}.course-code{display:grid;place-items:center;aspect-ratio:1;border:1px solid #536057;background:#202a23;font-family:'Newsreader Variable';font-size:2.2rem;font-style:italic}.course-code :global(svg){width:2.2rem}.active-course-body p{color:#91a096;font-size:.61rem;font-weight:800;letter-spacing:.1em;text-transform:uppercase}.active-course-body h2{max-width:30rem;margin:.6rem 0;font-size:clamp(2.4rem,3.8vw,4.5rem);font-weight:430;line-height:.9}.active-course-body>div>span{color:#aeb8b1;font-size:.66rem}.active-course-foot{display:grid;grid-template-columns:1fr auto;gap:2rem;align-items:end;margin-top:4rem}.progress-block>span{display:flex;justify-content:space-between;color:#aeb8b1;font-size:.62rem}.progress-block strong{color:#f3f0e8}.progress-block>div{height:.35rem;margin-top:.65rem;background:#354139}.progress-block i{display:block;height:100%;background:#c7f464}.active-course-foot button,.course-empty button{display:inline-flex;align-items:center;gap:.55rem;padding:.75rem 1rem;border-radius:999px;color:#17221c;background:#c7f464;font-size:.65rem;font-weight:850}.active-course-foot :global(svg),.course-empty :global(svg){width:.9rem}.course-empty{display:flex;min-height:25rem;flex-direction:column;align-items:flex-start;justify-content:center}.course-empty .course-code{width:5rem}.course-empty .wf-kicker{margin-top:2rem;color:#c7f464}.course-empty h2{max-width:31rem;margin:1rem 0;font-size:3.5rem;font-weight:430;line-height:.9}.course-empty>p:not(.wf-kicker){max-width:34rem;color:#aeb8b1;font-size:.75rem;line-height:1.7}.course-empty button{margin-top:2rem}
  .metric-card{display:flex;min-height:14rem;flex-direction:column;padding:1.35rem}.metric-card.signal{color:#17221c;background:#c7f464}.metric-card.paper{background:#eae5d9}.metric-icon{display:grid;width:2.4rem;height:2.4rem;place-items:center;border:1px solid rgba(23,34,28,.25);border-radius:50%}.metric-icon :global(svg){width:1rem}.metric-card p{margin-top:2rem;font-size:.58rem;font-weight:800;letter-spacing:.12em;text-transform:uppercase}.metric-card strong{margin-top:auto;font-size:4.2rem;font-weight:440;line-height:.8}.metric-card strong small{font-family:'Manrope Variable';font-size:1rem}.metric-card>span:last-child{margin-top:.5rem;font-size:.62rem;font-weight:700}
  .next-card{grid-column:3/-1;min-height:18rem;padding:1.35rem}.section-top .wf-kicker{color:var(--wf-text-muted)}.section-top>a,.section-top>button{color:var(--wf-text-muted);font-size:.6rem;font-weight:800}.action-list{margin-top:1.2rem}.action-list button{display:grid;width:100%;grid-template-columns:2rem 1fr 1rem;align-items:center;gap:.7rem;padding:.85rem 0;border-top:1px solid var(--wf-border);text-align:left}.action-number{color:#176b4a;font-family:'Newsreader Variable';font-size:1rem}.action-list button>span:nth-child(2){display:flex;min-width:0;flex-direction:column}.action-list strong{overflow:hidden;font-size:.69rem;text-overflow:ellipsis;white-space:nowrap}.action-list small{margin-top:.2rem;overflow:hidden;color:var(--wf-text-muted);font-size:.57rem;text-overflow:ellipsis;white-space:nowrap}.action-list :global(svg){width:.85rem}
  .skills-card{grid-column:span 2;padding:1.35rem}.skill-focus{display:grid;grid-template-columns:6.3rem 1fr;gap:1.2rem;align-items:center;margin-top:1.8rem}.skill-ring{display:grid;place-items:center;aspect-ratio:1;border-radius:50%;background:conic-gradient(#176b4a calc(var(--value)*1%),#ded8cb 0);position:relative}.skill-ring:after{position:absolute;inset:.55rem;border-radius:50%;background:var(--wf-surface-raised);content:''}.skill-ring span{z-index:1;font-family:'Newsreader Variable';font-size:1.8rem}.skill-ring small{font-family:'Manrope Variable';font-size:.6rem}.skill-focus>div:last-child{display:flex;flex-direction:column}.skill-focus small{color:var(--wf-text-muted);font-size:.55rem;font-weight:800;text-transform:uppercase}.skill-focus strong{margin:.25rem 0;font-size:1rem}.skill-focus p{color:var(--wf-text-muted);font-size:.62rem}.skill-bars{display:grid;grid-template-columns:1fr 1fr;gap:.7rem 1.2rem;margin-top:1.7rem}.skill-bars>div{display:grid;grid-template-columns:1fr 4rem 2rem;align-items:center;gap:.6rem;font-size:.58rem}.skill-bars i{height:.25rem;background:var(--wf-surface-muted)}.skill-bars b{display:block;height:100%;background:#176b4a}.skill-bars em{color:var(--wf-text-muted);font-style:normal;text-align:right}
  .activity-card{grid-column:span 2;padding:1.35rem}.activity-list{margin-top:1.2rem}.activity-list li{display:grid;grid-template-columns:.5rem 1fr auto;align-items:start;gap:.7rem;padding:.72rem 0;border-top:1px solid var(--wf-border)}.activity-list li>i{width:.4rem;height:.4rem;margin-top:.3rem;border-radius:50%;background:#ff735d}.activity-list li>span{display:flex;min-width:0;flex-direction:column}.activity-list strong{font-size:.65rem}.activity-list small{margin-top:.18rem;overflow:hidden;color:var(--wf-text-muted);font-size:.56rem;text-overflow:ellipsis;white-space:nowrap}.activity-list time{color:var(--wf-text-muted);font-size:.54rem}.calm-empty{display:flex;align-items:center;gap:.8rem;min-height:8rem;margin-top:1.2rem;padding:1rem;border-top:1px solid var(--wf-border);color:var(--wf-text-muted)}.calm-empty :global(svg){width:1.2rem;flex-shrink:0}.calm-empty span{display:flex;flex-direction:column}.calm-empty strong{color:var(--wf-text);font-size:.68rem}.calm-empty small{margin-top:.25rem;font-size:.58rem;line-height:1.5}
  .discovery-section{margin-top:5rem;padding-top:3rem;border-top:1px solid var(--wf-border)}.discovery-heading{display:flex;align-items:end;justify-content:space-between;gap:2rem}.discovery-heading h2{max-width:45rem;margin-top:1rem;font-size:clamp(2.7rem,4.2vw,5rem);font-weight:430;line-height:.9}.discovery-heading>a{display:inline-flex;align-items:center;gap:.5rem;font-size:.68rem;font-weight:850}.discovery-heading :global(svg){width:.9rem}.course-row{display:grid;grid-template-columns:repeat(4,1fr);gap:1rem;margin-top:2rem}.course-row article{border:1px solid var(--wf-border);background:var(--wf-surface-raised)}.course-art{display:flex;aspect-ratio:1.25;flex-direction:column;justify-content:space-between;padding:1rem;color:#17221c}.course-art>span{font-family:'Newsreader Variable';font-size:2.6rem;font-style:italic}.course-art small{font-size:.55rem;font-weight:800;text-transform:uppercase}.art-0{background:#c7f464}.art-1{background:#ff735d}.art-2{color:#f3f0e8;background:#3d63dd}.art-3{background:#eae5d9}.course-row article>div{padding:1rem}.course-row p{color:var(--wf-text-muted);font-size:.55rem;font-weight:750}.course-row h3{min-height:2.8rem;margin-top:.45rem;font-size:.76rem;line-height:1.4}.course-row footer{display:flex;align-items:center;justify-content:space-between;margin-top:1rem;padding-top:.75rem;border-top:1px solid var(--wf-border);color:var(--wf-text-muted);font-size:.54rem}.course-row footer button :global(svg){width:.85rem}.discovery-empty{margin-top:2rem;padding:3rem;border:1px dashed var(--wf-border);color:var(--wf-text-muted);font-size:.72rem;text-align:center}
  @media(max-width:1180px){.dashboard-grid{grid-template-columns:1fr 1fr}.active-course-card{grid-column:1/-1}.next-card{grid-column:1/-1}.skills-card,.activity-card{grid-column:span 1}.course-row{grid-template-columns:1fr 1fr}}
  @media(max-width:760px){.dashboard-shell{padding:1rem}.dashboard-heading{display:block;padding-top:1rem}.dashboard-heading h1{font-size:3.7rem}.discover-link{margin-top:1.5rem}.dashboard-grid{display:block}.dashboard-grid>article{margin-top:.8rem}.active-course-card{min-height:0;padding:1.2rem}.active-course-body{grid-template-columns:5rem 1fr;margin-top:3rem}.active-course-body h2{font-size:2.5rem}.active-course-foot{display:block;margin-top:2.5rem}.active-course-foot button{margin-top:1.2rem}.metric-card{min-height:12rem}.skill-focus{grid-template-columns:5.3rem 1fr}.skill-bars{grid-template-columns:1fr}.discovery-heading{align-items:start;flex-direction:column}.course-row{grid-template-columns:1fr}.dashboard-toast{right:1rem;left:1rem}}
</style>
