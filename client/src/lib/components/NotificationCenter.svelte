<script lang="ts">
  import { createEventDispatcher, tick } from 'svelte';
  import { goto } from '$app/navigation';
  import { Bell, X, BookOpen, Award, Zap, Clock } from 'lucide-svelte';
  import { notificationsApi, type NotificationDto } from '$lib/api';
  import { session } from '$lib/stores/sessionStore';

  export let isOpen = false;

  const dispatch = createEventDispatcher();

  let notifications: NotificationDto[] = [];
  let unreadCount = 0;
  let loaded = false;
  let loading = false;
  let error = '';
  let panel: HTMLElement | null = null;
  let returnFocus: HTMLElement | null = null;
  let wasOpen = false;
  let loadedFor: string | null = null;

  $: unreadCount = notifications.filter((notification) => !notification.isRead).length;
  $: if ($session.learnerId !== loadedFor) {
    loadedFor = $session.learnerId;
    notifications = [];
    loaded = false;
    error = '';
  }
  $: dispatch('change', { unreadCount });
  $: if (isOpen && !loaded && !loading) void loadNotifications();
  $: if (isOpen && !wasOpen) {
    wasOpen = true;
    returnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    void tick().then(() => panel?.focus());
  } else if (!isOpen && wasOpen) {
    wasOpen = false;
    returnFocus?.focus();
  }

  async function loadNotifications() {
    loading = true;
    error = '';
    try {
      notifications = await notificationsApi.list();
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load notifications.';
    } finally {
      loaded = true;
      loading = false;
    }
  }

  function handleClose() {
    isOpen = false;
    dispatch('close');
  }

  function handleDialogKeydown(event: KeyboardEvent) {
    event.stopPropagation();
    if (event.key === 'Escape') { event.preventDefault(); handleClose(); return; }
    if (event.key !== 'Tab' || !panel) return;
    const focusable = Array.from(panel.querySelectorAll<HTMLElement>('button:not([disabled]), a[href], input, select, textarea, [tabindex]:not([tabindex="-1"])'));
    if (focusable.length === 0) { event.preventDefault(); return; }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  }

  async function markAsRead(id: string) {
    const notification = notifications.find((item) => item.id === id);
    if (!notification || notification.isRead) return;
    await notificationsApi.markRead(id);
    notifications = notifications.map((item) => item.id === id ? { ...item, isRead: true } : item);
  }

  async function markAllAsRead() {
    if (unreadCount === 0) return;
    try {
      await notificationsApi.markAllRead();
      notifications = notifications.map((item) => ({ ...item, isRead: true }));
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not update notifications.';
    }
  }

  async function handleNotificationClick(notification: NotificationDto) {
    try { await markAsRead(notification.id); } catch { /* Navigation remains available if read-state persistence fails. */ }
    if (notification.linkUrl) await goto(notification.linkUrl);
    handleClose();
  }

  function formatTime(value: string) {
    const elapsed = Date.now() - new Date(value).getTime();
    if (!Number.isFinite(elapsed) || elapsed < 0) return 'Recently';
    const minutes = Math.floor(elapsed / 60_000);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    return `${Math.floor(hours / 24)}d ago`;
  }

  function getTypeColor(type: string) {
    switch (type) {
      case 'course':
        return 'text-ifa-pine bg-emerald-100';
      case 'achievement':
        return 'text-yellow-600 bg-yellow-100';
      case 'recommendation':
        return 'text-purple-600 bg-purple-100';
      case 'reminder':
        return 'text-blue-600 bg-blue-100';
      default:
        return 'text-gray-600 bg-gray-100';
    }
  }
</script>

{#if isOpen}
  <div class="fixed inset-0 bg-black/50 flex items-start justify-end pt-16 pr-4 z-50" role="presentation" on:click={handleClose}>
    <div
      bind:this={panel}
      class="w-full max-w-md bg-ifa-card rounded-2xl border border-ifa-border shadow-2xl overflow-hidden"
      role="dialog"
      tabindex="-1"
      aria-modal="true"
      aria-label="Notifications"
      on:click|stopPropagation={() => {}}
      on:keydown={handleDialogKeydown}
    >
      <!-- Header -->
      <div class="flex items-center justify-between px-4 py-4 border-b border-ifa-border">
        <div class="flex items-center gap-2">
          <Bell class="w-5 h-5 text-ifa-pine" />
          <h2 class="text-lg font-bold text-ifa-text-primary">Notifications</h2>
          {#if unreadCount > 0}
            <span class="px-2 py-0.5 bg-red-500 text-white text-xs font-bold rounded-full">
              {unreadCount}
            </span>
          {/if}
        </div>
        <div class="flex items-center gap-2">
          {#if unreadCount > 0}
            <button
              type="button"
              on:click={markAllAsRead}
              class="text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition"
            >
              Mark all read
            </button>
          {/if}
          <button
            type="button"
            aria-label="Close notifications"
            on:click={handleClose}
            class="p-2 text-ifa-text-muted hover:text-ifa-text-primary transition"
          >
            <X class="w-5 h-5" />
          </button>
        </div>
      </div>

      <!-- Notifications List -->
      <div class="max-h-96 overflow-y-auto">
        {#if loading}
          <div class="p-8 text-center text-sm text-ifa-text-secondary">Loading notifications…</div>
        {:else if error}
          <div class="p-6 text-center">
            <p role="alert" class="text-sm text-red-600">{error}</p>
            <button type="button" class="mt-3 text-xs font-semibold text-ifa-pine" on:click={() => { loaded = false; void loadNotifications(); }}>Retry</button>
          </div>
        {:else if notifications.length === 0}
          <div class="p-8 text-center">
            <Bell class="w-12 h-12 text-ifa-text-muted mx-auto mb-2" />
            <p class="text-ifa-text-secondary text-sm">No notifications</p>
          </div>
        {:else}
          <div class="divide-y divide-ifa-border">
            {#each notifications as notification}
              <button
                type="button"
                on:click={() => handleNotificationClick(notification)}
                class="w-full p-4 text-left hover:bg-ifa-card-muted transition {notification.isRead
                  ? 'opacity-60'
                  : 'bg-ifa-pine/5'}"
              >
                <div class="flex items-start gap-3">
                  <div class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0 {getTypeColor(notification.type)}">
                    {#if notification.type === 'course' || notification.type.includes('lesson')}
                      <BookOpen class="w-5 h-5" />
                    {:else if notification.type.includes('graded') || notification.type === 'achievement'}
                      <Award class="w-5 h-5" />
                    {:else if notification.type === 'recommendation'}
                      <Zap class="w-5 h-5" />
                    {:else if notification.type.includes('session') || notification.type === 'reminder'}
                      <Clock class="w-5 h-5" />
                    {:else}
                      <Bell class="w-5 h-5" />
                    {/if}
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="flex items-start justify-between gap-2">
                      <h4 class="text-sm font-semibold text-ifa-text-primary">{notification.title}</h4>
                      {#if !notification.isRead}
                        <div class="w-2 h-2 rounded-full bg-red-500 shrink-0 mt-1.5"></div>
                      {/if}
                    </div>
                    <p class="text-xs text-ifa-text-secondary mt-1 line-clamp-2">{notification.message}</p>
                    <p class="text-[10px] text-ifa-text-muted mt-2">{formatTime(notification.createdAt)}</p>
                  </div>
                </div>
              </button>
            {/each}
          </div>
        {/if}
      </div>

      <!-- Footer -->
      <div class="px-4 py-3 border-t border-ifa-border flex items-center justify-between">
        <button
          type="button"
          disabled={unreadCount === 0}
          on:click={markAllAsRead}
          class="text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition"
        >
          Mark all as read
        </button>
        <button
          type="button"
          on:click={() => { goto('/settings'); handleClose(); }}
          class="text-xs text-ifa-text-muted hover:text-ifa-text-primary transition"
        >
          Settings
        </button>
      </div>
    </div>
  </div>
{/if}
