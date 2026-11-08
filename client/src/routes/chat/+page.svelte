<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { Loader2 } from 'lucide-svelte';
  import { initSession } from '$lib/stores/sessionStore';

  onMount(async () => {
    const activeSession = await initSession();
    if (!activeSession.authenticated) {
      await goto('/login?redirect=/chat', { replaceState: true });
      return;
    }

    await goto(
      activeSession.role === 'Educator' ? '/studio/create' : '/ai-tutor',
      { replaceState: true }
    );
  });
</script>

<svelte:head>
  <title>Witforge</title>
</svelte:head>

<div class="min-h-screen flex items-center justify-center bg-ifa-bg text-ifa-text-secondary text-sm">
  <Loader2 class="w-5 h-5 mr-2 animate-spin" />
  <span>Opening your workspace…</span>
</div>
