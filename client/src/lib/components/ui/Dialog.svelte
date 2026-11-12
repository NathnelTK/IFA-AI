<script lang="ts">
  import { createEventDispatcher, tick } from 'svelte';
  import { X } from 'lucide-svelte';

  export let open = false;
  export let title: string;
  export let description = '';
  export let closeLabel = 'Close dialog';
  export let dismissible = true;

  const dispatch = createEventDispatcher<{ close: void }>();
  let panel: HTMLDivElement;
  let previousFocus: HTMLElement | null = null;
  let wasOpen = false;

  $: if (open && !wasOpen && typeof document !== 'undefined') {
    previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    document.body.style.overflow = 'hidden';
    void tick().then(() => focusable()[0]?.focus() ?? panel?.focus());
  }
  $: if (!open && wasOpen && typeof document !== 'undefined') {
    document.body.style.overflow = '';
    previousFocus?.focus();
  }
  $: wasOpen = open;

  function close() {
    if (dismissible) dispatch('close');
  }

  function focusable(): HTMLElement[] {
    if (!panel) return [];
    return Array.from(panel.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
    ));
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && dismissible) {
      event.preventDefault();
      close();
      return;
    }
    if (event.key !== 'Tab') return;
    const items = focusable();
    if (items.length === 0) {
      event.preventDefault();
      panel.focus();
      return;
    }
    const first = items[0];
    const last = items[items.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }
</script>

{#if open}
  <div class="fixed inset-0 z-[80] grid place-items-center overflow-y-auto p-4 sm:p-6" role="presentation">
    <button type="button" class="absolute inset-0 bg-black/55" aria-label={closeLabel} on:click={close}></button>
    <div
      bind:this={panel}
      role="dialog"
      aria-modal="true"
      aria-labelledby="dialog-title"
      aria-describedby={description ? 'dialog-description' : undefined}
      tabindex="-1"
      on:keydown={handleKeydown}
      class="relative z-10 max-h-[min(90vh,56rem)] w-full max-w-2xl overflow-y-auto rounded-2xl border border-ifa-border bg-ifa-card shadow-2xl"
    >
      <header class="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-ifa-border bg-ifa-card px-5 py-4 sm:px-6">
        <div>
          <h2 id="dialog-title" class="text-lg font-bold text-ifa-text-primary">{title}</h2>
          {#if description}<p id="dialog-description" class="mt-1 text-sm text-ifa-text-secondary">{description}</p>{/if}
        </div>
        {#if dismissible}
          <button type="button" aria-label={closeLabel} on:click={close} class="grid h-9 w-9 shrink-0 place-items-center rounded-lg text-ifa-text-muted hover:bg-ifa-card-muted hover:text-ifa-text-primary">
            <X class="h-4 w-4" />
          </button>
        {/if}
      </header>
      <div class="p-5 sm:p-6"><slot /></div>
    </div>
  </div>
{/if}
