<script lang="ts">
  import { workspacesApi } from '$lib/api';

  let type: 'Individual' | 'Organization' = 'Individual';
  let displayName = '';
  let title = '';
  let bio = '';
  let evidence = '';
  let submitting = false;
  let message = '';
  let error = '';

  async function submit() {
    submitting = true;
    error = '';
    message = '';
    const evidenceJson = JSON.stringify(evidence.split('\n').map((value) => value.trim()).filter(Boolean));
    try {
      const application = type === 'Individual'
        ? await workspacesApi.createIndividualApplication({ displayName, title, bio, evidenceJson })
        : await workspacesApi.createOrganizationApplication({ displayName, title, bio, evidenceJson });
      await workspacesApi.submitApplication(application.id);
      message = 'Your educator verification application was submitted for review.';
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'The application could not be submitted.';
    } finally {
      submitting = false;
    }
  }
</script>

<svelte:head><title>Apply as an educator · Witforge</title></svelte:head>

<main class="min-h-screen bg-ifa-bg px-4 py-12 sm:px-8">
  <section class="mx-auto max-w-2xl rounded-2xl border border-ifa-border bg-ifa-card p-6 shadow-sm sm:p-8">
    <p class="text-xs font-bold uppercase tracking-[0.18em] text-ifa-pine">Educator verification</p>
    <h1 class="mt-2 text-3xl font-bold text-ifa-text-primary">Create a trusted teaching workspace</h1>
    <p class="mt-3 text-sm leading-relaxed text-ifa-text-secondary">Applications are reviewed before course authoring and public educator capabilities are enabled.</p>

    <form class="mt-8 space-y-5" on:submit|preventDefault={submit}>
      <fieldset>
        <legend class="text-sm font-semibold">Workspace type</legend>
        <div class="mt-2 flex gap-3">
          <label class="flex-1 rounded-xl border border-ifa-border p-3"><input type="radio" bind:group={type} value="Individual" /> <span class="ml-2 font-semibold">Individual</span></label>
          <label class="flex-1 rounded-xl border border-ifa-border p-3"><input type="radio" bind:group={type} value="Organization" /> <span class="ml-2 font-semibold">Organization</span></label>
        </div>
      </fieldset>
      <label class="block text-sm font-semibold" for="educator-name">{type === 'Organization' ? 'Organization name' : 'Public educator name'}</label>
      <input id="educator-name" bind:value={displayName} maxlength="160" required class="w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3" />
      <label class="block text-sm font-semibold" for="educator-title">Professional title</label>
      <input id="educator-title" bind:value={title} maxlength="200" class="w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3" />
      <label class="block text-sm font-semibold" for="educator-bio">Teaching background</label>
      <textarea id="educator-bio" bind:value={bio} maxlength="4000" rows="5" class="w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3"></textarea>
      <label class="block text-sm font-semibold" for="educator-evidence">Evidence links <span class="font-normal text-ifa-text-muted">(one per line)</span></label>
      <textarea id="educator-evidence" bind:value={evidence} rows="4" placeholder="https://…" class="w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3"></textarea>
      {#if error}<p role="alert" class="text-sm text-red-700">{error}</p>{/if}
      {#if message}<p role="status" class="rounded-xl bg-ifa-pine/10 p-3 text-sm text-ifa-pine">{message}</p>{/if}
      <button disabled={submitting || !!message} class="rounded-xl bg-ifa-pine px-5 py-3 text-sm font-bold text-white disabled:opacity-60">{submitting ? 'Submitting…' : 'Submit for review'}</button>
    </form>
  </section>
</main>
