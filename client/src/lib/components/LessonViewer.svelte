<script lang="ts">
  import { Youtube, BookOpen, Clock, GraduationCap, ListChecks, FileText } from 'lucide-svelte';
  import type { GeneratedLesson } from '../types';
  import { renderMarkdown } from '../utils/markdown';

  export let lesson: GeneratedLesson;
  export let moduleTitle = '';
  export let moduleNumber = 1;

  $: renderedContent = renderMarkdown(lesson.contentMarkdown);
  $: hasVideo = Boolean(lesson.youTubeVideoId);
  $: hasCitation = Boolean(lesson.scholarxivPaperTitle);
</script>

<article class="bg-ifa-card rounded-3xl border border-ifa-border shadow-card overflow-hidden">
  <!-- Lesson header -->
  <header class="px-6 md:px-8 pt-6 pb-5 border-b border-ifa-border-light">
    <div class="flex items-center gap-2 text-[11px] font-mono uppercase tracking-wider text-ifa-text-muted mb-2">
      <BookOpen class="w-3.5 h-3.5 text-ifa-pine" />
      <span>Module {moduleNumber} · Lesson {lesson.lessonNumber}</span>
      <span class="text-ifa-border">•</span>
      <span class="flex items-center gap-1"><Clock class="w-3 h-3" /> {lesson.readingTimeMinutes} min</span>
    </div>
    <h1 class="text-2xl font-extrabold tracking-tight text-ifa-text-primary leading-tight">{lesson.title}</h1>
    {#if lesson.summary}
      <p class="text-xs text-ifa-text-secondary font-medium mt-2 max-w-2xl leading-relaxed">{lesson.summary}</p>
    {/if}
    {#if moduleTitle}
      <p class="text-[11px] text-ifa-text-muted mt-1">Part of <span class="font-semibold">{moduleTitle}</span></p>
    {/if}
  </header>

  <!-- Video embed -->
  {#if hasVideo}
    <div class="px-6 md:px-8 pt-6">
      <div class="flex items-center gap-2 mb-2.5">
        <Youtube class="w-4 h-4 text-red-600" />
        <span class="text-xs font-bold text-ifa-text-primary">{lesson.youTubeVideoTitle ?? 'Recommended video'}</span>
      </div>
      <div class="relative w-full rounded-2xl overflow-hidden border border-ifa-border bg-black" style="aspect-ratio: 16 / 9;">
        <iframe
          class="absolute inset-0 w-full h-full"
          src={`https://www.youtube-nocookie.com/embed/${lesson.youTubeVideoId}`}
          title={lesson.youTubeVideoTitle ?? 'Lesson video'}
          frameborder="0"
          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
          referrerpolicy="strict-origin-when-cross-origin"
          allowfullscreen
        ></iframe>
      </div>
    </div>
  {/if}

  <!-- Lesson body -->
  <div class="px-6 md:px-8 py-6 ifa-prose">
    {@html renderedContent}
  </div>

  <!-- Key takeaways -->
  {#if lesson.keyTakeaways && lesson.keyTakeaways.length}
    <div class="mx-6 md:mx-8 mb-6 rounded-2xl bg-ifa-pine/[0.04] border border-ifa-pine/15 p-5">
      <div class="flex items-center gap-2 mb-3">
        <div class="w-6 h-6 rounded-lg bg-ifa-pine/10 flex items-center justify-center text-ifa-pine">
          <ListChecks class="w-3.5 h-3.5" />
        </div>
        <h3 class="text-xs font-bold text-ifa-text-primary uppercase tracking-wide">Key Takeaways</h3>
      </div>
      <ul class="space-y-2">
        {#each lesson.keyTakeaways as takeaway}
          <li class="flex items-start gap-2 text-xs text-ifa-text-secondary leading-relaxed">
            <GraduationCap class="w-3.5 h-3.5 text-ifa-pine mt-0.5 shrink-0" />
            <span>{takeaway}</span>
          </li>
        {/each}
      </ul>
    </div>
  {/if}

  <!-- Scholarly citation -->
  {#if hasCitation}
    <footer class="px-6 md:px-8 py-4 border-t border-ifa-border-light bg-ifa-card-muted/40">
      <div class="flex items-start gap-2.5">
        <FileText class="w-4 h-4 text-ifa-accent-purple mt-0.5 shrink-0" />
        <div>
          <p class="text-[11px] font-semibold text-ifa-text-primary">Grounded in peer-reviewed research</p>
          <p class="text-[11px] text-ifa-text-secondary italic">"{lesson.scholarxivPaperTitle}"</p>
          {#if lesson.scholarxivCitationDoi}
            <a
              href={`https://doi.org/${lesson.scholarxivCitationDoi}`}
              target="_blank"
              rel="noopener noreferrer"
              class="text-[11px] font-mono text-ifa-pine hover:underline"
            >
              DOI: {lesson.scholarxivCitationDoi}
            </a>
          {/if}
        </div>
      </div>
    </footer>
  {/if}
</article>

<style>
  /* Scoped lesson typography. :global is required because the markdown is
     injected with {@html}, so these nodes aren't in the component's static markup. */
  .ifa-prose :global(.ifa-md-h1) {
    font-size: 1.25rem;
    font-weight: 800;
    color: #1f2923;
    letter-spacing: -0.01em;
    margin: 0 0 0.75rem;
  }
  .ifa-prose :global(.ifa-md-h2) {
    font-size: 1.05rem;
    font-weight: 700;
    color: #1b3d2f;
    margin: 1.5rem 0 0.6rem;
  }
  .ifa-prose :global(.ifa-md-h3) {
    font-size: 0.9rem;
    font-weight: 700;
    color: #1f2923;
    margin: 1.25rem 0 0.5rem;
  }
  .ifa-prose :global(.ifa-md-h4) {
    font-size: 0.8rem;
    font-weight: 700;
    color: #606c64;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin: 1rem 0 0.4rem;
  }
  .ifa-prose :global(p) {
    font-size: 0.8rem;
    line-height: 1.7;
    color: #3f4a43;
    margin: 0 0 0.85rem;
  }
  .ifa-prose :global(.ifa-md-list) {
    margin: 0 0 0.85rem;
    padding-left: 1.1rem;
  }
  .ifa-prose :global(.ifa-md-list li) {
    font-size: 0.8rem;
    line-height: 1.65;
    color: #3f4a43;
    margin-bottom: 0.3rem;
  }
  .ifa-prose :global(ul.ifa-md-list) {
    list-style: disc;
  }
  .ifa-prose :global(ol.ifa-md-list) {
    list-style: decimal;
  }
  .ifa-prose :global(strong) {
    font-weight: 700;
    color: #1f2923;
  }
  .ifa-prose :global(.ifa-inline-code) {
    font-family: 'JetBrains Mono', ui-monospace, monospace;
    font-size: 0.72rem;
    background: #f2efe8;
    border: 1px solid #e8e3da;
    border-radius: 0.35rem;
    padding: 0.05rem 0.35rem;
    color: #1b3d2f;
  }
  .ifa-prose :global(.ifa-md-link) {
    color: #1b3d2f;
    font-weight: 600;
    text-decoration: underline;
    text-underline-offset: 2px;
  }
  .ifa-prose :global(.ifa-code) {
    background: #12211a;
    border: 1px solid #1b3d2f;
    border-radius: 0.9rem;
    padding: 1rem 1.15rem;
    overflow-x: auto;
    margin: 0 0 1rem;
  }
  .ifa-prose :global(.ifa-code code) {
    font-family: 'JetBrains Mono', ui-monospace, monospace;
    font-size: 0.74rem;
    line-height: 1.6;
    color: #e6f0ea;
    white-space: pre;
  }
</style>
