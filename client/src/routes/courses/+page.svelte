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
    goto(`/courses/${courseId}`);
  }
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Courses</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Your personal course library</p>
    </div>
    <div class="flex items-center gap-2">
      <div class="flex items-center gap-1 bg-ifa-card-muted rounded-lg p-1">
        <button
          type="button"
          on:click={() => viewMode = 'grid'}
          class="p-2 rounded-md {viewMode === 'grid' ? 'bg-white text-ifa-pine shadow-soft' : 'text-ifa-text-muted hover:text-ifa-pine'}"
        >
          <Grid class="w-4 h-4" />
        </button>
        <button
          type="button"
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
