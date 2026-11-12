<script lang="ts">
  import { onMount } from 'svelte';
  import { Star, Flag } from 'lucide-svelte';
  import { ApiError, communityApi } from '$lib/api';

  export let courseId: string;
  export let canReview = false;

  let data: any = { rating: 0, reviewCount: 0, reviews: [] };
  let rating = 5;
  let body = '';
  let error = '';
  let unavailable = false;

  onMount(load);

  async function load() {
    try { data = await communityApi.reviews(courseId); }
    catch (cause) {
      if (cause instanceof ApiError && cause.status === 404) unavailable = true;
      else error = cause instanceof Error ? cause.message : 'Reviews could not be loaded.';
    }
  }

  async function save() {
    error = '';
    try {
      await communityApi.saveReview(courseId, rating, body);
      body = '';
      await load();
    } catch (cause) { error = cause instanceof Error ? cause.message : 'Review could not be saved.'; }
  }

  async function report(id: string) {
    const reason = prompt('Report reason: Spam, Harassment, ConflictOfInterest, Irrelevant, or Other');
    if (!reason) return;
    try { await communityApi.report(id, reason); }
    catch (cause) { error = cause instanceof Error ? cause.message : 'Report could not be submitted.'; }
  }
</script>

{#if !unavailable}
  <section class="mt-8 rounded-2xl border border-ifa-border bg-ifa-card p-5 sm:p-7">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div><p class="text-xs font-bold uppercase tracking-wide text-ifa-pine">Verified enrollment reviews</p><h2 class="mt-2 text-2xl font-bold">Learner feedback</h2></div>
      <p class="text-sm text-ifa-text-secondary"><strong class="text-xl text-ifa-text-primary">{data.rating}</strong> / 5 · {data.reviewCount} reviews</p>
    </div>
    {#if canReview}
      <form on:submit|preventDefault={save} class="mt-6 rounded-xl bg-ifa-card-muted p-4">
        <label class="text-sm font-semibold">Rating <select bind:value={rating} class="ml-2 rounded-lg border border-ifa-border bg-ifa-card px-2 py-1">{#each [5,4,3,2,1] as value}<option {value}>{value}</option>{/each}</select></label>
        <textarea bind:value={body} minlength="10" maxlength="5000" required rows="3" placeholder="What helped, and what could improve?" class="mt-3 w-full rounded-xl border border-ifa-border bg-ifa-card px-3 py-2 text-sm"></textarea>
        <button class="mt-2 rounded-lg bg-ifa-pine px-4 py-2 text-sm font-bold text-white">Save review</button>
      </form>
    {/if}
    {#if error}<p role="alert" class="mt-4 text-sm text-red-700">{error}</p>{/if}
    <div class="mt-6 space-y-4">
      {#each data.reviews ?? [] as review}
        <article class="rounded-xl border border-ifa-border p-4">
          <div class="flex items-start justify-between gap-3"><div><p class="font-semibold">{review.learnerName}</p><p class="mt-1 flex items-center gap-1 text-xs text-ifa-text-muted"><Star class="h-3.5 w-3.5 fill-current text-amber-600" />{review.rating} · release review</p></div><button on:click={() => report(review.id)} aria-label="Report review" class="rounded-lg p-2 text-ifa-text-muted hover:bg-ifa-card-muted"><Flag class="h-4 w-4" /></button></div>
          <p class="mt-3 whitespace-pre-wrap text-sm leading-6 text-ifa-text-secondary">{review.body}</p>
          {#each review.replies ?? [] as reply}<div class="mt-3 rounded-lg bg-ifa-card-muted p-3 text-sm"><strong>Instructor response</strong><p class="mt-1 text-ifa-text-secondary">{reply.body}</p></div>{/each}
        </article>
      {/each}
    </div>
  </section>
{/if}
