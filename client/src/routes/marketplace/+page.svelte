<script lang="ts">
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { Search, Filter, Grid, List, BookOpen, Star, Users, Clock, TrendingUp } from 'lucide-svelte';
  import PublicCourseCard from '$lib/components/PublicCourseCard.svelte';
  import MarketplaceFilters from '$lib/components/MarketplaceFilters.svelte';
  import { coursesApi, type MarketplaceCourseDto } from '$lib/api';
  import { resolveCover } from '$lib/utils/cover';

  interface MarketplaceCard {
    id: string;
    title: string;
    instructor: string;
    rating: number;
    reviews: number;
    enrolled: number;
    modules: number;
    duration: string;
    level: string;
    category: string;
    tags: string[];
    thumbnail: string;
    isPublished: boolean;
    shareCode?: string;
  }

  let searchQuery = '';
  let viewMode = 'grid';
  let activeFilter = 'all';
  let publicCourses: MarketplaceCard[] = [];
  let loading = true;
  let error = '';

  const categories = [
    { id: 'all', name: 'All Courses' },
    { id: 'frontend', name: 'Frontend' },
    { id: 'backend', name: 'Backend' },
    { id: 'data-science', name: 'Data Science' },
    { id: 'ai-ml', name: 'AI/ML' },
    { id: 'cloud', name: 'Cloud' },
    { id: 'devops', name: 'DevOps' },
    { id: 'security', name: 'Security' },
    { id: 'entrance-exam', name: 'Entrance Exam' }
  ];

  async function handleEnroll(course: MarketplaceCard) {
    if (!course.shareCode) {
      error = `This course cannot be enrolled in yet: ${course.title} has no share code.`;
      return;
    }

    try {
      await coursesApi.join(course.shareCode);
      await goto(`/courses/${course.id}`);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Enrollment failed.';
    }
  }

  function handleBookmark(courseId: string) {
    console.log('Bookmarking course:', courseId);
  }

  function parseCount(value: string | undefined): number {
    if (!value) return 0;
    const match = value.trim().toLowerCase().match(/^([\d.]+)\s*([km]?)/);
    if (!match) return 0;
    const amount = parseFloat(match[1]);
    if (Number.isNaN(amount)) return 0;
    if (match[2] === 'k') return Math.round(amount * 1000);
    if (match[2] === 'm') return Math.round(amount * 1_000_000);
    return Math.round(amount);
  }

  function toCard(course: MarketplaceCourseDto): MarketplaceCard {
    return {
      id: course.id,
      title: course.title,
      instructor: course.providerName || 'IFA AI',
      rating: course.rating,
      reviews: parseCount(course.reviewCount),
      enrolled: 0,
      modules: course.moduleCount,
      duration: course.estimatedDuration,
      level: course.targetAudience || 'All levels',
      category: course.category,
      tags: [course.category],
      thumbnail: resolveCover(course.thumbnailUrl, course.title),
      isPublished: true,
      shareCode: course.shareCode
    };
  }

  onMount(async () => {
    try {
      const list = await coursesApi.marketplace();
      publicCourses = list.map(toCard);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not load the public course catalog.';
    } finally {
      loading = false;
    }
  });

  $: filteredCourses = publicCourses.filter(course => {
    const matchesSearch = course.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
                        course.instructor.toLowerCase().includes(searchQuery.toLowerCase()) ||
                        course.tags.some(tag => tag.toLowerCase().includes(searchQuery.toLowerCase()));
    const matchesFilter = activeFilter === 'all' || course.category.toLowerCase().replace(/\s+/g, '') === activeFilter.replace('-', '');
    return matchesSearch && matchesFilter;
  });
</script>

<div class="p-6 md:p-8 space-y-6 max-w-[1600px] mx-auto">
  <!-- Header -->
  <div class="flex items-center justify-between mb-6">
    <div>
      <h1 class="text-2xl font-bold text-ifa-text-primary">Course Marketplace</h1>
      <p class="text-sm text-ifa-text-secondary mt-1">Discover courses published by the community</p>
    </div>
    <div class="flex items-center gap-2">
      <div class="relative">
        <Search class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ifa-text-muted" />
        <input
          type="text"
          bind:value={searchQuery}
          placeholder="Search courses..."
          class="pl-10 pr-4 py-2.5 rounded-xl bg-ifa-card-muted border border-ifa-border text-sm text-ifa-text-primary placeholder-ifa-text-muted focus:outline-none focus:ring-1 focus:ring-ifa-pine w-64"
        />
      </div>
      <button
        type="button"
        on:click={() => viewMode = viewMode === 'grid' ? 'list' : 'grid'}
        class="p-2.5 rounded-lg bg-ifa-card-muted border border-ifa-border text-ifa-text-secondary hover:text-ifa-pine transition"
      >
        {#if viewMode === 'grid'}
          <List class="w-5 h-5" />
        {:else}
          <Grid class="w-5 h-5" />
        {/if}
      </button>
    </div>
  </div>

  <!-- Categories -->
  <MarketplaceFilters
    categories={categories}
    activeFilter={activeFilter}
    onFilterChange={(filter) => activeFilter = filter}
  />

  <!-- Stats -->
  <div class="flex items-center gap-6 text-sm">
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <BookOpen class="w-4 h-4" />
      <span>{publicCourses.length} courses available</span>
    </div>
    <div class="flex items-center gap-2 text-ifa-text-secondary">
      <Users class="w-4 h-4" />
      <span>{publicCourses.reduce((sum, c) => sum + c.enrolled, 0).toLocaleString()} total enrollments</span>
    </div>
    <div class="flex items-center gap-2 text-emerald-600">
      <TrendingUp class="w-4 h-4" />
      <span>Community powered</span>
    </div>
  </div>

  <!-- Course Grid -->
  {#if error}
    <p role="alert" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>
  {/if}
  {#if loading}
    <p class="py-12 text-center text-sm text-ifa-text-secondary">Loading public courses from the IFA API…</p>
  {:else if filteredCourses.length === 0}
    <div class="text-center py-12">
      <BookOpen class="w-12 h-12 text-ifa-text-muted mx-auto mb-4" />
      <p class="text-ifa-text-secondary">{error ? 'The public course catalog is unavailable.' : 'No public courses found matching your criteria.'}</p>
    </div>
  {:else}
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
      {#each filteredCourses as course}
        <PublicCourseCard
          course={course}
          onEnroll={() => handleEnroll(course)}
          onBookmark={() => handleBookmark(course.id)}
        />
      {/each}
    </div>
  {/if}
</div>
