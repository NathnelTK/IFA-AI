<script lang="ts">
  import { beginCentralSignIn } from '$lib/auth/pkce';
  export let data: { site: any; basePath?: string };
  $: site = data.site;
  $: sections = Array.isArray(site?.page?.schema?.sections) ? site.page.schema.sections : [];

  function href(pageType: string): string {
    const base = data.basePath ?? '';
    return pageType.toLowerCase() === 'home' ? (base || '/') : `${base}/${pageType.toLowerCase()}`;
  }
</script>

<svelte:head>
  <title>{site.page.title} · {site.name}</title>
  {#if site.seo?.description}<meta name="description" content={site.seo.description} />{/if}
</svelte:head>

<div class="min-h-screen bg-white text-slate-900" style={`--site-accent:${site.theme?.accent ?? '#176B4A'}`}>
  <header class="border-b border-slate-200 bg-white/95">
    <div class="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4 py-5 sm:px-6">
      <a href="/" class="text-xl font-bold" style="color:var(--site-accent)">{site.name}</a>
      <nav aria-label="Site navigation" class="flex flex-wrap gap-4 text-sm font-semibold text-slate-600">
        {#each site.navigation as item}
          <a href={href(item.pageType)} class="hover:text-slate-950">{item.title}</a>
        {/each}
        <button type="button" on:click={() => beginCentralSignIn(location.pathname)} class="font-bold" style="color:var(--site-accent)">Sign in</button>
      </nav>
    </div>
  </header>

  <main>
    {#each sections as section}
      {#if section.type === 'hero'}
        <section class="border-b border-slate-200 bg-slate-50 px-4 py-20 text-center sm:px-6">
          <h1 class="mx-auto max-w-4xl text-4xl font-bold tracking-tight sm:text-6xl">{section.heading ?? site.page.title}</h1>
          {#if section.subheading}<p class="mx-auto mt-5 max-w-2xl text-lg text-slate-600">{section.subheading}</p>{/if}
          {#if section.ctaLabel && section.ctaHref}<a href={section.ctaHref} class="mt-8 inline-flex rounded-xl px-5 py-3 font-bold text-white" style="background:var(--site-accent)">{section.ctaLabel}</a>{/if}
        </section>
      {:else if section.type === 'richText'}
        <section class="mx-auto max-w-3xl px-4 py-12 sm:px-6">
          {#if section.heading}<h2 class="text-3xl font-bold">{section.heading}</h2>{/if}
          <p class="mt-4 whitespace-pre-wrap leading-7 text-slate-700">{section.body ?? ''}</p>
        </section>
      {:else if section.type === 'courseGrid'}
        <section class="mx-auto max-w-7xl px-4 py-14 sm:px-6">
          <h2 class="text-3xl font-bold">{section.heading ?? 'Courses'}</h2>
          <div class="mt-7 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {#each site.courses ?? [] as course}
              <article class="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                {#if course.coverImageUrl}<img src={course.coverImageUrl} alt="" class="aspect-[16/9] w-full object-cover" />{/if}
                <div class="p-5"><p class="text-xs font-bold uppercase tracking-wide" style="color:var(--site-accent)">{course.category}</p><h3 class="mt-2 text-lg font-bold">{course.title}</h3><p class="mt-2 line-clamp-3 text-sm text-slate-600">{course.description}</p><a href={`https://witforge.com/courses/${course.id}`} class="mt-5 inline-flex font-bold" style="color:var(--site-accent)">View course</a></div>
              </article>
            {/each}
          </div>
        </section>
      {:else if section.type === 'faq'}
        <section class="mx-auto max-w-3xl px-4 py-14 sm:px-6">
          <h2 class="text-3xl font-bold">{section.heading ?? 'Frequently asked questions'}</h2>
          <div class="mt-6 space-y-3">{#each section.items ?? [] as item}<details class="rounded-xl border border-slate-200 p-4"><summary class="cursor-pointer font-bold">{item.question}</summary><p class="mt-3 text-sm leading-6 text-slate-600">{item.answer}</p></details>{/each}</div>
        </section>
      {:else if section.type === 'contact'}
        <section class="mx-auto max-w-3xl px-4 py-14 text-center sm:px-6"><h2 class="text-3xl font-bold">{section.heading ?? 'Contact'}</h2><p class="mt-4 text-slate-600">{section.body ?? ''}</p>{#if section.email}<a href={`mailto:${section.email}`} class="mt-5 inline-flex font-bold" style="color:var(--site-accent)">{section.email}</a>{/if}</section>
      {/if}
    {/each}
  </main>

  <footer class="border-t border-slate-200 px-4 py-8 text-center text-xs text-slate-500">Courses delivered securely by <a href="https://witforge.com" class="font-bold">Witforge</a></footer>
</div>
