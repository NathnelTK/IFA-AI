<script lang="ts">
  import { onMount } from 'svelte';
  import { UserPlus, Trash2 } from 'lucide-svelte';
  import { workspacesApi } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';
  import StatusBadge from '$lib/components/ui/StatusBadge.svelte';

  let members: any[] = [];
  let pending: any[] = [];
  let email = '';
  let role: 'Admin' | 'Instructor' = 'Instructor';
  let error = '';
  let loading = true;
  let loadedWorkspace = '';
  $: workspaceId = $session.activeWorkspaceId;
  $: if (workspaceId && workspaceId !== loadedWorkspace && !loading) void load();

  onMount(load);

  async function load() {
    if (!workspaceId) return;
    loading = true;
    try {
      const result = await workspacesApi.members(workspaceId);
      loadedWorkspace = workspaceId;
      members = result.members;
      pending = result.pendingInvitations;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Team members could not be loaded.';
    } finally { loading = false; }
  }

  async function invite() {
    if (!workspaceId) return;
    error = '';
    try {
      await workspacesApi.invite(workspaceId, email.trim(), role);
      email = '';
      await load();
    } catch (cause) { error = cause instanceof Error ? cause.message : 'Invitation failed.'; }
  }

  async function remove(id: string) {
    if (!workspaceId || !confirm('Remove this workspace member and their course assignments?')) return;
    await workspacesApi.revokeMember(workspaceId, id);
    await load();
  }
</script>

<svelte:head><title>Studio team · Witforge</title></svelte:head>

<main class="mx-auto max-w-5xl px-4 py-8 sm:px-6">
  <p class="text-xs font-bold uppercase tracking-[0.18em] text-ifa-pine">Studio</p>
  <h1 class="mt-2 text-3xl font-bold">Team</h1>
  <p class="mt-2 text-sm text-ifa-text-secondary">Organization owners and admins can invite instructors without changing account-wide capabilities.</p>

  <form on:submit|preventDefault={invite} class="mt-8 flex flex-col gap-3 rounded-2xl border border-ifa-border bg-ifa-card p-5 sm:flex-row">
    <input type="email" bind:value={email} required placeholder="instructor@example.com" class="min-w-0 flex-1 rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 text-sm" />
    <select bind:value={role} class="rounded-xl border border-ifa-border bg-ifa-card-muted px-4 py-3 text-sm"><option>Instructor</option><option>Admin</option></select>
    <button class="inline-flex items-center justify-center gap-2 rounded-xl bg-ifa-pine px-4 py-3 text-sm font-bold text-white"><UserPlus class="h-4 w-4" />Invite</button>
  </form>
  {#if error}<p role="alert" class="mt-4 text-sm text-red-700">{error}</p>{/if}

  <section class="mt-8 overflow-hidden rounded-2xl border border-ifa-border bg-ifa-card">
    <h2 class="border-b border-ifa-border px-5 py-4 font-bold">Active members</h2>
    {#if !loading}
      {#each members as member}
        <div class="flex flex-wrap items-center justify-between gap-3 border-b border-ifa-border px-5 py-4 last:border-b-0">
          <div><p class="font-semibold">{member.name}</p><p class="text-xs text-ifa-text-muted">{member.email}</p></div>
          <div class="flex items-center gap-3"><StatusBadge label={member.role} tone={member.role === 'Owner' ? 'info' : 'neutral'} />{#if member.role !== 'Owner'}<button on:click={() => remove(member.id)} aria-label={`Remove ${member.name}`} class="rounded-lg p-2 text-red-700 hover:bg-red-50"><Trash2 class="h-4 w-4" /></button>{/if}</div>
        </div>
      {/each}
    {/if}
  </section>

  {#if pending.length > 0}
    <section class="mt-6 rounded-2xl border border-ifa-border bg-ifa-card p-5"><h2 class="font-bold">Pending invitations</h2>{#each pending as invitation}<div class="mt-3 flex justify-between text-sm"><span>{invitation.email}</span><span class="text-ifa-text-muted">{invitation.role}</span></div>{/each}</section>
  {/if}
</main>
