<script lang="ts">
  import { Search, Grid, List } from 'lucide-svelte';
  import { goto } from '$app/navigation';
  import CourseLibrary from '$lib/components/CourseLibrary.svelte';
  import CourseFilterBar from '$lib/components/CourseFilterBar.svelte';
  import { courses, courseProgress, moduleInfo, toggleBookmark } from '$lib/stores/coursesStore';

  let viewMode = 'grid'; // 'grid' | 'list'
  let searchQuery = '';
  let activeFilter = 'all'; // 'all' | 'inProgress' | 'completed' | 'bookmarked'
  let sortBy = 'recent'; // 'recent' | 'progress' | 'name' | 'duration'

  // Enrich each store course with the display fields CourseLibrary expects.
  $: enrolledCourses = $courses.map((c) => ({
    ...c,
    progressPercent: courseProgress(c),
    moduleInfo: c.status === 'completed' ? 'Completed' : moduleInfo(c)
  }));

  $: filteredCourses = enrolledCourses.filter((course) => {
    const matchesSearch =
      course.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      course.provider.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesFilter =
      activeFilter === 'all' ||
      (activeFilter === 'inProgress' && course.status === 'inProgress') ||
      (activeFilter === 'completed' && course.status === 'completed') ||
      (activeFilter === 'bookmarked' && course.isBookmarked);
    return matchesSearch && matchesFilter;
  });

  $: sortedCourses = [...filteredCourses].sort((a, b) => {
    switch (sortBy) {
      case 'progress':
        return b.progressPercent - a.progressPercent;
      case 'name':
        return a.title.localeCompare(b.title);
      case 'duration':
        return a.duration.localeCompare(b.duration);
      case 'recent':
      default:
        return 0; // preserve store order (lastAccessed is a relative label, not a date)
    }
  });

  function handleToggleBookmark(courseId: string) {
    toggleBookmark(courseId);
  }

  function handleContinueCourse(courseId: string) {
    goto(`/app/courses/${courseId}/learn`);
  }
</script>

<div class="library-shell space-y-6">
  <!-- Header -->
  <div class="library-heading">
    <div>
      <p class="wf-kicker">Your learning library</p>
      <h1 class="wf-display">Every path you have <em>chosen.</em></h1>
      <p>Continue an active release, return to saved work, or review what you have completed.</p>
    </div>
    <div class="flex items-center gap-2">
      <div class="flex items-center gap-1 bg-ifa-card-muted rounded-lg p-1">
        <button
          type="button"
          aria-label="Grid view"
          aria-pressed={viewMode === 'grid'}
          on:click={() => viewMode = 'grid'}
          class="p-2 rounded-md {viewMode === 'grid' ? 'bg-white text-ifa-pine shadow-soft' : 'text-ifa-text-muted hover:text-ifa-pine'}"
        >
          <Grid class="w-4 h-4" />
        </button>
        <button
          type="button"
          aria-label="List view"
          aria-pressed={viewMode === 'list'}
          on:click={() => viewMode = 'list'}
          class="p-2 rounded-md {viewMode === 'list' ? 'bg-white text-ifa-pine shadow-soft' : 'text-ifa-text-muted hover:text-ifa-pine'}"
        >
          <List class="w-4 h-4" />
        </button>
      </div>
    </div>
  </div>

  <!-- Search and Filters -->
  <div class="flex flex-col md:flex-row gap-4">
    <!-- Search Bar -->
    <div class="flex-1 relative">
      <Search class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
      <input
        type="text"
        aria-label="Search your courses"
        bind:value={searchQuery}
        placeholder="Search courses..."
        class="w-full pl-10 pr-4 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine"
      />
    </div>

    <!-- Filter Bar -->
    <CourseFilterBar
      activeFilter={activeFilter}
      sortBy={sortBy}
      onFilterChange={(filter) => activeFilter = filter}
      onSortChange={(sort) => sortBy = sort}
    />
  </div>

  <!-- Course Stats -->
  <div class="flex items-center gap-6 text-sm">
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <span class="font-semibold text-ifa-text-primary">{enrolledCourses.length}</span>
      <span>Total Courses</span>
    </div>
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <span class="font-semibold text-ifa-pine">{enrolledCourses.filter(c => c.status === 'inProgress').length}</span>
      <span>In Progress</span>
    </div>
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <span class="font-semibold text-emerald-600">{enrolledCourses.filter(c => c.status === 'completed').length}</span>
      <span>Completed</span>
    </div>
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <span class="font-semibold text-ifa-accent-purple">{enrolledCourses.filter(c => c.isBookmarked).length}</span>
      <span>Bookmarked</span>
    </div>
  </div>

  <!-- Course Library -->
  <CourseLibrary
    courses={sortedCourses}
    viewMode={viewMode}
    onToggleBookmark={handleToggleBookmark}
    onContinue={handleContinueCourse}
  />

  <!-- Empty State -->
  {#if sortedCourses.length === 0}
    <div class="text-center py-12">
      <div class="w-16 h-16 bg-ifa-card-muted rounded-full flex items-center justify-center mx-auto mb-4">
        <Search class="w-8 h-8 text-ifa-text-muted" />
      </div>
      <h3 class="text-lg font-semibold text-ifa-text-primary mb-2">No courses found</h3>
      <p class="text-ifa-text-secondary">Try adjusting your search or filters</p>
    </div>
  {/if}
</div>

<style>
  .library-shell{width:min(100%,94rem);margin:0 auto;padding:clamp(1.5rem,3vw,3rem)}.library-heading{display:flex;align-items:end;justify-content:space-between;gap:2rem;padding:1rem 0 2.8rem;border-bottom:1px solid var(--wf-border)}.library-heading .wf-kicker{color:var(--wf-text-muted)}.library-heading h1{max-width:58rem;margin:1rem 0 .7rem;font-size:clamp(3.5rem,6vw,7rem);font-weight:430;line-height:.84;letter-spacing:-.055em}.library-heading h1 em{color:#176b4a;font-weight:430}.library-heading>div:first-child>p:last-child{max-width:45rem;color:var(--wf-text-muted);font-size:.72rem}.library-shell :global(.rounded-2xl),.library-shell :global(.rounded-xl),.library-shell :global(.rounded-lg){border-radius:0!important}.library-shell :global(.bg-ifa-card){box-shadow:none!important}.library-shell :global(.bg-ifa-pine){color:#17221c!important;background:#c7f464!important}@media(max-width:760px){.library-shell{padding:1rem}.library-heading{display:block}.library-heading h1{font-size:4rem}.library-heading>div+div{margin-top:1rem}}
</style>
