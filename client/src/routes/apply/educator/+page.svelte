<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { apiErrorMessage, workspacesApi } from '$lib/api';
  import { refreshSession, signOut } from '$lib/stores/sessionStore';

  type ApplicationStatus = Awaited<ReturnType<typeof workspacesApi.myApplication>>;

  let type: 'Individual' | 'Organization' = 'Individual';
  let displayName = '';
  let title = '';
  let bio = '';
  let evidence = '';
  let submitting = false;
  let loading = true;
  let message = '';
  let error = '';
  let application: ApplicationStatus = null;

  onMount(loadApplication);

  async function loadApplication() {
    loading = true;
    error = '';
    try {
      application = await workspacesApi.myApplication();
      if (application?.status === 'Approved') {
        await refreshSession();
        await goto('/studio/courses', { replaceState: true });
      }
    } catch (cause) {
      error = apiErrorMessage(cause, 'Your verification status could not be loaded.');
    } finally {
      loading = false;
    }
  }

  async function submit() {
    submitting = true;
    error = '';
    message = '';
    const evidenceJson = JSON.stringify(evidence.split('\n').map((value) => value.trim()).filter(Boolean));
    try {
      const draft = application?.status === 'Draft'
        ? application
        : type === 'Individual'
          ? await workspacesApi.createIndividualApplication({ displayName, title, bio, evidenceJson })
          : await workspacesApi.createOrganizationApplication({ displayName, title, bio, evidenceJson });
      await workspacesApi.submitApplication(draft.id);
      application = await workspacesApi.myApplication();
      message = 'Your educator verification application was submitted for review.';
    } catch (cause) {
      error = apiErrorMessage(cause, 'The application could not be submitted.');
    } finally {
      submitting = false;
    }
  }

  function leave() {
    signOut();
    void goto('/login');
  }
</script>

<svelte:head><title>Apply as an educator · Witforge</title></svelte:head>

<main class="min-h-screen bg-ifa-bg px-4 py-6 sm:px-8">
  <header class="mx-auto flex max-w-2xl items-center justify-between py-4">
    <a href="/" class="text-lg font-black text-ifa-text-primary">Witforge</a>
    <button type="button" on:click={leave} class="text-xs font-bold text-ifa-text-secondary underline">Sign out</button>
  </header>
  <section class="mx-auto mt-8 max-w-2xl rounded-2xl border border-ifa-border bg-ifa-card p-6 shadow-sm sm:p-8">
    <p class="text-xs font-bold uppercase tracking-[0.18em] text-ifa-pine">Educator verification</p>
    <h1 class="mt-2 text-3xl font-bold text-ifa-text-primary">Create a trusted teaching workspace</h1>
    <p class="mt-3 text-sm leading-relaxed text-ifa-text-secondary">Applications are reviewed before course authoring and public educator capabilities are enabled.</p>

    {#if loading}
      <p role="status" class="mt-8 text-sm text-ifa-text-secondary">Loading verification status…</p>
    {:else if application?.status === 'PendingReview'}
      <div class="mt-8 rounded-2xl border border-ifa-border bg-ifa-card-muted p-5">
        <p class="text-sm font-bold text-ifa-text-primary">Application under review</p>
        <p class="mt-2 text-sm leading-relaxed text-ifa-text-secondary">Your learning and teaching tools stay locked while the review is in progress. You will enter Educator Studio directly after approval.</p>
        {#if message}<p role="status" class="mt-3 text-sm font-semibold text-ifa-pine">{message}</p>{/if}
        <button type="button" on:click={loadApplication} class="mt-4 rounded-xl border border-ifa-border px-4 py-2 text-xs font-bold">Refresh status</button>
      </div>
    {:else if application?.status === 'Rejected'}
      <div class="mt-8 rounded-2xl border border-red-200 bg-red-50 p-5 text-red-800">
        <p class="text-sm font-bold">Verification needs attention</p>
        <p class="mt-2 text-sm">{application.decisionReason || 'The application was not approved. Contact support before submitting new evidence.'}</p>
      </div>
    {:else}
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
      {#if message}<p role="status" class="rounded-xl bg-ifa-pine/10 p-3 text-sm text-ifa-pine">{message}</p>{/if}
      <button disabled={submitting || !!message} class="rounded-xl bg-ifa-pine px-5 py-3 text-sm font-bold text-white disabled:opacity-60">{submitting ? 'Submitting…' : 'Submit for review'}</button>
    </form>
    {/if}
    {#if error}<p role="alert" class="mt-5 text-sm text-red-700">{error}</p>{/if}
  </section>
</main>
