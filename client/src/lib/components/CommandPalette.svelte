<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { Search, BookOpen, Video, Layout, Users, Settings, X, ArrowRight, Loader2 } from 'lucide-svelte';
  import { searchApi } from '$lib/api';
  import { courses } from '$lib/stores/coursesStore';
  import { get } from 'svelte/store';

  export let isOpen = false;
  export let query = '';

  const dispatch = createEventDispatcher();

  interface Result {
    type: 'course' | 'lesson' | 'page';
    title: string;
    subtitle: string;
    href: string;
  }

  // Static navigation targets — these are app pages, not content, so they are
  // matched locally rather than searched in the database.
  const pages: Result[] = [
    { type: 'page', title: 'My Courses', subtitle: 'Library', href: '/courses' },
    { type: 'page', title: 'My Skills', subtitle: 'Skill profile', href: '/my-skills' },
    { type: 'page', title: 'Progress', subtitle: 'Learning analytics', href: '/progress' },
    { type: 'page', title: 'Recommendations', subtitle: 'For you', href: '/recommendations' },
    { type: 'page', title: 'Marketplace', subtitle: 'Public courses', href: '/marketplace' },
    { type: 'page', title: 'Research', subtitle: 'Sources & summaries', href: '/research' },
    { type: 'page', title: 'Settings', subtitle: 'Preferences', href: '/settings' }
  ];

  // Results are only shown once the learner actually types something.
  let results: Result[] = [];
  let loading = false;
  let selectedIndex = 0;
  let searchError = false;

  let lastQuery = '';
  let debounceTimer: ReturnType<typeof setTimeout> | null = null;
  let requestSeq = 0;

  function localPageResults(q: string): Result[] {
    const needle = q.toLowerCase();
    return pages.filter(
      (p) => p.title.toLowerCase().includes(needle) || p.subtitle.toLowerCase().includes(needle)
    );
  }

  // Search enrolled (demo/local) courses + the backend course & lesson index.
  async function runSearch(q: string) {
    const seq = ++requestSeq;
    loading = true;
    searchError = false;

    try {
      const remote = await searchApi.query(q);
      if (seq !== requestSeq) return; // a newer query superseded this one

      const localCourses: Result[] = get(courses)
        .filter((c) => c.title.toLowerCase().includes(q.toLowerCase()))
        .slice(0, 4)
        .map((c) => ({
          type: 'course',
          title: c.title,
          subtitle: c.provider || 'Course',
          href: `/courses/${c.id}`
        }));

      const remoteCourses: Result[] = (remote.courses as any[]).map((c) => ({
        type: 'course',
        title: c.title,
        subtitle: c.category || 'Course',
        href: `/courses/${c.id}`
      }));

      const lessons: Result[] = (remote.lessons as any[]).map((l) => ({
        type: 'lesson',
        title: l.title,
        subtitle: l.summary || 'Lesson',
        href: `/courses/${l.courseId}`
      }));

      // De-duplicate courses by href, keeping local entries first.
      const seen = new Set<string>();
      const merged = [...localPageResults(q), ...localCourses, ...remoteCourses, ...lessons].filter((r) => {
        if (seen.has(r.href + r.title)) return false;
        seen.add(r.href + r.title);
        return true;
      });

      results = merged;
      selectedIndex = 0;
    } catch {
      // Backend unreachable: still offer local courses + pages.
      if (seq !== requestSeq) return;
      searchError = true;
      const needle = q.toLowerCase();
      results = [
        ...localPageResults(q),
        ...get(courses)
          .filter((c) => c.title.toLowerCase().includes(needle))
          .slice(0, 6)
          .map((c) => ({
            type: 'course' as const,
            title: c.title,
            subtitle: c.provider || 'Course',
            href: `/courses/${c.id}`
          }))
      ];
      selectedIndex = 0;
    } finally {
      if (seq === requestSeq) loading = false;
    }
  }

  // Debounce typing so we don't hit the API on every keystroke.
  $: {
    const q = query.trim();
    if (q === lastQuery) {
      // no-op: same query
    } else {
      lastQuery = q;
      if (debounceTimer) clearTimeout(debounceTimer);
      if (q.length === 0) {
        results = [];
        loading = false;
        selectedIndex = 0;
      } else {
        loading = true;
        debounceTimer = setTimeout(() => void runSearch(q), 220);
      }
    }
  }

  $: if (selectedIndex >= results.length) selectedIndex = 0;

  function handleClose() {
    isOpen = false;
    query = '';
    results = [];
    selectedIndex = 0;
    lastQuery = '';
    dispatch('close');
  }

  function handleSelect(result: Result | undefined) {
    if (result?.href) goto(result.href);
    handleClose();
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (!isOpen) return;
    if (e.key === 'Escape') {
      handleClose();
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      selectedIndex = Math.min(selectedIndex + 1, Math.max(results.length - 1, 0));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      selectedIndex = Math.max(selectedIndex - 1, 0);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      handleSelect(results[selectedIndex]);
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

  function iconFor(type: string) {
    if (type === 'course') return BookOpen;
    if (type === 'lesson') return Video;
    return Layout;
  }

  $: hasQuery = query.trim().length > 0;

  // Focus the input when the palette opens (Svelte action, replaces autofocus).
  function focusOnMount(node: HTMLInputElement) {
    queueMicrotask(() => node.focus());
    return {};
  }
</script>

<svelte:window on:keydown={handleKeyDown} />

{#if isOpen}
  <div
    class="fixed inset-0 bg-black/50 flex items-start justify-center pt-[20vh] z-50"
    role="presentation"
    on:click={handleClose}
  >
    <div
      class="w-full max-w-2xl bg-ifa-card rounded-2xl border border-ifa-border shadow-2xl overflow-hidden"
      role="dialog"
      aria-modal="true"
      aria-label="Search"
      on:click|stopPropagation={() => {}}
    >
      <!-- Search Input -->
      <div class="flex items-center gap-3 px-4 py-4 border-b border-ifa-border">
        <Search class="w-5 h-5 text-ifa-text-muted" />
        <input
          type="text"
          bind:value={query}
          use:focusOnMount
          placeholder="Search courses, lessons, pages..."
          class="flex-1 bg-transparent text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none"
        />
        {#if loading}
          <Loader2 class="w-4 h-4 animate-spin text-ifa-text-muted" />
        {/if}
        <button
          type="button"
          on:click={handleClose}
          class="p-2 text-ifa-text-muted hover:text-ifa-text-primary transition"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Body: prompt until typed, then results -->
      <div class="max-h-96 overflow-y-auto">
        {#if !hasQuery}
          <!-- Hint shown before typing — no long list of everything -->
          <div class="p-8 text-center">
            <Search class="w-6 h-6 text-ifa-text-muted mx-auto mb-3" />
            <p class="text-sm font-medium text-ifa-text-primary">Search IFA</p>
            <p class="text-xs text-ifa-text-secondary mt-1">
              Type to search your courses, lessons and pages.
            </p>
          </div>
        {:else if loading && results.length === 0}
          <div class="p-8 text-center text-ifa-text-secondary text-sm">Searching…</div>
        {:else if results.length === 0}
          <div class="p-8 text-center">
            <p class="text-ifa-text-secondary text-sm">No results for “{query.trim()}”</p>
          </div>
        {:else}
          <div class="p-2">
            {#each results as result, index}
              {@const Icon = iconFor(result.type)}
              <button
                type="button"
                on:click={() => handleSelect(result)}
                class="w-full flex items-center gap-3 px-4 py-3 rounded-lg transition {index === selectedIndex
                  ? 'bg-ifa-pine/10'
                  : 'hover:bg-ifa-card-muted'}"
              >
                <div class="w-8 h-8 rounded-lg flex items-center justify-center {getTypeColor(result.type)}">
                  <svelte:component this={Icon} class="w-4 h-4" />
                </div>
                <div class="flex-1 text-left min-w-0">
                  <h4 class="text-sm font-semibold text-ifa-text-primary truncate">{result.title}</h4>
                  <p class="text-xs text-ifa-text-secondary truncate">{result.subtitle}</p>
                </div>
                {#if index === selectedIndex}
                  <ArrowRight class="w-4 h-4 text-ifa-pine shrink-0" />
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
