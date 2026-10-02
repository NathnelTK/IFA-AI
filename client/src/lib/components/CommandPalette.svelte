<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { goto } from '$app/navigation';
  import { Search, BookOpen, Video, Layout, Users, Settings, X, ArrowRight } from 'lucide-svelte';

  export let isOpen = false;
  export let query = '';

  const dispatch = createEventDispatcher();

  const commands = [
    { type: 'course', title: 'C# Backend Development', subtitle: '4 modules • 8 lessons', icon: BookOpen, href: '/courses/csharp-backend' },
    { type: 'course', title: 'Python Fundamentals', subtitle: '2 modules • 3 lessons', icon: BookOpen, href: '/courses/python-fundamentals' },
    { type: 'lesson', title: 'Working with REST APIs', subtitle: 'C# Backend Development • Module 3', icon: Video, href: '/courses/csharp-backend' },
    { type: 'lesson', title: 'Joining Tables', subtitle: 'SQL for Developers • Module 1', icon: Video, href: '/courses/sql-developers' },
    { type: 'page', title: 'My Learning', subtitle: 'Dashboard', icon: Layout, href: '/my-learning' },
    { type: 'page', title: 'Courses', subtitle: 'Library', icon: BookOpen, href: '/courses' },
    { type: 'page', title: 'My Skills', subtitle: 'Profile', icon: Users, href: '/my-skills' },
    { type: 'page', title: 'Settings', subtitle: 'Preferences', icon: Settings, href: '/settings' }
  ];

  // Filter live as the user types.
  $: searchResults = query.trim()
    ? commands.filter(
        (c) =>
          c.title.toLowerCase().includes(query.toLowerCase()) ||
          c.subtitle.toLowerCase().includes(query.toLowerCase())
      )
    : commands;

  let selectedIndex = 0;
  $: if (selectedIndex >= searchResults.length) selectedIndex = 0;

  function handleClose() {
    isOpen = false;
    query = '';
    selectedIndex = 0;
    dispatch('close');
  }

  function handleSelect(result: any) {
    if (result?.href) goto(result.href);
    handleClose();
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (!isOpen) return;
    if (e.key === 'Escape') {
      handleClose();
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      selectedIndex = Math.min(selectedIndex + 1, searchResults.length - 1);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      selectedIndex = Math.max(selectedIndex - 1, 0);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      handleSelect(searchResults[selectedIndex]);
    }
  }

  function getTypeColor(type: string) {
    switch (type) {
      case 'course':
        return 'text-ifa-pine bg-emerald-100';
      case 'lesson':
        return 'text-blue-600 bg-blue-100';
      case 'page':
        return 'text-purple-600 bg-purple-100';
      default:
        return 'text-gray-600 bg-gray-100';
    }
  }
</script>

<svelte:window on:keydown={handleKeyDown} />

{#if isOpen}
  <div class="fixed inset-0 bg-black/50 flex items-start justify-center pt-[20vh] z-50" on:click={handleClose}>
    <div
      class="w-full max-w-2xl bg-ifa-card rounded-2xl border border-ifa-border shadow-2xl overflow-hidden"
      on:click|stopPropagation={() => {}}
    >
      <!-- Search Input -->
      <div class="flex items-center gap-3 px-4 py-4 border-b border-ifa-border">
        <Search class="w-5 h-5 text-ifa-text-muted" />
        <input
          type="text"
          bind:value={query}
          placeholder="Search courses, lessons, pages..."
          class="flex-1 bg-transparent text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none"
          autofocus
        />
        <button
          type="button"
          on:click={handleClose}
          class="p-2 text-ifa-text-muted hover:text-ifa-text-primary transition"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Results -->
      <div class="max-h-96 overflow-y-auto">
        {#if searchResults.length === 0}
          <div class="p-8 text-center">
            <p class="text-ifa-text-secondary text-sm">No results found</p>
          </div>
        {:else}
          <div class="p-2">
            {#each searchResults as result, index}
              <button
                type="button"
                on:click={() => handleSelect(result)}
                class="w-full flex items-center gap-3 px-4 py-3 rounded-lg transition {index === selectedIndex
                  ? 'bg-ifa-pine/10'
                  : 'hover:bg-ifa-card-muted'}"
              >
                <div class="w-8 h-8 rounded-lg flex items-center justify-center {getTypeColor(result.type)}">
                  {#if result.icon === BookOpen}
                    <BookOpen class="w-4 h-4" />
                  {:else if result.icon === Video}
                    <Video class="w-4 h-4" />
                  {:else if result.icon === Layout}
                    <Layout class="w-4 h-4" />
                  {:else if result.icon === Users}
                    <Users class="w-4 h-4" />
                  {:else if result.icon === Settings}
                    <Settings class="w-4 h-4" />
                  {:else}
                    <Search class="w-4 h-4" />
                  {/if}
                </div>
                <div class="flex-1 text-left">
                  <h4 class="text-sm font-semibold text-ifa-text-primary">{result.title}</h4>
                  <p class="text-xs text-ifa-text-secondary">{result.subtitle}</p>
                </div>
                {#if index === selectedIndex}
                  <ArrowRight class="w-4 h-4 text-ifa-pine" />
                {/if}
              </button>
            {/each}
          </div>
        {/if}
      </div>

      <!-- Footer -->
      <div class="px-4 py-2 border-t border-ifa-border flex items-center justify-between text-xs text-ifa-text-muted">
        <div class="flex items-center gap-4">
          <span class="flex items-center gap-1">
            <kbd class="px-1.5 py-0.5 bg-ifa-card-muted rounded text-[10px]">↑↓</kbd>
            <span>Navigate</span>
          </span>
          <span class="flex items-center gap-1">
            <kbd class="px-1.5 py-0.5 bg-ifa-card-muted rounded text-[10px]">↵</kbd>
            <span>Select</span>
          </span>
        </div>
        <span class="flex items-center gap-1">
          <kbd class="px-1.5 py-0.5 bg-ifa-card-muted rounded text-[10px]">esc</kbd>
          <span>Close</span>
        </span>
      </div>
    </div>
  </div>
{/if}
