<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { Bell, X, Check, BookOpen, Award, Zap, Clock } from 'lucide-svelte';

  export let isOpen = false;

  const dispatch = createEventDispatcher();

  const notifications = [
    {
      id: 'notif-1',
      type: 'course',
      title: 'New Module Available',
      message: 'Module 2: REST API Design is now available in C# Backend Development',
      time: '2 hours ago',
      read: false,
      icon: BookOpen
    },
    {
      id: 'notif-2',
      type: 'achievement',
      title: 'Achievement Unlocked',
      message: 'You completed C# Fundamentals with a score of 85%',
      time: '5 hours ago',
      read: false,
      icon: Award
    },
    {
      id: 'notif-3',
      type: 'recommendation',
      title: 'IFA Recommendation',
      message: 'Based on your progress, we recommend learning Database Optimization',
      time: '1 day ago',
      read: true,
      icon: Zap
    },
    {
      id: 'notif-4',
      type: 'reminder',
      title: 'Study Reminder',
      message: 'Time to continue your learning journey! You have 2 courses in progress.',
      time: '2 days ago',
      read: true,
      icon: Clock
    }
  ];

  let unreadCount = notifications.filter(n => !n.read).length;

  function handleClose() {
    isOpen = false;
    dispatch('close');
  }

  function markAsRead(id: string) {
    const notification = notifications.find(n => n.id === id);
    if (notification) {
      notification.read = true;
      unreadCount = notifications.filter(n => !n.read).length;
    }
  }

  function markAllAsRead() {
    notifications.forEach(n => n.read = true);
    unreadCount = 0;
  }

  function handleNotificationClick(notification: any) {
    markAsRead(notification.id);
    console.log('Navigate to:', notification);
    handleClose();
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
  <div class="fixed inset-0 bg-black/50 flex items-start justify-end pt-16 pr-4 z-50" on:click={handleClose}>
    <div
      class="w-full max-w-md bg-ifa-card rounded-2xl border border-ifa-border shadow-2xl overflow-hidden"
      on:click|stopPropagation={() => {}}
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
            on:click={handleClose}
            class="p-2 text-ifa-text-muted hover:text-ifa-text-primary transition"
          >
            <X class="w-5 h-5" />
          </button>
        </div>
      </div>

      <!-- Notifications List -->
      <div class="max-h-96 overflow-y-auto">
        {#if notifications.length === 0}
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
                class="w-full p-4 text-left hover:bg-ifa-card-muted transition {notification.read
                  ? 'opacity-60'
                  : 'bg-ifa-pine/5'}"
              >
                <div class="flex items-start gap-3">
                  <div class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0 {getTypeColor(notification.type)}">
                    {#if notification.icon === BookOpen}
                      <BookOpen class="w-5 h-5" />
                    {:else if notification.icon === Award}
                      <Award class="w-5 h-5" />
                    {:else if notification.icon === Zap}
                      <Zap class="w-5 h-5" />
                    {:else if notification.icon === Clock}
                      <Clock class="w-5 h-5" />
                    {:else}
                      <Bell class="w-5 h-5" />
                    {/if}
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="flex items-start justify-between gap-2">
                      <h4 class="text-sm font-semibold text-ifa-text-primary">{notification.title}</h4>
                      {#if !notification.read}
                        <div class="w-2 h-2 rounded-full bg-red-500 shrink-0 mt-1.5"></div>
                      {/if}
                    </div>
                    <p class="text-xs text-ifa-text-secondary mt-1 line-clamp-2">{notification.message}</p>
                    <p class="text-[10px] text-ifa-text-muted mt-2">{notification.time}</p>
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
          class="text-xs font-semibold text-ifa-pine hover:text-emerald-700 transition"
        >
          View all notifications
        </button>
        <button
          type="button"
          class="text-xs text-ifa-text-muted hover:text-ifa-text-primary transition"
        >
          Settings
        </button>
      </div>
    </div>
  </div>
{/if}
