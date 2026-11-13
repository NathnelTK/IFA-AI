<script lang="ts">
  import type { CourseAppearanceDto } from '$lib/api';
  import { courseAppearanceStyle } from '$lib/courseAppearance';

  export let appearance: CourseAppearanceDto;
  export let title = 'Designing trustworthy learning experiences';
</script>

<div class="preview" data-canvas={appearance.canvasLayout.toLowerCase()} data-buttons={appearance.buttonStyle.toLowerCase()} style={courseAppearanceStyle(appearance)}>
  {#if appearance.canvasLayout === 'Presentation'}
    <header class="presentation-header"><span>Lesson 02 / 08</span><strong>{title}</strong></header>
    <main class="presentation-slide"><p class="eyebrow">Core concept</p><h2>Evidence before conclusions.</h2><p>A clear course design gives learners enough context to question, connect, and apply each idea.</p><div class="visual"></div></main>
    <footer class="presentation-footer"><button>Previous</button><span>2 of 8</span><button>Continue</button></footer>
  {:else}
    <header class="course-header"><div><span>Witforge course</span><h2>{title}</h2></div><button>Continue learning</button></header>
    <div class="course-layout">
      {#if appearance.canvasLayout === 'Sidebar'}
        <aside><strong>Course outline</strong><a class="active" href="#preview">01. Foundations</a><a href="#preview">02. Evidence</a><a href="#preview">03. Application</a></aside>
      {/if}
      <main class="lesson">
        {#if appearance.canvasLayout === 'Classic'}<nav><span>Overview</span><span>Lessons</span><span>Assignments</span></nav>{/if}
        <p class="eyebrow">Module 01</p><h3>Learning begins with a useful question</h3><p>Strong instruction connects clear outcomes with credible material and space for reflection.</p>
        <blockquote>Design is not decoration. It shapes how learners find, understand, and remember information.</blockquote>
        <div class="cards"><article><strong>Learning objective</strong><span>Compare evidence from multiple sources.</span></article><article><strong>Activity</strong><span>Annotate a claim and explain your reasoning.</span></article></div>
        <button>Open lesson</button>
      </main>
    </div>
  {/if}
</div>

<style>
  .preview{overflow:hidden;min-height:34rem;border:var(--course-border-width) solid color-mix(in srgb,var(--course-text) 18%,transparent);border-radius:var(--course-radius);color:var(--course-text);background:var(--course-background);font-family:var(--course-body-font);box-shadow:var(--course-shadow)}
  .course-header{display:flex;align-items:end;justify-content:space-between;gap:1rem;padding:calc(var(--course-space)*1.2);color:white;background:var(--course-primary)}.course-header span,.eyebrow{font-size:.6rem;font-weight:800;letter-spacing:.14em;text-transform:uppercase}.course-header h2{max-width:34rem;margin-top:.5rem;font-family:var(--course-heading-font);font-size:clamp(1.4rem,3vw,2.4rem);line-height:1.05}.preview button{padding:.65rem .9rem;border:1px solid var(--course-primary);border-radius:calc(var(--course-radius)*.65);color:white;background:var(--course-primary);font-size:.65rem;font-weight:800}.course-header button{color:var(--course-primary);background:var(--course-secondary);border-color:transparent}.preview[data-buttons='soft'] button{color:var(--course-primary);background:color-mix(in srgb,var(--course-primary) 12%,var(--course-surface))}.preview[data-buttons='outline'] button{color:var(--course-primary);background:transparent}.course-layout{min-height:26rem}.preview[data-canvas='sidebar'] .course-layout{display:grid;grid-template-columns:12rem minmax(0,1fr)}aside{display:flex;flex-direction:column;gap:.4rem;padding:var(--course-space);border-right:var(--course-border-width) solid color-mix(in srgb,var(--course-text) 14%,transparent);background:var(--course-surface)}aside strong{margin-bottom:.8rem;font-family:var(--course-heading-font);font-size:.75rem}aside a{padding:.65rem;color:var(--course-text);font-size:.62rem;text-decoration:none}aside a.active{color:white;background:var(--course-primary)}.lesson{width:min(100%,var(--course-content-width));margin:0 auto;padding:calc(var(--course-space)*1.5)}.lesson nav{display:flex;gap:1rem;margin-bottom:calc(var(--course-space)*1.5);padding-bottom:.7rem;border-bottom:var(--course-border-width) solid color-mix(in srgb,var(--course-text) 16%,transparent);font-size:.62rem;font-weight:700}.lesson .eyebrow{color:var(--course-primary)}.lesson h3{max-width:42rem;margin:.7rem 0;font-family:var(--course-heading-font);font-size:clamp(1.4rem,3vw,2.2rem);line-height:1.1}.lesson>p:not(.eyebrow){max-width:42rem;font-size:.8rem;line-height:1.8}.lesson blockquote{margin:var(--course-space) 0;padding:var(--course-space);border-left:4px solid var(--course-primary);background:var(--course-surface);font-family:var(--course-heading-font);font-size:.8rem;line-height:1.7}.cards{display:grid;grid-template-columns:1fr 1fr;gap:.7rem;margin:var(--course-space) 0}.cards article{padding:var(--course-space);border:var(--course-border-width) solid color-mix(in srgb,var(--course-text) 16%,transparent);border-radius:var(--course-radius);background:var(--course-surface);box-shadow:var(--course-shadow)}.cards strong,.cards span{display:block}.cards strong{font-family:var(--course-heading-font);font-size:.7rem}.cards span{margin-top:.4rem;font-size:.62rem;line-height:1.5}.preview[data-canvas='editorial'] .course-header{color:var(--course-text);background:var(--course-surface)}.preview[data-canvas='editorial'] .course-header button{color:white;background:var(--course-primary)}.preview[data-canvas='editorial'] .lesson{text-align:left}.preview[data-canvas='editorial'] .lesson h3{font-size:2.7rem}.presentation-header{display:flex;align-items:center;justify-content:space-between;gap:1rem;padding:1rem 1.3rem;color:white;background:var(--course-primary);font-family:var(--course-heading-font);font-size:.65rem}.presentation-slide{display:grid;align-content:center;min-height:27rem;padding:clamp(1.5rem,5vw,4rem);background:var(--course-surface)}.presentation-slide .eyebrow{color:var(--course-primary)}.presentation-slide h2{max-width:42rem;margin:.8rem 0;font-family:var(--course-heading-font);font-size:clamp(2rem,5vw,4.5rem);line-height:.95}.presentation-slide>p:not(.eyebrow){max-width:36rem;font-size:.85rem;line-height:1.7}.visual{height:.45rem;width:8rem;margin-top:2rem;background:var(--course-secondary)}.presentation-footer{display:flex;align-items:center;justify-content:space-between;padding:1rem 1.3rem;background:var(--course-background);font-size:.65rem}
  @media(max-width:640px){.course-header{align-items:start;flex-direction:column}.preview[data-canvas='sidebar'] .course-layout{display:block}.preview[data-canvas='sidebar'] aside{display:none}.cards{grid-template-columns:1fr}.preview[data-canvas='editorial'] .lesson h3{font-size:2rem}}
</style>
