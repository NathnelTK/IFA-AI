<script lang="ts">
  export let data: { profile: any };
  $: profile = data.profile;
</script>

<svelte:head><title>{profile.displayName} · Witforge</title><meta name="description" content={profile.headline || profile.bio} /></svelte:head>

<main class="min-h-screen bg-ifa-bg px-4 py-12 sm:px-6">
  <section class="mx-auto max-w-5xl">
    <div class="rounded-3xl border border-ifa-border bg-ifa-card p-7 sm:p-10">
      <p class="text-xs font-bold uppercase tracking-[0.18em] text-ifa-pine">Verified educator</p>
      <h1 class="mt-3 text-4xl font-bold text-ifa-text-primary">{profile.displayName}</h1>
      {#if profile.headline}<p class="mt-3 text-lg text-ifa-text-secondary">{profile.headline}</p>{/if}
      <p class="mt-6 max-w-3xl whitespace-pre-wrap leading-7 text-ifa-text-secondary">{profile.bio}</p>
    </div>
    <section class="mt-10">
      <h2 class="text-2xl font-bold">Courses</h2>
      <div class="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {#each profile.courses ?? [] as course}
          <a href={`/courses/${course.id}`} class="rounded-2xl border border-ifa-border bg-ifa-card p-5 transition hover:-translate-y-0.5 hover:shadow-md"><p class="text-xs font-bold uppercase tracking-wide text-ifa-pine">{course.category}</p><h3 class="mt-2 text-lg font-bold">{course.title}</h3><p class="mt-2 line-clamp-3 text-sm text-ifa-text-secondary">{course.description}</p></a>
        {/each}
      </div>
    </section>
  </section>
</main>
