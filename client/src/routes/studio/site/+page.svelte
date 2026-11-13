<script lang="ts">
  import { session } from '$lib/stores/sessionStore';
  import { siteManagementApi } from '$lib/api';

  let slug = '';
  let name = '';
  let headline = '';
  let bio = '';
  let domain = '';
  let message = '';
  let error = '';
  $: workspace = $session.workspaces?.find((item) => item.id === $session.activeWorkspaceId) ?? $session.workspaces?.[0];

  async function save() {
    if (!workspace) return;
    error = '';
    try {
      if (workspace.type === 'Organization') {
        await siteManagementApi.saveOrganizationSite(workspace.id, { slug, name, themeJson: '{}', seoJson: '{}' });
        await siteManagementApi.saveOrganizationPage(workspace.id, { pageType: 'Home', isVisible: true, navigationOrder: 0, title: 'Home', schemaJson: JSON.stringify({ sections: [{ type: 'hero', heading: name, subheading: headline }, { type: 'courseGrid', heading: 'Courses' }] }) });
        await siteManagementApi.saveOrganizationPage(workspace.id, { pageType: 'Courses', isVisible: true, navigationOrder: 1, title: 'Courses', schemaJson: JSON.stringify({ sections: [{ type: 'hero', heading: 'Courses' }, { type: 'courseGrid' }] }) });
      } else {
        await siteManagementApi.saveEducatorProfile(workspace.id, { slug, displayName: name, headline, bio, expertiseJson: '[]', linksJson: '[]', themeJson: '{}' });
      }
      message = 'Draft site settings saved.';
    } catch (cause) { error = cause instanceof Error ? cause.message : 'Site settings could not be saved.'; }
  }

  async function publish() {
    if (!workspace) return;
    try {
      if (workspace.type === 'Organization') await siteManagementApi.publishOrganizationSite(workspace.id);
      else await siteManagementApi.publishEducatorProfile(workspace.id);
      message = 'Public presence published.';
    } catch (cause) { error = cause instanceof Error ? cause.message : 'Publishing failed.'; }
  }

  async function addDomain() {
    if (!workspace || !domain.trim()) return;
    try {
      const result: any = await siteManagementApi.addDomain(workspace.id, domain.trim());
      message = `Add TXT ${result.dns.txtName} = ${result.dns.txtValue} and CNAME ${result.dns.cnameName} = ${result.dns.cnameValue}`;
    } catch (cause) { error = cause instanceof Error ? cause.message : 'Domain registration failed.'; }
  }
</script>

<svelte:head><title>Studio site · Witforge</title></svelte:head>

<main class="mx-auto max-w-4xl px-4 py-8 sm:px-6">
  <p class="text-xs font-bold uppercase tracking-[0.18em] text-ifa-pine">Studio</p><h1 class="mt-2 text-3xl font-bold">Public site</h1>
  <p class="mt-2 text-sm text-ifa-text-secondary">Individual profiles and organization microsites use separate, bounded schemas.</p>
  <form on:submit|preventDefault={save} class="mt-8 space-y-5 rounded-2xl border border-ifa-border bg-ifa-card p-6">
    <label class="block text-sm font-semibold">Public slug<input bind:value={slug} required minlength="3" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" /></label>
    <label class="block text-sm font-semibold">Display name<input bind:value={name} required class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" /></label>
    <label class="block text-sm font-semibold">Headline<input bind:value={headline} class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal" /></label>
    {#if workspace?.type !== 'Organization'}<label class="block text-sm font-semibold">Bio<textarea bind:value={bio} required rows="6" class="mt-2 w-full rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 font-normal"></textarea></label>{/if}
    <div class="flex gap-3"><button class="rounded-xl bg-ifa-pine px-4 py-2.5 text-sm font-bold text-white">Save draft</button><button type="button" on:click={publish} class="rounded-xl border border-ifa-pine px-4 py-2.5 text-sm font-bold text-ifa-pine">Publish</button></div>
  </form>
  <section class="mt-6 rounded-2xl border border-ifa-border bg-ifa-card p-6"><h2 class="font-bold">Custom domain</h2><div class="mt-3 flex gap-2"><input bind:value={domain} placeholder="courses.example.org" class="min-w-0 flex-1 rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 text-sm" /><button on:click={addDomain} class="rounded-xl border border-ifa-border px-4 py-2.5 text-sm font-bold">Add</button></div></section>
  {#if message}<p role="status" class="mt-5 whitespace-pre-wrap rounded-xl bg-ifa-pine/10 p-4 text-sm text-ifa-pine">{message}</p>{/if}
  {#if error}<p role="alert" class="mt-5 text-sm text-red-700">{error}</p>{/if}
</main>
